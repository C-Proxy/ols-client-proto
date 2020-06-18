using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UniRx;

public class LabelObjectManager : MonoBehaviour
{
    [SerializeField] GameObject prefab_LabelObject = default;
    [SerializeField] ActiveImage _ActiveImage = default;
    [SerializeField] Transform transform_Disposed = default;
    Transform transform_ActiveImage => _ActiveImage.transform;
    [SerializeField] InteractManager _InteractManager = default;
    [SerializeField] LabelEditor _LabelEditor = default;
    [SerializeField] ClassWindow _ClassWindow = default;
    [SerializeField] BaseCanvas _BaseCanvas = default;

    const float FLAME_SCALE = 2f;

    List<LabelObject>[] LabelObjects;
    Queue<int>[] EmptyIndexes;
    Queue<LabelObject> DisposedLabelObjects = new Queue<LabelObject>();

    [SerializeField] LabelObject ActiveLabel = default;
    int CurrentId;

    float _RimWidth;

    public void SetCurrentId(int classId)
    {
        CurrentId = classId;
    }

    Subject<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> CreateLabelSubject = new Subject<(int, int, Vector2 Position, Vector2 Size)>();
    Subject<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> ResizeLabelSubject = new Subject<(int, int, Vector2 Position, Vector2 Size)>();
    Subject<(int ClassId, int LabelId)> RemoveLabelSubject = new Subject<(int ClassId, int LabelId)>();
    Subject<(int ClassId, int LabelId, int newClassId, int newLabelId)> ChangeClassSubject = new Subject<(int ClassId, int LabelId, int newClassId, int newLabelId)>();

    public IObservable<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> OnCreateLabel => CreateLabelSubject;
    public IObservable<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> OnResizeLabel => ResizeLabelSubject;
    public IObservable<(int ClassId, int LabelId)> OnRemoveLabel => RemoveLabelSubject;
    public IObservable<(int ClassId, int LabelId, int newClassId, int newLabelId)> OnChangeClass => ChangeClassSubject;

    Vector2 ImageSize;
    public void SetImageSize(Vector2 size) => ImageSize = size;

