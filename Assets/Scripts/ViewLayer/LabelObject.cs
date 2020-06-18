using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
public class LabelObject : MonoBehaviour
{
    [SerializeField] Image _FillImage = default;
    [SerializeField] RectTransform _RectTransform = default;
    [SerializeField] LabelRim _LabelRim;

    #region Rx

    Subject<(Vector2 Position, Vector2 Size)> CreateSubject;
    Subject<(Vector2 Position, Vector2 Size)> ResizeSubject;
    Subject<Unit> RemoveSubject;
    Subject<int> ChangeClassSubject;

    public IObservable<(Vector2 Position, Vector2 Size)> OnCreated => CreateSubject;
    public IObservable<(Vector2 Position, Vector2 Size)> OnResized => ResizeSubject;
    public IObservable<Unit> OnRemove => RemoveSubject;
    public IObservable<int> OnChangedClass => ChangeClassSubject;

    public void ConfirmCreate() => CreateSubject.OnNext((localPosition, sizeDelta));
    public void ConfirmResize() => ResizeSubject.OnNext((localPosition, sizeDelta));
    public void Remove() => RemoveSubject.OnNext(Unit.Default);
    public void ChangeClass(int newClassId) => ChangeClassSubject.OnNext(newClassId);

    #endregion

    public int ClassId { get; private set; }
    public int LabelId { get; private set; }
    public Vector2 sizeDelta
    {
        get { return _RectTransform.sizeDelta; }
        private set { _RectTransform.sizeDelta = value; }
    }
    public Vector2 localPosition
    {
        get { return _RectTransform.localPosition; }
        private set { _RectTransform.localPosition = value; }
    }
    Color Color;
    Color GrayColor => new Color(Color.r / 2, Color.g / 2, Color.b / 2);
    Color AlphaColor => new Color(Color.r, Color.g, Color.b, _FillImage.color.a);

    public void SetSize(Vector2 size) => sizeDelta = size;
    public void SetPosition(Vector2 position) => localPosition = position;
    void SetColor(Color color) => Color = color;
    public void Init(Vector2 position, Vector2 size, int classId, int labelId)
    {
        SetPosition(position);
        SetSize(size);
        ClassId = classId;
        LabelId = labelId;
        SetColor(ColorPalatte.GetColor(classId));
        ChangeColor_Default();
        SetActivate(false);
        CreateSubject = new Subject<(Vector2 Position, Vector2 Size)>();
        ResizeSubject = new Subject<(Vector2 Position, Vector2 Size)>();
        RemoveSubject = new Subject<Unit>();
        ChangeClassSubject = new Subject<int>();
    }
    public void ChangeColor_Default()
    {
        _LabelRim.SetColor(Color);
        _FillImage.color = AlphaColor;
    }
    public void ChangeColor_Gray()
    {
        _LabelRim.SetColor(GrayColor);
    }
    public void SetActivate(bool active) => _FillImage.enabled = active;

    public void SetRimScale(float scale) => _LabelRim.SetRimScale(scale);


    public float? GetDistance(Vector2 mousePos)
    {
        var min = localPosition;
        var max = min + sizeDelta;

        var d = new float[] { max.x - mousePos.x, max.y - mousePos.y, mousePos.x - min.x, mousePos.y - min.y }.Min();
        if (d > 0)
            return d;
        else
            return null;
    }
    public bool IsContainsMouse(Vector2 mousePos)
    {
        var min = localPosition;
        var max = min + sizeDelta;

        return (min.x <= mousePos.x) && (max.x >= mousePos.x) && (min.y <= mousePos.y) && (max.y >= mousePos.y);
    }
    public bool IsValidSize()
    {
        float minLength = 4;
        float minArea = 20;

        var x = sizeDelta.x;
        var y = sizeDelta.y;

        var area = x * y;

        return (x > minLength) && (y > minLength) && (area > minArea);

    }

    public void Dispose()
    {
        gameObject.SetActive(false);
        CreateSubject.Dispose();
        ResizeSubject.Dispose();
        RemoveSubject.Dispose();
        ChangeClassSubject.Dispose();
    }
    public void Restore()
    {
        gameObject.SetActive(true);
    }
}
