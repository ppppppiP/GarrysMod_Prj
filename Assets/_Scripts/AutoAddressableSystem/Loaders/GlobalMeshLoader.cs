using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class GlobalMeshLoader : MonoBehaviour
{
    public IEnumerator LoadMeshesForAutoAddressable()
    {
        AutoAddressableProcessData[] autoObjs = GameObject.FindObjectsOfType<AutoAddressableProcessData>(true);
        foreach (AutoAddressableProcessData obj in autoObjs)
        {
            if (obj.processMesh)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) =>
                {
                    StartCoroutine(LoadAndAssignMesh(comp));
                });
            }
        }
        yield break;
    }

    private IEnumerator LoadAndAssignMesh(AutoAddressableProcessData autoAddr)
    {
        string address = autoAddr.GetMeshAddress();
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError($"[GlobalMeshLoader] Адрес меша не задан для объекта {autoAddr.gameObject.name}");
            yield break;
        }
        Debug.Log($"[GlobalMeshLoader] Загружаем меш по адресу: {address}");

        AsyncOperationHandle<Mesh> handle = Addressables.LoadAssetAsync<Mesh>(address);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Mesh loadedMesh = handle.Result;
            MeshFilter mf = autoAddr.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = loadedMesh;
                Debug.Log($"[GlobalMeshLoader] Меш {loadedMesh.name} назначен объекту {autoAddr.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[GlobalMeshLoader] Не найден компонент MeshFilter у объекта {autoAddr.gameObject.name}");
            }
        }
        else
        {
            Debug.LogError($"[GlobalMeshLoader] Не удалось загрузить меш по адресу: {address}");
        }
    }
}