    public void Init()
    {
        _InteractManager.OnActiveLabelChange
            .Pairwise()
            .Subscribe(pair =>
            {
                if (pair.Previous != null)
                    pair.Previous.SetActivate(false);
                if (pair.Current != null)
                {
                    ActiveLabel = pair.Current;
                    ActiveLabel.SetActivate(true);
                    _LabelEditor.SetParentLabel(ActiveLabel);
                }
            }).AddTo(this);

        _ClassWindow.OnAcitveElementChanged
            .Subscribe(classId =>
            {
                SetCurrentId(classId);
            }).AddTo(this);

        _LabelEditor.OnBeginDrag
            .Subscribe(value => OnDragEditor(value)).AddTo(this);

        _BaseCanvas.OnChangedScale
            .Subscribe(scale => SetFlameScaleAll(scale)).AddTo(this);
    }
    public void SetLength(int length)
    {
        LabelObjects = Enumerable.Range(0, length).Select(_ => new List<LabelObject>()).ToArray();
        EmptyIndexes = Enumerable.Range(0, length).Select(_ => new Queue<int>()).ToArray();
    }
    //生成、削除
    public LabelObject CreateLabelObject(int classId, int index, Vector2 positon, Vector2 size)
    {
        var list = LabelObjects[classId];
        if (list == null)
        {
            Debug.LogError($"クラスID:{classId}が存在しません。");
            return null;
        }
        LabelObject labelObject;
        if (DisposedLabelObjects.Count == 0)
        {
            var obj = Instantiate(prefab_LabelObject, transform_ActiveImage);
            labelObject = obj.GetComponent<LabelObject>();
        }
        else
        {
            labelObject = DisposedLabelObjects.Dequeue();
            labelObject.transform.SetParent(transform_ActiveImage);

            labelObject.Restore();
        }
        labelObject.name = $"Label[{classId}][{index}]";
        var queue = EmptyIndexes[classId];
        if (queue.Count != 0)
            queue.Dequeue();
        if (index >= list.Count)
            list.Add(labelObject);
        else
            list[index] = labelObject;

        labelObject.Init(positon, size, classId, index);
        labelObject.OnCreated
            .Subscribe(info => CreateLabelSubject.OnNext((classId, index, info.Position, info.Size))).AddTo(this);
        labelObject.OnResized
            .Subscribe(info => ResizeLabelSubject.OnNext((classId, index, info.Position, info.Size))).AddTo(this);
        labelObject.OnRemove
            .Subscribe(_ => RemoveLabelSubject.OnNext((classId, index))).AddTo(this);
        labelObject.OnChangedClass
            .Subscribe(currentClassId => ChangeClassSubject.OnNext((classId, index, currentClassId, GetEmptyIndex(currentClassId)))).AddTo(this);
        return labelObject;
    }
    public LabelObject CreateLabelObject(int classId, Vector2 position, Vector2 size) => CreateLabelObject(classId, GetEmptyIndex(classId), position, size);
    public LabelObject CreateLabelObject(Vector2 position, Vector2 size) => CreateLabelObject(CurrentId, position, size);
    public LabelObject CreateLabelObject(Vector2 position) => CreateLabelObject(position, Vector2.zero);
    public void CreateLabelObject(int classId, (Vector2, Vector2)[] infoArray)
    {
        var length = infoArray.Length;
        for (int i = 0; i < length; i++)
        {
            CreateLabelObject(classId, infoArray[i].Item1, infoArray[i].Item2);
        }
    }
    public void ResizeLabelObject(int classId, int labelId, Vector2 newPosition, Vector2 newSize)
    {
        var label = LabelObjects[classId][labelId];
        label.SetPosition(newPosition);
        label.SetSize(newSize);
    }
    public void RemoveLabelObject(int classId, int index)
    {
        var list = LabelObjects[classId];
        if (list == null)
        {
            Debug.LogError($"クラスID:{classId}が存在しません。");
            return;
        }
        var labelObject = list[index];
        if (labelObject == null)
        {
            Debug.LogError($"削除対象:LabelObjects[{classId}][{index}]が存在しません。");
        }
        else
        {
            list[index] = null;
            DisposedLabelObjects.Enqueue(labelObject);
            EmptyIndexes[classId].Enqueue(index);
            labelObject.transform.SetParent(transform_Disposed);
            labelObject.Dispose();
            return;
        }
    }
    public LabelObject ChangeClassLabelObject(int classId, int labelId, int newClassId, int newLabelId)
    {
        var label = LabelObjects[classId][labelId];
        var pos = label.localPosition;
        var size = label.sizeDelta;
        var newLabel = CreateLabelObject(newClassId, newLabelId, pos, size);
        RemoveLabelObject(classId, labelId);
        return newLabel;
    }
    public void RefreshLabels()
    {
        foreach (var list in EmptyIndexes)
            list.Clear();
        foreach (var label in LabelObjects.SelectMany(_ => _).Where(_ => _ != null))
        {
            DisposedLabelObjects.Enqueue(label);
            label.transform.SetParent(transform_Disposed);
            label.Dispose();
        }
        foreach (var list in LabelObjects)
            list.Clear();

    }
    //操作
    public void OnDragEditor(int value)
    {
        var slice = (Slice)value;
        if (!Input.GetMouseButton(0))
            return;
        var onLeftUp = _InteractManager.OnMouseUp(0);
        var drag = _InteractManager.OnUpdate_Mouse.TakeUntil(onLeftUp);

        var prePos = ActiveLabel.localPosition;
        var preSize = ActiveLabel.sizeDelta;
        Action onComplete = () =>
        {
            if (!ActiveLabel.IsValidSize())
            {
                ActiveLabel.Remove();
                _InteractManager.SetDefault();
            }
            else
                ActiveLabel.ConfirmResize();
        };

        switch (slice)
        {
            //Corner
            case Slice.LeftTop:
            case Slice.RightTop:
            case Slice.LeftBottom:
            case Slice.RightBottom:
                Vector2 opposite;
                switch (slice)
                {
                    case Slice.LeftTop:
                        opposite = prePos + new Vector2(preSize.x, 0);
                        break;
                    case Slice.RightTop:
                        opposite = prePos;
                        break;
                    case Slice.LeftBottom:
                        opposite = prePos + preSize;
                        break;
                    case Slice.RightBottom:
                        opposite = prePos + new Vector2(0, preSize.y);
                        break;
                    default:
                        opposite = Vector2.zero;
                        break;
                }
                drag.Subscribe(mouse =>
                {
                    ResizeActiveLabel(opposite, mouse);
                }, onComplete).AddTo(this);
                break;
            //VerticalEdge
            case Slice.Left:
            case Slice.Right:
                float x = prePos.x;
                if (slice == Slice.Left)
                    x += preSize.x;
                var py = prePos.y;
                drag.Subscribe(mouse =>
                {
                    ResizeActiveLabel(new Vector2(x, py), new Vector2(mouse.x, py + preSize.y));
                }, onComplete).AddTo(this);
                break;
            //HorizontalEdge
            case Slice.Top:
            case Slice.Bottom:
                var px = prePos.x;
                float y = prePos.y;
                if (slice == Slice.Bottom)
                    y += preSize.y;
                drag.Subscribe(mouse =>
                {
                    ResizeActiveLabel(new Vector2(px, y), new Vector2(px + preSize.x, mouse.y));
                }, onComplete).AddTo(this);
                break;
            //Center
            case Slice.Center:
                drag.Subscribe(
                    mouse =>
                    {
                        var size = preSize;
                        var pos = (Vector2)mouse - size / 2;
                        prePos = RectClamp(pos);
                        ResizeActiveLabel(prePos, pos + preSize);
                    }, onComplete).AddTo(this);
                break;
            default:
                break;
        }

    }
    void ResizeActiveLabel(Vector2 startPos, Vector2 endPos)
    {
        endPos = RectClamp(endPos);
        float x1, x2;
        if (startPos.x > endPos.x)
        {
            x1 = endPos.x;
            x2 = startPos.x;
        }
        else
        {
            x1 = startPos.x;
            x2 = endPos.x;
        }
        float y1, y2;
        if (startPos.y > endPos.y)
        {
            y1 = endPos.y;
            y2 = startPos.y;
        }
        else
        {
            y1 = startPos.y;
            y2 = endPos.y;
        }
        var position = new Vector2(x1, y1);
        ActiveLabel.SetPosition(position);
        ActiveLabel.SetSize(new Vector2(x2, y2) - position);

    }

