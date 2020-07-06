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
    [SerializeField] ActiveImage _ActiveImage = default;
    [SerializeField] LabelObjectManager _LabelObjectManager = default;
    [SerializeField] ClassWindow _ClassWindow = default;
    [SerializeField] EditWindow _EditWindow = default;
    [SerializeField] LabelEditor _LabelEditor = default;

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

    bool isEnable_Undo;
    bool isEnable_Redo;
    bool isEnable_Delete;

    bool isInteractable;

    public void EnableButton_Undo(bool enable)
    {
        isEnable_Undo = enable;
        _EditWindow.EnableButton_Undo(enable);
    }
    public void EnableButton_Redo(bool enable)
    {
        isEnable_Redo = enable;
        _EditWindow.EnableButton_Redo(enable);
    }
    public void EnableButton_Delete(bool enable)
    {
        isEnable_Delete = enable;
        _EditWindow.EnableButton_Delete(enable);
    }

    #region  InputObserbables
    public IObservable<Unit> OnInputUndo;
    public IObservable<Unit> OnInputRedo;
    public IObservable<Unit> OnInputDelete;
    public IObservable<int> OnInputNumber;
    public IObservable<Arrow> OnInputArrowKey;

    public IObservable<Unit> OnInputCtrl;
    public IObservable<Unit> OnInputShift;
    public IObservable<Unit> OnInputCtrlAndShift;
    public IObservable<Vector3> OnUpdate_InputPosition;
    public IObservable<Vector3> OnUpdate_CanvasPosition;
    IObservable<Unit> OnUpdate_Unit;

    public IObservable<Vector3>[] OnMouseUp = new IObservable<Vector3>[3];
    public IObservable<Vector3>[] OnMouseDown = new IObservable<Vector3>[3];
    public IObservable<float> OnMouseScroll;
    public IObservable<Vector3>[] OnMouseDragDelta = new IObservable<Vector3>[3];
    public IObservable<(Vector3 Current, Vector3 Start)>[] OnMouseDownMove_Canvas = new IObservable<(Vector3 Current, Vector3 Start)>[3];
    public IObservable<(Vector3 Current, Vector3 Start)>[] OnMouseDrag_Canvas = new IObservable<(Vector3 Current, Vector3 Start)>[3];

    public enum Arrow
    {
        Up, Down, Right, Left, None
    }
    #endregion
    void Awake()
    {
        OnUpdate_Unit = this.UpdateAsObservable().Where(_ => isInteractable).Publish().RefCount();
        OnUpdate_InputPosition = OnUpdate_Unit.Select(_ => Input.mousePosition).Publish().RefCount();
        OnUpdate_CanvasPosition = OnUpdate_InputPosition
            .Select(pos => (Vector3)_ActiveImage.GetLocalPosition(pos))
            .Publish().RefCount();

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
                        EnableButton_Delete(false);
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
                        OnUpdate_CanvasPosition
                            .TakeWhile(_ => Mode == State.Edit)
                            .Where(_ => Input.GetMouseButton(0) == false)
                            .Subscribe(mouse => Cursor.SetCursor(_LabelEditor.GetCursorTexture(mouse), Vector2.one * 16, CursorMode.Auto),
                            () => Cursor.SetCursor(null, Vector2.one * 16, CursorMode.Auto)
                            ).AddTo(this);
                        EnableButton_Delete(true);
                        break;
                }
            }).AddTo(this);

        #region InputKeyCommands

        OnInputCtrl = OnUpdate_Unit.Where(_ => !GetShiftKey() && GetCmdKey() == true).Publish().RefCount();
        OnInputShift = OnUpdate_Unit.Where(_ => !GetCmdKey() && GetShiftKey() == true).Publish().RefCount();
        OnInputCtrlAndShift = OnUpdate_Unit.Where(_ => GetCmdKey() && GetShiftKey() == true).Publish().RefCount();

        OnInputUndo = OnPressKeyWithCmd(KeyCode.Z).Where(_ => isEnable_Undo).Publish().RefCount();
        OnInputRedo = Observable.Merge(OnPressKeyWithCmd(KeyCode.Y), OnPressKeyWithCmdShift(KeyCode.Z)).Where(_ => isEnable_Redo).Publish().RefCount();
        OnInputDelete = OnPressKey(KeyCode.Delete).Where(_ => isEnable_Delete).Publish().RefCount();

        OnInputNumber = OnUpdate_Unit
            .Select(_ =>
            {
                for (int i = 0; i < 10; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                        return i;
                }
                return -1;
            })
            .Where(value => value >= 0)
            .Publish().RefCount();
        OnInputArrowKey = OnUpdate_Unit
            .Select(_ =>
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow + i))
                        return (Arrow)i;
                }
                return Arrow.None;
            })
            .Where(arrow => arrow != Arrow.None)
            .Publish().RefCount();
        for (int i = 0; i < 3; i++)
        {
            var value = i;
            OnMouseUp[i] = OnUpdate_CanvasPosition.Where(_ => Input.GetMouseButtonUp(value)).Publish().RefCount();

            OnMouseDown[i] = OnUpdate_CanvasPosition.Where(_ => Input.GetMouseButtonDown(value)).Publish().RefCount();

            OnMouseDownMove_Canvas[i] = OnUpdate_CanvasPosition.CombineLatest(OnMouseDown[value], (current, start) => (current, start)).Publish().RefCount();

            OnMouseDrag_Canvas[i] = OnMouseDownMove_Canvas[i]
                .TakeUntil(OnMouseUp[value])
                .Where(tuple => (tuple.Current - tuple.Start).sqrMagnitude >= SQR_DRAG_DISTANCE)
                .RepeatUntilDestroy(this)
                .Publish().RefCount();

            OnMouseDragDelta[i] = OnUpdate_InputPosition
                .SkipUntil(OnMouseDown[value])
                .TakeUntil(OnMouseUp[value])
                .Pairwise()
                .Select(pair => pair.Current - pair.Previous)
                .RepeatUntilDestroy(this)
                .Publish().RefCount();
        }
        OnMouseScroll = OnUpdate_Unit
            .Select(_ => Input.GetAxis("Mouse ScrollWheel"))
            .Where(scroll => scroll != 0)
            .Publish().RefCount();

        #endregion
        Mode = State.Default;

        OnInputDelete
            .Subscribe(_ =>
            {
                ActiveLabel.Remove();
                Mode = State.Default;
            }).AddTo(this);
    }
    public void Init()
    {
        OnUpdate_CanvasPosition
            .Where(_ => Mode == State.Default)
            .Subscribe(mouse => ActiveLabel = _LabelObjectManager.GetNearestLabel(mouse)).AddTo(this);

        _ClassWindow.OnAcitveElementChanged
            .Where(_ => Mode == State.Edit)
            .Subscribe(classId => ActiveLabel.ChangeClass(classId)).AddTo(this);
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

    enum State
    {
        Default,
        Draw,
        Edit,
    }
    public void SetDefault() => Mode = State.Default;
    public void SetActiveLabel(LabelObject labelObject) => ActiveLabel = labelObject;
    public void SetInteractable(bool interactable) => isInteractable = interactable;

    IObservable<Unit> OnPressKey(KeyCode key) => OnUpdate_Unit.Where(_ => Input.GetKeyDown(key) == true);
    IObservable<Unit> OnPressKeyWithCmd(KeyCode key) => OnInputCtrl.Where(_ => Input.GetKeyDown(key));
    IObservable<Unit> OnPressKeyWithCmdShift(KeyCode key) => OnInputCtrlAndShift.Where(_ => Input.GetKeyDown(key));

    bool GetCmdKey() => Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.RightControl);
    bool GetShiftKey() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
}