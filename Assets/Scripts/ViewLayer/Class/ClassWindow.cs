using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class ClassWindow : Window
{
    [SerializeField]
    GameObject prefab_ClassElement;
    [SerializeField]
    Transform ClassContentTarget;


    Subject<int> ActiveElementSubject = new Subject<int>();
    public IObservable<int> OnAcitveElementChanged => ActiveElementSubject;

    ClassElement[] ClassElements;

    public void Set(string[] classNames)
    {
        var length = classNames.Length;
        if (length < 1)
        {
            CreateDummy();
            return;
        }
        ClassElements = new ClassElement[length];

        Color color;
        ClassElement element;
        for (var i = 0; i < length; i++)
        {
            var j = i;
            color = ColorPalatte.GetColor(j);
            element = CreateClassElement(classNames[j], color);
            ClassElements[j] = element;
            element.OnClick
            .Select(_ => j)
            .Subscribe(index =>
            {
                ActiveElementSubject.OnNext(index);
                EnableCheck(index);
            }).AddTo(this);
        }
        EnableCheck(0);
    }
    public void ActivateElement(int classId)
    {
        EnableCheck(classId);
        ActiveElementSubject.OnNext(classId);
    }
    ClassElement CreateClassElement(string className, Color color)
    {
        var obj = Instantiate(prefab_ClassElement, ClassContentTarget);
        var element = obj.GetComponent<ClassElement>();
        element.Init(className, color);
        return element;
    }
    void CreateDummy()
    {
        Instantiate(prefab_ClassElement, ClassContentTarget);
    }
    void EnableCheck(int index)
    {
        foreach (var e in ClassElements)
            e.SetCheck(false);
        ClassElements[index].SetCheck(true);
    }

}
public static class ColorPalatte
{

    static List<Color> Colors = new List<Color>
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.magenta,
        Color.yellow,
        Color.cyan,
        new Color(1,0.5f,0),
        new Color(0,0.5f,0),
        new Color(0.5f,0,1)
    };

    public static Color GetColor(int index)
    {
        if (index < 0)
            return Color.white;

        if (Colors.Count > index)
        {
            return Colors[index];
        }
        else
        {
            var newColor = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0.25f, 0.75f), 1);
            Colors.Add(newColor);
            return newColor;
        }
    }
}
