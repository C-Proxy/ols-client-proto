using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class GammaSlider : MonoBehaviour
{
    [SerializeField] Material _Material = default;
    [SerializeField] Slider _Slider = default;

    private void Awake()
    {
        _Slider.OnValueChangedAsObservable()
        .Subscribe(value => _Material.SetFloat("_Gamma", value)).AddTo(this);
    }
    public void SetValue(float value) => _Slider.value = value;

}
