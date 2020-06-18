using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class EditView : MonoBehaviour
{
    [SerializeField] LabelObjectManager _LabelObjectManager = default;
    [SerializeField] FileWindow _FileWindow = default;
    [SerializeField] ClassWindow _ClassWindow = default;
    [SerializeField] ActiveImage _ActiveImage = default;
    [SerializeField] BaseCanvas _BaseCanvas = default;
    [SerializeField] InteractManager _InteractManager = default;
    [SerializeField] EditWindow _EditWindow = default;
    [SerializeField] GammaSlider _GammaSlider = default;

    private void Awake()
    {
        OnCallUndo = Observable.Merge(_InteractManager.OnInputUndo, _EditWindow.OnClick_Undo);
        OnCallRedo = Observable.Merge(_InteractManager.OnInputRedo, _EditWindow.OnClick_Redo);
        OnCallDelete = Observable.Merge(_InteractManager.OnInputDelete, _EditWindow.OnClick_Delete);

        Observable.Merge(_InteractManager.OnInputArrowKey.Where(arrow => arrow == InteractManager.Arrow.Left).Select(_ => Unit.Default), _FileWindow.OnCkick_Back)
        .Subscribe(_ => _FileWindow.Back()).AddTo(this);
        Observable.Merge(_InteractManager.OnInputArrowKey.Where(arrow => arrow == InteractManager.Arrow.Right).Select(_ => Unit.Default), _FileWindow.OnClick_Next)
        .Subscribe(_ => _FileWindow.Next()).AddTo(this);

        _InteractManager.OnInputNumber
        .Subscribe(index => _ClassWindow.SelectElement(index != 0 ? index - 1 : 10)).AddTo(this);
    }
    public void Init()
    {
        _InteractManager.Init();
        _LabelObjectManager.Init();
        _BaseCanvas.Init();
        _FileWindow.SendValue();
    }

    public IObservable<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> OnCreateLabel => _LabelObjectManager.OnCreateLabel;
    public IObservable<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> OnResizeLabel => _LabelObjectManager.OnResizeLabel;
    public IObservable<(int ClassId, int LabelId)> OnRemoveLabel => _LabelObjectManager.OnRemoveLabel;
    public IObservable<(int ClassId, int LabelId, int NewClassId, int NewLabelId)> OnChangeClass => _LabelObjectManager.OnChangeClass;

    public IObservable<Unit> OnCallUndo;
    public IObservable<Unit> OnCallRedo;
    public IObservable<Unit> OnCallDelete;

    public IObservable<string> OnSendFileName => _FileWindow.OnSendValue;

    public void SetClassNames(string[] names)
    {
        _ClassWindow.Set(names);
        _LabelObjectManager.SetLength(names.Length);
    }
    public void SetLabels((Vector2 Position, Vector2 Size)[][] labelInfos)
    {
        _LabelObjectManager.RefreshLabels();
        for (int i = labelInfos.Length - 1; i >= 0; i--)
            _LabelObjectManager.CreateLabelObject(i, labelInfos[i]);
    }
    public void SetImage(Texture2D texture)
    {
        var width = texture.width;
        var hight = texture.height;
        var imageSize = new Vector2(width, hight);
        var sprite = Sprite.Create(texture, new Rect(Vector2.zero, imageSize), Vector2.zero);
        _ActiveImage.Set(sprite);
        float xRatio = (float)Screen.width / width;
        float yRatio = (float)Screen.height / hight;
        _BaseCanvas.Resize(xRatio > yRatio ? yRatio : xRatio);
        _BaseCanvas.PositionReset();
        _InteractManager.SetDefault();
        _LabelObjectManager.SetImageSize(imageSize);
        _GammaSlider.SetValue(1.0f);
    }
    public void SetFileNames(List<string> fileNames)
    {
        _FileWindow.Set(fileNames);
    }

    public void CreateLabel(int classId, int labelId, Vector2 position, Vector2 size)
    {
        Debug.Log($"Set-ClassID:{classId},LabelID:{labelId} -Log");
        _LabelObjectManager.CreateLabelObject(classId, labelId, position, size);
    }
    public void ResizeLabel(int classId, int labelId, Vector2 position, Vector2 size)
    {
        Debug.Log($"Resize-ClassID:{classId},LabelID:{labelId},NewPosition:{position},NewSize:{size} -Log");
        _LabelObjectManager.ResizeLabelObject(classId, labelId, position, size);
    }
    public void RemoveLabel(int classId, int labelId)
    {
        Debug.Log($"Remove-ClassID:{classId},LabelID:{labelId}");
        _LabelObjectManager.RemoveLabelObject(classId, labelId);
    }
    public void ChangeClass(int classId, int labelId, int newClassId, int newLabelId)
    {
        Debug.Log($"ChangeClass-ClassID:{classId},LabelID:{labelId},newClassID:{newClassId},newLabelID:{newLabelId}");
        var label = _LabelObjectManager.ChangeClassLabelObject(classId, labelId, newClassId, newLabelId);
        _InteractManager.SetActiveLabel(label);
    }
    public void EnableButton_Undo(bool enable)
    {
        _InteractManager.EnableButton_Undo(enable);
    }
    public void EnableButton_Redo(bool enable)
    {
        _InteractManager.EnableButton_Redo(enable);
    }
}
