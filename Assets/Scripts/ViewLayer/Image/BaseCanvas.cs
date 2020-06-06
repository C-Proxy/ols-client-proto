using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class BaseCanvas : MonoBehaviour
{
    [SerializeField]
    RectTransform _RectTransform;
    [SerializeField]
    InteractManager _InteractManager;

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
        OnChangedScale.Subscribe(scale => Resize(scale)).AddTo(this);
    }
    public void Init()
    {
        _InteractManager.OnMouseScrolled
            .Subscribe(scroll => Scale *= 1 + scroll).AddTo(this);
        _InteractManager.OnMouseDrag(2)
            .CombineLatest(_InteractManager.OnMouseDown(2).Select(pos => _RectTransform.position - Input.mousePosition), (pos1, pos2) => pos2 + Input.mousePosition)
            .Subscribe(pos => _RectTransform.position = pos).AddTo(this);
    }

    public void Resize(float scale) => _RectTransform.localScale = Vector3.one * scale;
    public void PositionReset() => _RectTransform.anchoredPosition = Vector2.zero;
}
