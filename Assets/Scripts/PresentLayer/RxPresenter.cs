using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;

public class RxPresenter : MonoBehaviour
{
    [SerializeField]
    EditView _EditView;
    [SerializeField]
    AnnotationModel _AnnotationModel;
    private void Start()
    {
        //LabelObjectManager to AnnotationModel
        _EditView.OnCreateLabel
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var pos = tuple.Position;
                var size = tuple.Size;
                Debug.Log($"Set-ClassID:{classId},LabelID:{labelId}");
                _AnnotationModel.CreateLabelInfoWithLog(classId, labelId, pos, size);
            }).AddTo(this);
        _EditView.OnResizeLabel
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var position = tuple.Position;
                var size = tuple.Size;
                Debug.Log($"Resize-ClassID:{classId},LabelID:{labelId},NewPosition:{position},NewSize:{size}");
                _AnnotationModel.ResizeLabelInfoWithLog(classId, labelId, position, size);
            }).AddTo(this);
        _EditView.OnRemoveLabel
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                //Debug.Log($"Remove-ClassID:{classId},LabelID:{labelId}");
                _AnnotationModel.RemoveLabelInfoWithLog(classId, labelId);
                _EditView.RemoveLabel(classId, labelId);
            }).AddTo(this);
        _EditView.OnChangeClass
            .Subscribe(tuple =>
            {
                var classId = tuple.ClassId;
                var labelId = tuple.LabelId;
                var newClassId = tuple.NewClassId;
                var newLabelId = tuple.NewLabelId;
                //Debug.Log($"ChangeClass-ClassID:{classId},LabelID:{labelId},newClassID:{newClassId},newLabelID:{newLabelId}");
                _AnnotationModel.ChangeClassLabelInfoWithLog(classId, labelId, newClassId, newLabelId);
                _EditView.ChangeClass(classId, labelId, newClassId, newLabelId);
            }).AddTo(this);

        //InteractManager to AM
        _EditView.OnCallUndo
            .Subscribe(_ => _AnnotationModel.Undo()).AddTo(this);
        _EditView.OnCallRedo
            .Subscribe(_ => _AnnotationModel.Redo()).AddTo(this);

        //FileWindow to AnnotationModel 
        _EditView.OnSendFileName
            .Pairwise()
            .Subscribe(fileNames =>
            {
                Debug.Log($"Open-Prevous:{fileNames.Previous},Current:{fileNames.Current}");
                _AnnotationModel.SaveLabels(fileNames.Previous);
                _AnnotationModel.Load(fileNames.Current);
            }).AddTo(this);


        //From AM
        _AnnotationModel.OnLoadClassNames
            .Subscribe(names => _EditView.SetClassNames(names)).AddTo(this);
        _AnnotationModel.OnLoadLabels
            .Subscribe(tuples => _EditView.SetLabels(tuples)).AddTo(this);
        _AnnotationModel.OnLoadImage
            .Subscribe(texture => _EditView.SetImage(texture)).AddTo(this);
        _AnnotationModel.OnLoadFiles
            .Subscribe(names => _EditView.SetFileNames(names)).AddTo(this);

        _AnnotationModel
            .OnCreated_Log
            .Subscribe(tuple => _EditView.CreateLabel(tuple.ClassId, tuple.LabelId, tuple.Position, tuple.Size)).AddTo(this);
        _AnnotationModel
            .OnResized_Log
            .Subscribe(tuple => _EditView.ResizeLabel(tuple.ClassId, tuple.LabelId, tuple.Position, tuple.Size)).AddTo(this);
        _AnnotationModel
            .OnRemoved_Log
            .Subscribe(tuple => _EditView.RemoveLabel(tuple.ClassId, tuple.LabelId)).AddTo(this);
        _AnnotationModel
            .OnChangedClass_Log
            .Subscribe(tuple => _EditView.ChangeClass(tuple.ClassId, tuple.LabelId, tuple.NewClassId, tuple.NewLabelId)).AddTo(this);

        _AnnotationModel
            .OnUndoCountChanged
            .Subscribe(count => _EditView.EnableButton_Undo(count > 0)).AddTo(this);
        _AnnotationModel
            .OnRedoCountChanged
            .Subscribe(count => _EditView.EnableButton_Redo(count > 0)).AddTo(this);

        _AnnotationModel.LoadFileinfos();
        _AnnotationModel.LoadClassNames();
        _EditView.Init();
        _AnnotationModel.FirstLoad();


    }
}

