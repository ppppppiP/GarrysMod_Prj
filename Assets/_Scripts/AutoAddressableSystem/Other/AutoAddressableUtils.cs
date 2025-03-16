
using UnityEngine;


public static class AutoAddressableUtils
{
    /// <summary>
    /// Выполняет указанное действие для компонента, временно включая GameObject, если он был выключен.
    /// После выполнения восстанавливает исходное состояние.
    /// </summary>
    public static void ExecuteWithActive<T>(T component, System.Action<T> action) where T : Component
    {
        GameObject go = component.gameObject;
        bool wasActive = go.activeSelf;
        if (!wasActive)
        {
            go.SetActive(true);
        }
        action(component);
        if (!wasActive)
        {
            go.SetActive(false);
        }
    }
}
