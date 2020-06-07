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
    [SerializeField]
    EditWindow _EditWindow;

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
    public IObservable<Tuple<Vector3, float>> OnMouseDownMoved(int value) =>
        OnUpdate_Mouse.CombineLatest(OnMouseDown(value), (pos, start) => new Tuple<Vector3, float>(pos, (pos - start).sqrMagnitude));
    public IObservable<Vector3> OnMouseDragBegin(int value) =>
        OnMouseDownMoved(value)
            .TakeUntil(OnMouseUp(value))
            .Where(tuple => tuple.Item2 >= SQR_DRAG_DISTANCE)
            .Select(tuple => tuple.Item1)
            .Take(1)
            .RepeatUntilDestroy(this);
    public IObservable<Vector3> OnMouseDrag(int value) =>
        OnUpdate_Mouse
            .SkipUntil(OnMouseDragBegin(value))
            .TakeUntil(OnMouseUp(value))
            .RepeatUntilDestroy(this);

    #region
    BoolReactiveProperty _isEnable_Undo = new BoolReactiveProperty();
    BoolReactiveProperty _isEnable_Redo = new BoolReactiveProperty();
    BoolReactiveProperty _isEnable_Delete = new BoolReactiveProperty();
    bool isEnable_Undo
    {
        get { return _isEnable_Undo.Value; }
        set { _isEnable_Undo.Value = value; }
    }
    bool isEnable_Redo
    {
        get { return _isEnable_Redo.Value; }
        set { _isEnable_Redo.Value = value; }
    }
    bool isEnable_Delete
    {
        get { return _isEnable_Delete.Value; }
        set { _isEnable_Delete.Value = value; }
    }
    public void EnableButton_Undo(bool enable) => isEnable_Undo = enable;
    public void EnableButton_Redo(bool enable) => isEnable_Redo = enable;
    public void EnableButton_Delete(bool enable) => isEnable_Delete = enable;

    public IObservable<Unit> OnCallUndo;
    public IObservable<Unit> OnCallRedo;
    public IObservable<Unit> OnCallDelete;
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
        OnCallUndo = Observable.Merge(_EditWindow.OnClick_Undo, OnPressKeyWithCmd(KeyCode.Z)).Where(_ => isEnable_Undo);
        OnCallRedo = Observable.Merge(_EditWindow.OnClick_Redo, OnPressKeyWithCmd(KeyCode.Y), OnPressKeysWithCmd(KeyCode.LeftShift, KeyCode.Z)).Where(_ => isEnable_Redo);
        OnCallDelete = Observable.Merge(_EditWindow.OnClick_Delete, OnPressKey(KeyCode.Delete)).Where(_ => isEnable_Delete);
        #endregion
        Mode = State.Default;

        OnCallDelete
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

        _isEnable_Undo.Subscribe(enabled => _EditWindow.EnableButton_Undo(enabled)).AddTo(this);
        _isEnable_Redo.Subscribe(enabled => _EditWindow.EnableButton_Redo(enabled)).AddTo(this);
        _isEnable_Delete.Subscribe(enabled => _EditWindow.EnableButton_Delete(enabled)).AddTo(this);

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
    .Select(_ => Input.GetKey(key1))
    .SkipWhile(input => !input)
    .TakeWhile(input => input)
    .Where(_ => Input.GetKeyDown(key2))
    .Select(_ => Unit.Default)
    .RepeatUntilDestroy(this);
    public IObservable<Unit> OnPressKeyWithCmd(KeyCode key) =>
    OnUpdate_Unit
    .Select(_ => GetCmdKey())
    .SkipWhile(input => !input)
    .TakeWhile(input => input)
    .Where(_ => Input.GetKeyDown(key))
    .Select(_ => Unit.Default)
    .RepeatUntilDestroy(this);
    public IObservable<Unit> OnPressKeysWithCmd(KeyCode key1, KeyCode key2) =>
    OnUpdate_Unit
    .Select(_ => GetCmdKey() && Input.GetKey(key1))
    .SkipWhile(input => !input)
    .TakeWhile(input => input)
    .Where(_ => Input.GetKeyDown(key2))
    .Select(_ => Unit.Default)
    .RepeatUntilDestroy(this);

    bool GetCmdKey() => Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl);
}