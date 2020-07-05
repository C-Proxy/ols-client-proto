using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using UniRx.Diagnostics;

public class BaseCanvas : MonoBehaviour
{
    [SerializeField] RectTransform _RectTransform = default;
    [SerializeField] InteractManager _InteractManager = default;

    const float maxScale = 20f;
    const float minScale = 0.1f;
    FloatReactiveProperty _Scale = new FloatReactiveProperty(1);
    float Scale
    {
        get
        {
            return _Scale.Value;
        }
        set
        {
            _Scale.Value = Mathf.Clamp(value, minScale, maxScale);
        }
    }
    public IObservable<float> OnChangedScale => _Scale;

    private void Awake()
    {
        OnChangedScale.Subscribe(scale => _RectTransform.localScale = Vector3.one * scale).AddTo(this);
    }
    public void Init()
    {
        _InteractManager.OnMouseScroll
            .Subscribe(scroll => Scale *= 1 + scroll).AddTo(this);
        _InteractManager.OnMouseDragDelta[2]
            .Subscribe(delta => _RectTransform.position += delta).AddTo(this);
    }
    public void SetScale(float scale) => Scale = scale;
    public void PositionReset() => _RectTransform.anchoredPosition = Vector2.zero;
}
