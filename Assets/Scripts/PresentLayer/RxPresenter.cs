using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;

public class RxPresenter : MonoBehaviour
{
    [SerializeField]
    LabelObjectManager _LabelObjectManager;
    [SerializeField]
    FileWindow _FileWindow;
    [SerializeField]
    ClassWindow _ClassWindow;
    [SerializeField]
    ActiveImage _ActiveImage;
    [SerializeField]
    BaseCanvas _BaseCanvas;
    [SerializeField]
    AnnotationModel _AnnotationModel;
    [SerializeField]
    InteractManager _InteractManager;


    private void Start()
    {
        //LabelObjectManager to AnnotationModel
        _LabelObjectManager.OnCreatedLabel
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var pos = tuple.Position;
                var size = tuple.Size;
                Debug.Log($"Set-ClassID:{classId},LabelID:{labelId}");
                _AnnotationModel.CreateLabelInfoWithLog(classId, labelId, pos, size);
            }).AddTo(this);
        _LabelObjectManager.OnResizedLabel
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var position = tuple.Position;
                var size = tuple.Size;
                Debug.Log($"Resize-ClassID:{classId},LabelID:{labelId},NewPosition:{position},NewSize:{size}");
                _AnnotationModel.ResizeLabelInfoWithLog(classId, labelId, position, size);
            }).AddTo(this);
        _LabelObjectManager.OnRemoveLabel
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                Debug.Log($"Remove-ClassID:{classId},LabelID:{labelId}");
                _AnnotationModel.RemoveLabelInfoWithLog(classId, labelId);
                _LabelObjectManager.RemoveLabelObject(classId, labelId);
            }).AddTo(this);
        _LabelObjectManager.OnChangedClass
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var newClassId = tuple.newClassId;
                var newLabelId = tuple.newLabelId;
                Debug.Log($"ChangeClass-ClassID:{classId},LabelID:{labelId},newClassID:{newClassId},newLabelID:{newLabelId}");
                _AnnotationModel.ChangeClassLabelInfoWithLog(classId, labelId, newClassId, newLabelId);
                var newLabel = _LabelObjectManager.ChangeClassLabelObject(classId, labelId, newClassId, newLabelId);
                _InteractManager.SetActiveLabel(newLabel);
            }).AddTo(this);

        //InteractManager to AM
        _InteractManager.OnCallUndo
            .Subscribe(_ => _AnnotationModel.Undo()).AddTo(this);
        _InteractManager.OnCallRedo
            .Subscribe(_ => _AnnotationModel.Redo()).AddTo(this);

        //FileWindow to AnnotationModel 
        _FileWindow.OnSendValue
            .Pairwise()
            .Subscribe(fileNames =>
            {
                Debug.Log($"Open-Prevous:{fileNames.Previous},Current:{fileNames.Current}");
                _AnnotationModel.SaveLabels(fileNames.Previous);
                _AnnotationModel.Load(fileNames.Current);
            }).AddTo(this);


        //From AM
        _AnnotationModel.OnLoadClassNames
            .Subscribe(names =>
            {
                _ClassWindow.Set(names);
                _LabelObjectManager.SetLength(names.Length);
            }).AddTo(this);
        _AnnotationModel.OnLoadLabels
            .Subscribe(tuples =>
            {
                _LabelObjectManager.RefreshLabels();
                for (int i = tuples.Length - 1; i >= 0; i--)
                    _LabelObjectManager.CreateLabelObject(i, tuples[i]);
            }).AddTo(this);
        _AnnotationModel.OnLoadImage
            .Subscribe(texture =>
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
            }).AddTo(this);
        _AnnotationModel.OnLoadFiles
            .Subscribe(names => _FileWindow.Set(names));

        _AnnotationModel
            .OnCreated_Log
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var position = tuple.Position;
                var size = tuple.Size;
                Debug.Log($"Set-ClassID:{classId},LabelID:{labelId} -Log");
                _LabelObjectManager.CreateLabelObject(classId, labelId, position, size);
            }).AddTo(this);
        _AnnotationModel
            .OnResized_Log
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var position = tuple.Position;
                var size = tuple.Size;
                Debug.Log($"Resize-ClassID:{classId},LabelID:{labelId},NewPosition:{position},NewSize:{size} -Log");
                _LabelObjectManager.ResizeLabelObject(classId, labelId, position, size);
            }).AddTo(this);
        _AnnotationModel
            .OnRemoved_Log
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                Debug.Log($"Remove-ClassID:{classId},LabelID:{labelId} -Log");
                _LabelObjectManager.RemoveLabelObject(classId, labelId);
            }).AddTo(this);
        _AnnotationModel
            .OnChangedClass_Log
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var newClassId = tuple.NewClassId;
                var newLabelId = tuple.NewLabelId;
                Debug.Log($"ChangeClass-ClassID:{classId},LabelID:{labelId},newClassID:{newClassId},newLabelID:{newLabelId}");
                var label = _LabelObjectManager.ChangeClassLabelObject(classId, labelId, newClassId, newLabelId);
            }).AddTo(this);

        _AnnotationModel
            .OnUndoCountChanged
            .Subscribe(count => _InteractManager.EnableButton_Undo(count > 0)).AddTo(this);
        _AnnotationModel
            .OnRedoCountChanged
            .Subscribe(count => _InteractManager.EnableButton_Redo(count > 0)).AddTo(this);

        _AnnotationModel.LoadFileinfos();
        _AnnotationModel.LoadClassNames();

        _InteractManager.Init();
        _LabelObjectManager.Init();
        _BaseCanvas.Init();

        _FileWindow.SendValue();
        _AnnotationModel.FirstLoad();


    }
}

