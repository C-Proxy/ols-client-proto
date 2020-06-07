using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UniRx.Diagnostics;

public class InteractManager : MonoBehaviour
{
    [SerializeField]
    ActiveImage _ActiveImage;
    [SerializeField]
    LabelObjectManager _LabelObjectManager;
    [SerializeField]
    ClassWindow _ClassWindow;
    [SerializeField]
    LabelEditor _LabelEditor;

    const float SQR_DRAG_DISTANCE = 9;

    ReactiveProperty<State> _Mode = new ReactiveProperty<State>();
    [SerializeField]
    State Mode
    {
        get { return _Mode.Value; }
        set { _Mode.Value = value; }
    }
    IObservable<State> OnModeChange => _Mode;
    ReactiveProperty<LabelObject> _ActiveLabel = new ReactiveProperty<LabelObject>();
    LabelObject ActiveLabel
    {
        get { return _ActiveLabel.Value; }
        set { _ActiveLabel.Value = value; }
    }
    public IObservable<LabelObject> OnActiveLabelChange => _ActiveLabel;
    Vector3 DragStartPosition;

    public IObservable<Vector3> OnUpdate;
    public IObservable<Vector3> OnMouseUp(int value) =>
        OnUpdate.Where(_ => Input.GetMouseButtonUp(value));
    public IObservable<Vector3> OnMouseDown(int value) =>
        OnUpdate.Where(_ => Input.GetMouseButtonDown(value));
    public IObservable<float> OnMouseScrolled =>
        OnUpdate.Select(_ => Input.GetAxis("Mouse ScrollWheel"))
            .Where(scroll => scroll != 0);
    public IObservable<Tuple<Vector3, float>> OnMouseDownMoved(int value) =>
        OnUpdate.CombineLatest(OnMouseDown(value), (pos, start) => new Tuple<Vector3, float>(pos, (pos - start).sqrMagnitude));
    public IObservable<Vector3> OnMouseDragBegin(int value) =>
        OnMouseDownMoved(value)
            .TakeUntil(OnMouseUp(value))
            .Where(tuple => tuple.Item2 >= SQR_DRAG_DISTANCE)
            .Select(tuple => tuple.Item1)
            .Take(1)
            .RepeatUntilDestroy(this);
    public IObservable<Vector3> OnMouseDrag(int value) =>
        OnUpdate
            .SkipUntil(OnMouseDragBegin(value))
            .TakeUntil(OnMouseUp(value))
            .RepeatUntilDestroy(this);

    void Awake()
    {
        OnModeChange
            .Pairwise()
            .Subscribe(modes =>
            {
                switch (modes.Previous)
                {
                    case State.Default:
                        break;
                    case State.Draw:
                        _LabelEditor.SetActive(false);
                        break;
                    case State.Edit:
                        _LabelEditor.SetActive(false);
                        _LabelObjectManager.DefaultColorAll();
                        EnableButton_Remove(false);
                        break;
                }
                switch (modes.Current)
                {
                    case State.Default:
                        _LabelEditor.SetActive(false);
                        break;
                    case State.Draw:
                        if (ActiveLabel != null)
                            ActiveLabel.SetActivate(false);
                        _LabelEditor.SetActive(true);
                        _LabelObjectManager.DrawLabel(_ActiveImage.GetLocalPosition(DragStartPosition));
                        break;
                    case State.Edit:
                        _LabelEditor.SetActive(true);
                        _LabelObjectManager.FocusActiveLabel();
                        OnUpdate
                            .TakeWhile(_ => Mode == State.Edit)
                            .Where(_ => Input.GetMouseButton(0) == false)
                            .Subscribe(mouse => Cursor.SetCursor(_LabelEditor.GetCursorTexture(mouse), Vector2.one * 16, CursorMode.Auto),
                            () => Cursor.SetCursor(null, Vector2.one * 16, CursorMode.Auto)
                            ).AddTo(this);
                        EnableButton_Remove(true);
                        break;
                }
            }).AddTo(this);

        Mode = State.Default;

        _Button_Remove.OnClickAsObservable()
            .Subscribe(_ =>
            {
                ActiveLabel.Remove();
                Mode = State.Default;
            }).AddTo(this);
    }
    public void Init()
    {
        OnUpdate = this.UpdateAsObservable()
            .Select(_ => (Vector3)_ActiveImage.GetLocalPosition(Input.mousePosition));

        OnUpdate
            .Where(_ => Mode == State.Default)
            .Subscribe(mouse => ActiveLabel = _LabelObjectManager.GetNearestLabel(mouse)).AddTo(this);

        _ClassWindow.OnAcitveElementChanged
            .Where(_ => Mode == State.Edit)
            .Subscribe(classId =>
            {
                ActiveLabel.ChangeClass(classId);
            }).AddTo(this);
    }

    public void OnDragCanvas(BaseEventData data)
    {
        PointerEventData point = data as PointerEventData;
        if (point.pointerId != -1)
            return;
        DragStartPosition = point.pressPosition;
        Mode = State.Draw;
    }
    public void OnClickCanvas(BaseEventData data)
    {
        var point = data as PointerEventData;
        if (point.pointerId != -1)
            return;
        switch (Mode)
        {
            case State.Default:
                if (ActiveLabel == null)
                    return;
                _ClassWindow.ActivateElement(ActiveLabel.ClassId);
                Mode = State.Edit;
                break;
            case State.Draw:
                break;
            case State.Edit:
                if (!ActiveLabel.IsContainsMouse(_ActiveImage.GetLocalPosition(point.position)))
                {
                    Mode = State.Default;
                }
                break;
        }
    }

    //------------Buttons--------------//
    [SerializeField]
    Button _Button_Remove, _Button_Undo, _Button_Redo;

    public IObservable<Unit> OnClick_Undo => _Button_Undo.OnClickAsObservable();
    public IObservable<Unit> OnClick_Redo => _Button_Redo.OnClickAsObservable();

    public void EnableButton_Remove(bool enable) => _Button_Remove.interactable = enable;
    public void EnableButton_Undo(bool enable) => _Button_Undo.interactable = enable;
    public void EnableButton_Redo(bool enable) => _Button_Redo.interactable = enable;


    enum State
    {
        Default,
        Draw,
        Edit,
    }
    public void SetDefault() => Mode = State.Default;
    public void SetActiveLabel(LabelObject labelObject) => ActiveLabel = labelObject;
}