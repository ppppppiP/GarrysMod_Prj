using UnityEngine;
using UnityEngine.Events;
using YG;

public class SimpleOnTriggerEvents: MonoBehaviour
{
    [SerializeField] UnityEvent EOnTriggerEnter;
    [SerializeField] UnityEvent EOnTriggerExit;
    [SerializeField] UnityEvent EOnTriggerStay;

    [SerializeField] KeyCode key;
    [SerializeField] bool IsKeyUse;
    [SerializeField] UnityEvent EOnKeyInTriggerDown;

    [SerializeField] LayerMask DetectLayer;

    private bool isEnter;

    private void OnTriggerEnter(Collider other)
    {
        if ((DetectLayer & (1 << other.gameObject.layer)) != 0)
        {
            isEnter = true;
            EOnTriggerEnter?.Invoke();
            ButtonEventHandler.instance.SetListener(() => MobileInput()) ;
            if (YandexGame.EnvironmentData.isMobile)
                ButtonEventHandler.instance.Enable();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((DetectLayer & (1 << other.gameObject.layer)) != 0)
        {
            isEnter = false;
            EOnTriggerExit?.Invoke();
            ButtonEventHandler.instance.RemoveListener();
            if(YandexGame.EnvironmentData.isMobile)
            ButtonEventHandler.instance.Disable();
        }
    }

    private void Update()
    {
        if(isEnter)
        {
            if (IsKeyUse)
            {
                if (Input.GetKeyDown(key))
                {
                    EOnKeyInTriggerDown?.Invoke();
                }
            }
            EOnTriggerStay?.Invoke();
            
        }
    }

    public void MobileInput()
    {
        if (isEnter)
        {
            EOnKeyInTriggerDown?.Invoke();
        }
    }

    public void EOnTriggerEnterVoid()
    {
        EOnTriggerEnter?.Invoke();
    } 
    public void EOnTriggerExitVoid()
    {
        EOnTriggerExit?.Invoke();
    } 
    public void EOnTriggerStayVoid()
    {
        EOnTriggerStay?.Invoke();
    }
    public void EOnKeyInTriggerDownVoid()
    {
        EOnKeyInTriggerDown?.Invoke();
    }
}
public static class SaveSystemHelper
{
    public static void SaveKey(string key, bool value)
    {
        var keys = YandexGame.savesData.KeysToSave;
        var states = YandexGame.savesData.KeyStates;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == key)
            {
                states[i] = value; // Обновляем значение, если ключ найден
                return;
            }

            if (string.IsNullOrEmpty(keys[i]))
            {
                keys[i] = key; // Записываем новый ключ
                states[i] = value; // Устанавливаем значение
                return;
            }
        }

        Debug.LogWarning("Массив ключей заполнен. Увеличьте его размер.");
    }
}