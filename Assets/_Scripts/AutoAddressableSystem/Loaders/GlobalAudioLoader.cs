using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using System.Collections;

public class GlobalAudioLoader : MonoBehaviour
{
 
    public IEnumerator LoadAudioForAutoAddressable()
    {
        AutoAddressableProcessData[] autoObjs = GameObject.FindObjectsOfType<AutoAddressableProcessData>(true);
        foreach (AutoAddressableProcessData autoAddr in autoObjs)
        {
            if (autoAddr.processAudio)
            {
                yield return StartCoroutine(LoadAndAssignAudio(autoAddr));
            }
        }
    }

    private IEnumerator LoadAndAssignAudio(AutoAddressableProcessData autoAddr)
    {
        GameObject go = autoAddr.gameObject;
        bool wasActive = go.activeSelf;
        if (!wasActive)
            go.SetActive(true);

        string address = autoAddr.GetAudioAddress();
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError($"[GlobalAudioLoader] Адрес аудио не задан для объекта {go.name}");
            if (!wasActive)
                go.SetActive(false);
            yield break;
        }
        Debug.Log($"[GlobalAudioLoader] Загружаем аудио по адресу: {address}");

        AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(address);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip loadedClip = handle.Result;
            AudioSource source = autoAddr.GetComponent<AudioSource>();
            if (source != null)
            {
                source.clip = loadedClip;
                Debug.Log($"[GlobalAudioLoader] Аудиоклип {loadedClip.name} назначен объекту {go.name}");
            }
            else
            {
                Debug.LogError($"[GlobalAudioLoader] Не найден AudioSource у объекта {go.name}");
            }
        }
        else
        {
            Debug.LogError($"[GlobalAudioLoader] Не удалось загрузить аудиоклип по адресу: {address}");
        }

        if (!wasActive)
            go.SetActive(false);
    }
}
