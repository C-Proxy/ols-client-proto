using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using System;

public class LabelEditor : MonoBehaviour
{
    [SerializeField] CanvasGroup _CanvasGroup = default;
    RectTransform _RectTransform;
    [SerializeField] Texture2D[] CursorTextures = default;

    Subject<int> DragSubject = new Subject<int>();
    public IObservable<int> OnBeginDrag => DragSubject;

    Vector2 sizeDelta => _RectTransform.sizeDelta;
    Vector2 localPosition => _RectTransform.localPosition;

    public void SetParentLabel(LabelObject label)
    {
        transform.SetParent(label.transform, false);
        _RectTransform = label.GetComponent<RectTransform>();
    }
    public void SetActive(bool active) => _CanvasGroup.blocksRaycasts = _CanvasGroup.interactable = active;
    public Texture2D GetCursorTexture(Vector2 mousePos)
    {
        var size = sizeDelta;
        var tenthSize = sizeDelta * 0.1f;
        var pos1 = localPosition;
        var pos2 = localPosition + tenthSize;
        var pos4 = localPosition + size;
        var pos3 = pos4 - tenthSize;

        Slice s1 = Slice.Center;
        if ((mousePos.x < pos1.x) || (pos4.x < mousePos.x))
            s1 = Slice.None;
        else if (mousePos.x < pos2.x)
            s1 = Slice.Left;
        else if (pos3.x < mousePos.x)
            s1 = Slice.Right;
        Slice s2 = Slice.Center;
        if ((mousePos.y < pos1.y) || (pos4.y < mousePos.y))
            s2 = Slice.None;
        else if (mousePos.y < pos2.y)
            s2 = Slice.Bottom;
        else if (pos3.y < mousePos.y)
            s2 = Slice.Top;

        Texture2D texture = null;
        switch (s1)
        {
            case Slice.Left:
                switch (s2)
                {
                    case Slice.Top:
                        texture = CursorTextures[3];
                        break;
                    case Slice.Center:
                        texture = CursorTextures[2];
                        break;
                    case Slice.Bottom:
                        texture = CursorTextures[1];
                        break;
                }
                break;
            case Slice.Center:
                switch (s2)
                {
                    case Slice.Center:
                        texture = CursorTextures[4];
                        break;
                    case Slice.Top:
                    case Slice.Bottom:
                        texture = CursorTextures[0];
                        break;
                }
                break;
            case Slice.Right:
                switch (s2)
                {
                    case Slice.Top:
                        texture = CursorTextures[1];
                        break;
                    case Slice.Center:
                        texture = CursorTextures[2];
                        break;
                    case Slice.Bottom:
                        texture = CursorTextures[3];
                        break;
                }
                break;
        }
        return texture;
    }

    public void OnDrag(int value) => DragSubject.OnNext(value);

    public enum Slice
    {
        Top,
        Left,
        Right,
        Bottom,
        Center,
        None,
    }
}
