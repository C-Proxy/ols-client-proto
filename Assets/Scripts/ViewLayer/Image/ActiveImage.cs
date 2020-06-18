using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class ActiveImage : MonoBehaviour
{
    [SerializeField] Image _Image = default;
    [SerializeField] RectTransform _RectTransform = default;

    Subject<Vector2> SizeChangeSubject = new Subject<Vector2>();
    public IObservable<Vector2> OnSizeChange => SizeChangeSubject;

    public void Set(Sprite sprite)
    {
        var size = sprite.rect.size;
        _RectTransform.sizeDelta = size;
        _Image.sprite = sprite;
        SizeChangeSubject.OnNext(size);
    }

    public Vector2 GetLocalPosition(Vector3 position)
    {
        var pos = new Vector2();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_RectTransform, position, null, out pos);
        pos = new Vector2((int)pos.x, (int)pos.y);
        return pos;
    }

}
