using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class FileWindow : Window
{
    [SerializeField]
    Dropdown Dropdown;
    [SerializeField]
    Button _NextButton, _BackButton;

    public IObservable<Unit> OnClick_Next => _NextButton.OnClickAsObservable();
    public IObservable<Unit> OnCkick_Back => _BackButton.OnClickAsObservable();

    Subject<string> SendValueSubject = new Subject<string>();
    public IObservable<string> OnSendValue => SendValueSubject.Merge(OnValueChanged);
    public IObservable<string> OnValueChanged => Dropdown.onValueChanged.AsObservable().Select(value => Dropdown.options[value].text);

    public void Set(List<string> fileNames)
    {
        Dropdown.ClearOptions();
        Dropdown.AddOptions(fileNames);
    }
    public void SendValue()
    {
        SendValueSubject.OnNext(Dropdown.options[Dropdown.value].text);
    }

    public void Next() => Dropdown.value++;
    public void Back() => Dropdown.value--;
}
