using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Window : MonoBehaviour
{
    [SerializeField] CanvasGroup _CanvasGroup = default;
    Vector3 _deltaPos;
    public void OnBeginDrag()
    {
        _deltaPos = transform.position - Input.mousePosition;
    }
    public void OnDrag()
    {
        transform.position = Input.mousePosition + _deltaPos;
    }
    public void SetInteractable(bool interactable) => _CanvasGroup.interactable = interactable;
}

