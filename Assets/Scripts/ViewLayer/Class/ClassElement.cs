using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class ClassElement : MonoBehaviour
{
    [SerializeField] Button _Button = default;
    Text _ButtonText;
    [SerializeField] Image _CheckImage = default;

    public IObservable<Unit> OnClick => _Button.OnClickAsObservable();

    private void Awake()
    {
        _ButtonText = _Button.GetComponentInChildren<Text>();
    }

    public void Init(string className, Color color)
    {
        _ButtonText.text = className;
        _Button.image.color = color;
    }
    public void SetCheck(bool bl)
    {
        _CheckImage.enabled = bl;
    }
}
