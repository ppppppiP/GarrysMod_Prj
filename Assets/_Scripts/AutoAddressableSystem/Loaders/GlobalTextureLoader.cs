using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GlobalTextureLoader : MonoBehaviour
{
  
    public IEnumerator LoadTexturesForAutoAddressable()
    {
        AutoAddressableProcessData[] autoObjs = GameObject.FindObjectsOfType<AutoAddressableProcessData>(true);
        foreach (AutoAddressableProcessData autoAddr in autoObjs)
        {
            if (autoAddr.processTexture)
            {
                yield return StartCoroutine(LoadAndAssignTexture(autoAddr));
            }
        }
    }

    private IEnumerator LoadAndAssignTexture(AutoAddressableProcessData autoAddr)
    {
        GameObject go = autoAddr.gameObject;
        bool wasActive = go.activeSelf;
        if (!wasActive)
            go.SetActive(true);

        string address = autoAddr.GetTextureAddress();
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError($"[GlobalTextureLoader] Адрес текстуры не задан для объекта {go.name}");
            if (!wasActive)
                go.SetActive(false);
            yield break;
        }
        Debug.Log($"[GlobalTextureLoader] Загружаем текстуру по адресу: {address}");

        AsyncOperationHandle<Texture> handle = Addressables.LoadAssetAsync<Texture>(address);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Texture loadedTexture = handle.Result;
            Renderer rend = autoAddr.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                rend.sharedMaterial.mainTexture = loadedTexture;
                Debug.Log($"[GlobalTextureLoader] Текстура {loadedTexture.name} назначена объекту {go.name}");
            }
            else
            {
                Debug.LogError($"[GlobalTextureLoader] Renderer или материал не найден у объекта {go.name}");
            }
        }
        else
        {
            Debug.LogError($"[GlobalTextureLoader] Не удалось загрузить текстуру по адресу: {address}");
        }

        if (!wasActive)
            go.SetActive(false);
    }
}