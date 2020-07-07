using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Linq;

public class FileWindow : Window
{
    [SerializeField] Dropdown _Dropdown = default;
    [SerializeField] Button _NextButton = default;
    [SerializeField] Button _BackButton = default;

    List<DropDownElement> Elements = new List<DropDownElement>();

    public IObservable<Unit> OnClick_Next => _NextButton.OnClickAsObservable();
    public IObservable<Unit> OnCkick_Back => _BackButton.OnClickAsObservable();

    Subject<int> SendValueSubject = new Subject<int>();
    Subject<bool> InvalidLoadSubject = new Subject<bool>();
    public IObservable<(int Previous, int Current)> OnSendIndex;
    public IObservable<(string Previous, string Current)> OnSendValue => OnSendIndex.Select(pair => (Elements[pair.Previous].FileName, Elements[pair.Current].FileName));
    public IObservable<int> OnValueChanged => _Dropdown.onValueChanged.AsObservable();
    public IObservable<bool> OnInvalidLoad => InvalidLoadSubject;

    private void Awake()
    {
        OnSendIndex = SendValueSubject.Merge(OnValueChanged).Pairwise().Select(pair => (pair.Previous, pair.Current)).Publish().RefCount();
        OnSendIndex.Subscribe(pair => SetDone(pair.Previous)).AddTo(this);
    }

    public void Set(List<(string FileName, bool isDone)> fileTuples)
    {
        Elements.Clear();
        Elements = fileTuples.Select(tuple => new DropDownElement(tuple.FileName, tuple.isDone)).ToList();
        var fileNames = Elements.Select(element => element.ToString()).ToList();
        _Dropdown.ClearOptions();
        _Dropdown.AddOptions(fileNames);
    }
    public void SendValue()
    {
        SendValueSubject.OnNext(_Dropdown.value);
    }

    public void Next()
    {
        if (_Dropdown.value + 1 == Elements.Count)
            InvalidLoadSubject.OnNext(true);
        else
            _Dropdown.value++;
    }
    public void Back()
    {
        if (_Dropdown.value == 0)
            InvalidLoadSubject.OnNext(false);
        else
            _Dropdown.value--;
    }
    void SetDone(int index)
    {
        var element = Elements[index];
        if (element.IsDone)
            return;
        var eName = element.FileName;
        Elements[index] = new DropDownElement(eName, true);
        SetOptionToDropdown(index, Elements[index].ToString());
    }
    void SetOptionToDropdown(int index, string text)
    {
        if ((index >= 0) && index < _Dropdown.options.Count)
        {
            _Dropdown.options.RemoveAt(index);
            _Dropdown.options.Insert(index, new Dropdown.OptionData { text = text });
            _Dropdown.RefreshShownValue();
        }
        else
        {
            Debug.LogWarning($"予期せぬファイルリストを削除しようとしました。index:{index}");
        }
    }
    struct DropDownElement
    {
        const string TRUE_PREFIX = "[済]";
        const string FALSE_PREFIX = "[  ]";
        public string FileName;
        public bool IsDone;
        public DropDownElement(string fileName, bool isDone)
        {
            FileName = fileName;
            IsDone = isDone;
        }
        public override string ToString() => IsDone ? TRUE_PREFIX + FileName : FALSE_PREFIX + FileName;

    }
}