    public void DrawLabel(Vector2 start)
    {
        start = RectClamp(start);
        ActiveLabel = CreateLabelObject(start);
        ActiveLabel.SetRimScale(_RimWidth);
        _InteractManager.OnUpdate_Mouse
            .TakeUntil(_InteractManager.OnMouseUp(0))
            .Subscribe(pos =>
            {
                ResizeActiveLabel(start, pos);
            },
            () =>
            {
                if (!ActiveLabel.IsValidSize())
                {
                    DisposedLabelObjects.Enqueue(ActiveLabel);
                    EmptyIndexes[ActiveLabel.ClassId].Enqueue(ActiveLabel.LabelId);
                    ActiveLabel.transform.SetParent(transform_Disposed);
                    ActiveLabel.Dispose();
                    _InteractManager.SetDefault();
                }
                else
                    ActiveLabel.ConfirmCreate();

                _InteractManager.SetDefault();
            }
            ).AddTo(this);
    }

    public void FocusActiveLabel()
    {
        foreach (var label in LabelObjects.SelectMany(_ => _).Where(l => l != null))
            label.ChangeColor_Gray();
        ActiveLabel.ChangeColor_Default();
    }
    public void DefaultColorAll()
    {
        foreach (var label in LabelObjects.SelectMany(_ => _).Where(l => l != null))
            label.ChangeColor_Default();
    }
    public void SetFlameScaleAll(float scale)
    {
        _RimWidth = FLAME_SCALE / scale;
        foreach (var label in LabelObjects.SelectMany(_ => _).Where(l => l != null))
            label.SetRimScale(_RimWidth);
    }

    //関数
    public LabelObject GetNearestLabel(Vector2 mousePos)
    {
        LabelObject near = null;
        float? min = null;

        for (int i = LabelObjects.Length - 1; i >= 0; i--)
        {
            for (int j = LabelObjects[i].Count - 1; j >= 0; j--)
            {
                var label = LabelObjects[i][j];
                if (label == null)
                    continue;
                var distance = label.GetDistance(mousePos);
                if (distance == null)
                    continue;
                if (!(min < distance))
                {
                    min = distance;
                    near = label;
                }
            }
        }
        return near;
    }
    public Vector2 RectClamp(Vector2 position)
    {
        var halfSize = ImageSize / 2;
        var halfx = halfSize.x;
        var halfy = halfSize.y;
        return new Vector2(Mathf.Clamp(position.x, -halfx, halfx), Mathf.Clamp(position.y, -halfy, halfy));
    }
    int GetEmptyIndex(int classId)
    {
        var queue = EmptyIndexes[classId];

        int index;
        if (queue.Count > 0)
            index = queue.Peek();
        else
            index = LabelObjects[classId].Count;
        return index;
    }

    public enum Slice
    {
        LeftTop,
        Top,
        RightTop,
        Left,
        Center,
        Right,
        LeftBottom,
        Bottom,
        RightBottom,
        None,
    }
}
