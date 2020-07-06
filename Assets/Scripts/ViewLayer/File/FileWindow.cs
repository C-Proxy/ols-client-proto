using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Linq;

public class FileWindow : Window
{
    [SerializeField] Dropdown Dropdown = default;
    [SerializeField] Button _NextButton = default;
    [SerializeField] Button _BackButton = default;

    List<DropDownElement> Elements = new List<DropDownElement>();

    public IObservable<Unit> OnClick_Next => _NextButton.OnClickAsObservable();
    public IObservable<Unit> OnCkick_Back => _BackButton.OnClickAsObservable();

    Subject<int> SendValueSubject = new Subject<int>();
    Subject<bool> InvalidLoadSubject = new Subject<bool>();
    public IObservable<string> OnSendValue => SendValueSubject.Merge(OnValueChanged).Select(index => Elements[index].FileName);
    public IObservable<int> OnValueChanged => Dropdown.onValueChanged.AsObservable();
    public IObservable<bool> OnInvalidLoad => InvalidLoadSubject;

    public void Set(List<(string FileName, bool isDone)> fileTuples)
    {
        Elements.Clear();
        Elements = fileTuples.Select(tuple => new DropDownElement(tuple.FileName, tuple.isDone)).ToList();
        var fileNames = Elements.Select(element => element.ToString()).ToList();
        Dropdown.ClearOptions();
        Dropdown.AddOptions(fileNames);
    }
    public void SendValue()
    {
        SendValueSubject.OnNext(Dropdown.value);
    }

    public void Next()
    {
        if (Dropdown.value + 1 == Elements.Count)
            InvalidLoadSubject.OnNext(true);
        else
            Dropdown.value++;
    }
    public void Back()
    {
        if (Dropdown.value == 0)
            InvalidLoadSubject.OnNext(false);
        else
            Dropdown.value--;
    }

    struct DropDownElement
    {
        const string TRUE_PREFIX = "[済]";
        const string FALSE_PREFIX = "[  ]";
        public string FileName;
        bool IsDone;
        public DropDownElement(string fileName, bool isDone)
        {
            FileName = fileName;
            IsDone = isDone;
        }
        public override string ToString() => IsDone ? TRUE_PREFIX + FileName : FALSE_PREFIX + FileName;

    }
}
