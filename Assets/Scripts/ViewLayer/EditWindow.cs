using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class EditWindow : Window
{
    [SerializeField] Button UndoButton = default;
    [SerializeField] Button RedoButton = default;
    [SerializeField] Button DeleteButton = default;

    public IObservable<Unit> OnClick_Undo => UndoButton.OnClickAsObservable();
    public IObservable<Unit> OnClick_Redo => RedoButton.OnClickAsObservable();
    public IObservable<Unit> OnClick_Delete => DeleteButton.OnClickAsObservable();

    public void EnableButton_Undo(bool enable) => UndoButton.interactable = enable;
    public void EnableButton_Redo(bool enable) => RedoButton.interactable = enable;
    public void EnableButton_Delete(bool enable) => DeleteButton.interactable = enable;
}
