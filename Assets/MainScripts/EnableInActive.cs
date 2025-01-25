using UnityEngine;
using UnityEngine.Events;

public class EnableInActive: MonoBehaviour
{
    [SerializeField] UnityEvent OnActive;
    [SerializeField] UnityEvent EOnDisable;

    private void OnEnable()
    {
        OnActive?.Invoke();
    }

    private void OnDisable()
    {
        EOnDisable?.Invoke();
    }
}