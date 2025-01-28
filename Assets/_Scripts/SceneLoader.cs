using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class SceneLoader : MonoBehaviour
{

    [SerializeField] private AssetReference sceneReference;
    AssetReferenceGameObject sceneReferenceGameObject;

    private AsyncOperationHandle<SceneInstance> sceneHandle;

    

    public void LoadScene()
    {
        if (sceneReference == null)
        {
            Debug.LogError("Ссылка на сцену не указана!");
            return;
        }

        sceneReference.LoadSceneAsync().Completed += OnSceneLoaded;
    }

    private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("Сцена успешно загружена!");
            sceneHandle = handle;
        }
        else
        {
            Debug.LogError($"Ошибка загрузки сцены: {handle.OperationException}");
        }
    }


    // пока не нужно потом нужно
    public void UnloadScene()
    {
        if (sceneHandle.IsValid())
        {
            Addressables.UnloadSceneAsync(sceneHandle).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("Сцена успешно выгружена!");
                }
                else
                {
                    Debug.LogError($"Ошибка выгрузки сцены: {handle.OperationException}");
                }
            };
        }
        else
        {
            Debug.LogError("Сцена не загружена или handle недействителен.");
        }
    }
}
