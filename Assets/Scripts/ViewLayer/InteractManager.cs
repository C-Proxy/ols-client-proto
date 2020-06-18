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
    EditWindow _EditWindow;
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

    public IObservable<Vector3> OnUpdate_Mouse;
    IObservable<Unit> OnUpdate_Unit;
    public IObservable<Vector3> OnMouseUp(int value) =>
        OnUpdate_Mouse.Where(_ => Input.GetMouseButtonUp(value));
    public IObservable<Vector3> OnMouseDown(int value) =>
        OnUpdate_Mouse.Where(_ => Input.GetMouseButtonDown(value));
    public IObservable<float> OnMouseScrolled =>
        OnUpdate_Mouse.Select(_ => Input.GetAxis("Mouse ScrollWheel"))
            .Where(scroll => scroll != 0);
    public IObservable<(Vector3 Current, Vector3 Start)> OnMouseDownMoved(int value) =>
        OnUpdate_Mouse.CombineLatest(OnMouseDown(value), (current, start) => (current, start));
    public IObservable<(Vector3 Current, Vector3 Start)> OnMouseDrag(int value) =>
        OnMouseDownMoved(value)
            .TakeUntil(OnMouseUp(value))
            .Where(tuple => (tuple.Current - tuple.Start).sqrMagnitude >= SQR_DRAG_DISTANCE)
            .RepeatUntilDestroy(this);

    bool isEnable_Undo;
    bool isEnable_Redo;
    bool isEnable_Delete;

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
    public IObservable<Unit> OnInputRight;

    public enum Arrow
    {
        Up, Down, Right, Left, None
    }
    #endregion
    void Awake()
    {
        OnUpdate_Unit = this.UpdateAsObservable();

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
                        OnUpdate_Mouse
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
        OnInputUndo = OnPressKeyWithCmd(KeyCode.Z).Where(_ => isEnable_Undo);
        OnInputRedo = Observable.Merge(OnPressKeyWithCmd(KeyCode.Y), OnPressKeyWithCmdShift(KeyCode.Z)).Where(_ => isEnable_Redo);
        OnInputDelete = OnPressKey(KeyCode.Delete).Where(_ => isEnable_Delete);

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
            .Where(value => value >= 0);

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
        .Where(arrow => arrow != Arrow.None);

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
        OnUpdate_Mouse = this.UpdateAsObservable()
            .Select(_ => (Vector3)_ActiveImage.GetLocalPosition(Input.mousePosition));

        OnUpdate_Mouse
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


    enum State
    {
        Default,
        Draw,
        Edit,
    }
    public void SetDefault() => Mode = State.Default;
    public void SetActiveLabel(LabelObject labelObject) => ActiveLabel = labelObject;

    IObservable<Unit> OnPressKey(KeyCode key) => OnUpdate_Unit.Where(_ => Input.GetKeyDown(key) == true);
    IObservable<Unit> OnPressKeys(KeyCode key1, KeyCode key2) =>
    OnUpdate_Unit
    .Where(_ => Input.GetKey(key1) && Input.GetKeyDown(key2));
    public IObservable<Unit> OnPressKeyWithCmd(KeyCode key) =>
    OnUpdate_Unit
    .Where(_ => GetCmdKey() && !GetShiftKey() && Input.GetKeyDown(key));
    public IObservable<Unit> OnPressKeyWithCmdShift(KeyCode key) =>
    OnUpdate_Unit
    .Where(_ => GetCmdKey() && GetShiftKey() && Input.GetKeyDown(key));

    bool GetCmdKey() => Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.RightControl);
    bool GetShiftKey() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
}