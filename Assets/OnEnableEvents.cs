using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnEnableEvents : MonoBehaviour
{
    [SerializeField] UnityEvent EOnEnable;
    [SerializeField] UnityEvent EOnDisable;

    private void OnEnable()
    {
        EOnEnable?.Invoke();
    }
    private void OnDisable()
    {
        EOnDisable?.Invoke();
    }
}
