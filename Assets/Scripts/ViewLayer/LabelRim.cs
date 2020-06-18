using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LabelRim : MonoBehaviour
{
    [SerializeField] Image _TopRim = default;
    [SerializeField] Image _BottomRim = default;
    [SerializeField] Image _LeftRim = default;
    [SerializeField] Image _RightRim = default;
    [SerializeField] RectTransform _ImageRect = default;

    RectTransform _TopRT, _BottomRT, _LeftRT, _RightRT;

    private void Awake()
    {
        _TopRT = _TopRim.GetComponent<RectTransform>();
        _BottomRT = _BottomRim.GetComponent<RectTransform>();
        _LeftRT = _LeftRim.GetComponent<RectTransform>();
        _RightRT = _RightRim.GetComponent<RectTransform>();
    }

    public void SetRimScale(float width)
    {
        var size = _ImageRect.sizeDelta;
        _TopRT.sizeDelta = _BottomRT.sizeDelta = Vector2.right * size.x + Vector2.one * width;
        _LeftRT.sizeDelta = _RightRT.sizeDelta = Vector2.up * size.y + Vector2.one * width;
    }
    public void SetColor(Color color) => _TopRim.color = _BottomRim.color = _LeftRim.color = _RightRim.color = color;
}
