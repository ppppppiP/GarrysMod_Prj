
#if UNITY_EDITOR
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using UnityEditor;

public static class PreLoadAddressablesEditor
{
    [MenuItem("Tools/AutoAdressableSystem/Preload Addressables")]
    public static void PreloadAddressables()
    {
        AutoAddressableProcessData[] autoObjs = GameObject.FindObjectsOfType<AutoAddressableProcessData>(true);
        foreach (AutoAddressableProcessData autoAddr in autoObjs)
        {
            if (autoAddr.processMesh)
            {
                string address = autoAddr.GetMeshAddress();
                if (!string.IsNullOrEmpty(address))
                {
                    AsyncOperationHandle<Mesh> meshHandle = Addressables.LoadAssetAsync<Mesh>(address);
                    Mesh loadedMesh = meshHandle.WaitForCompletion();
                    if (loadedMesh != null)
                    {
                        MeshFilter mf = autoAddr.GetComponent<MeshFilter>();
                        MeshCollider meshCollider = autoAddr.GetComponent<MeshCollider>();
                        if (mf != null)
                        {
                            mf.sharedMesh = loadedMesh;
                            meshCollider.sharedMesh = loadedMesh;
                            Debug.Log($"[PreLoad] Меш {loadedMesh.name} назначен {autoAddr.gameObject.name}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PreLoad] Не удалось загрузить меш по адресу {address} для {autoAddr.gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PreLoad] Для {autoAddr.gameObject.name} не задан адрес меша.");
                }
            }
            if (autoAddr.processAudio)
            {
                string address = autoAddr.GetAudioAddress();
                if (!string.IsNullOrEmpty(address))
                {
                    AsyncOperationHandle<AudioClip> audioHandle = Addressables.LoadAssetAsync<AudioClip>(address);
                    AudioClip loadedClip = audioHandle.WaitForCompletion();
                    if (loadedClip != null)
                    {
                        AudioSource source = autoAddr.GetComponent<AudioSource>();
                        if (source != null)
                        {
                            source.clip = loadedClip;
                            Debug.Log($"[PreLoad] Аудиоклип {loadedClip.name} назначен {autoAddr.gameObject.name}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PreLoad] Не удалось загрузить аудиоклип по адресу {address} для {autoAddr.gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PreLoad] Для {autoAddr.gameObject.name} не задан адрес аудио.");
                }
            }

            if (autoAddr.processTexture)
            {
                string address = autoAddr.GetTextureAddress();
                if (!string.IsNullOrEmpty(address))
                {
                    AsyncOperationHandle<Texture> texHandle = Addressables.LoadAssetAsync<Texture>(address);
                    Texture loadedTexture = texHandle.WaitForCompletion();
                    if (loadedTexture != null)
                    {
                        Renderer rend = autoAddr.GetComponent<Renderer>();
                        if (rend != null && rend.sharedMaterial != null)
                        {
                            rend.sharedMaterial.mainTexture = loadedTexture;
                            Debug.Log($"[PreLoad] Текстура {loadedTexture.name} назначена {autoAddr.gameObject.name}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PreLoad] Не удалось загрузить текстуру по адресу {address} для {autoAddr.gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PreLoad] Для {autoAddr.gameObject.name} не задан адрес текстуры.");
                }
            }
            if (autoAddr.processAnimator)
            {
                string address = autoAddr.GetAnimatorAddress();
                if (!string.IsNullOrEmpty(address))
                {
                    AsyncOperationHandle<RuntimeAnimatorController> animHandle = Addressables.LoadAssetAsync<RuntimeAnimatorController>(address);
                    RuntimeAnimatorController loadedController = animHandle.WaitForCompletion();
                    if (loadedController != null)
                    {
                        Animator animator = autoAddr.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.runtimeAnimatorController = loadedController;
                            Debug.Log($"[PreLoad] Контроллер {loadedController.name} назначен {autoAddr.gameObject.name}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PreLoad] Не удалось загрузить контроллер по адресу {address} для {autoAddr.gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PreLoad] Для {autoAddr.gameObject.name} не задан адрес аниматора.");
                }
            }
        }
        Debug.Log("[PreLoad] Предзагрузка Addressables завершена.");

    }
}

public static class ClearAddressablesProcessor
{
    [MenuItem("Tools/AutoAdressableSystem/Clear Addressable Assets")]
    public static void ClearAddressableAssetsForAll()
    {
        AutoAddressableProcessData[] autoObjs = GameObject.FindObjectsOfType<AutoAddressableProcessData>(true);
        int count = 0;
        foreach (AutoAddressableProcessData obj in autoObjs)
        {
            obj.ClearAddressableAssets();
            EditorUtility.SetDirty(obj);
            count++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[ClearAddressablesProcessor] Очищено адресных ассетов у {count} объектов.");
    }
}
#endif