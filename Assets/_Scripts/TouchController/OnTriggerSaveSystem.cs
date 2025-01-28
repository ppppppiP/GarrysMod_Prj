//using UnityEngine;
//using UnityEngine.Events;
//using YG;

//public class OnTriggerSaveSystem : MonoBehaviour
//{
//    public UnityEvent OnSaveLoad; // Событие, вызываемое при совпадении ключа
//    public string SaveKey; // Уникальный ключ, задаваемый в инспекторе

//    private void Start()
//    {
//        if (EnsureKeyExists(SaveKey)) // Убедиться, что ключ существует (добавить, если нет)
//        {
//            if (IsKeyTrue(SaveKey))
//            {
//                OnSaveLoad?.Invoke(); // Если ключ равен true, вызываем событие
//            }
//        }
//    }

//    /// <summary>
//    /// Проверяет, равен ли ключ true.
//    /// </summary>
//    private bool IsKeyTrue(string key)
//    {
//        for (int i = 0; i < YandexGame.savesData.KeysToSave.Length; i++)
//        {
//            if (YandexGame.savesData.KeysToSave[i] == key)
//            {
//                return YandexGame.savesData.KeyStates[i];
//            }
//        }
//        return false; // Ключ не найден
//    }

//    /// <summary>
//    /// Убеждается, что ключ существует. Если его нет, добавляет с состоянием false.
//    /// </summary>
//    private bool EnsureKeyExists(string key)
//    {
//        var keys = YandexGame.savesData.KeysToSave;
//        var states = YandexGame.savesData.KeyStates;

//        for (int i = 0; i < keys.Length; i++)
//        {
//            if (keys[i] == key)
//            {
//                return true; // Ключ уже существует
//            }

//            if (string.IsNullOrEmpty(keys[i]))
//            {
//                keys[i] = key; // Добавляем новый ключ
//                states[i] = false; // Устанавливаем состояние по умолчанию (false)
//                YandexGame.SaveProgress();
//                return true;
//            }
//        }

//        Debug.LogWarning("Массив ключей заполнен. Увеличьте его размер.");
//        return false;
//    }

//    /// <summary>
//    /// Устанавливает ключ в true.
//    /// </summary>
//    public void SetKeyTrue()
//    {
//        var keys = YandexGame.savesData.KeysToSave;
//        var states = YandexGame.savesData.KeyStates;

//        for (int i = 0; i < keys.Length; i++)
//        {
//            if (keys[i] == SaveKey)
//            {
//                states[i] = true; // Устанавливаем значение в true
//                YandexGame.SaveProgress();
//                return;
//            }
//        }

//        Debug.LogWarning($"Ключ '{SaveKey}' не найден. Возможно, он не был добавлен.");
//    }
//}
