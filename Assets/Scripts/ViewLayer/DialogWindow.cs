using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
public class DialogWindow : MonoBehaviour
{
    [SerializeField] Text _Text = default;
    [SerializeField] Button _Button = default;
    public IObservable<Unit> OnClick_OK => _Button.OnClickAsObservable();

    public void SetMessage(string msg) => _Text.text = msg;
    public void SetEnable(bool enable) => gameObject.SetActive(enable);
}
