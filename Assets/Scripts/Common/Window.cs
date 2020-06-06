using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Window : MonoBehaviour
{

    Vector3 _deltaPos;

    public void OnBeginDrag()
    {
        _deltaPos = transform.position - Input.mousePosition;
    }
    public void OnDrag()
    {
        transform.position = Input.mousePosition + _deltaPos;
    }
}

