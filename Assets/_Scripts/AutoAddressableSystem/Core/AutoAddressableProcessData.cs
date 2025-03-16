using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AutoAddressableProcessData : MonoBehaviour
{

    public bool processMesh = false;
    public bool processAudio = false;
    public bool processTexture = false;
    public bool processAnimator = true;

    [Header("Mesh Data")]
    public string fbxPath;    
    public string meshName;   
    public string meshAddress; 

    public string GetMeshAddress()
    {
        if (!string.IsNullOrEmpty(meshAddress))
            return meshAddress;
        if (string.IsNullOrEmpty(fbxPath) || string.IsNullOrEmpty(meshName))
            return "";
        string baseName = Path.GetFileNameWithoutExtension(fbxPath);
        meshAddress = $"{baseName}[{meshName}]";



        return meshAddress;
    }
#if UNITY_EDITOR
    public void ProcessMeshData()
    {
        var mf = GetComponent<MeshFilter>();
        var mc = GetComponent<MeshCollider>();
        if (mf != null && mf.sharedMesh != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh);
            if (!string.IsNullOrEmpty(assetPath))
            {
                fbxPath = assetPath;
            }
            meshName = mf.sharedMesh.name;
            
            GetMeshAddress();
            Debug.Log($"[AutoAddressable] Извлечены данные меша: {meshAddress}");
            mf.sharedMesh = null;
            mc.sharedMesh = null;
            EditorUtility.SetDirty(this);

        }
        else
        {
            Debug.LogWarning($"[AutoAddressable] MeshFilter или sharedMesh отсутствует на {gameObject.name}");
        }
    }
#endif

    [Header("Audio Data")]
    public string audioPath;     
    public string audioClipName;  

    public string GetAudioAddress()
    {
        if (string.IsNullOrEmpty(audioPath) || string.IsNullOrEmpty(audioClipName))
            return "";
        string baseName = Path.GetFileNameWithoutExtension(audioPath);
        return $"{baseName}";
    }


    public static Dictionary<int, (string audioPath, string audioClipName)> sharedAudioCache = new Dictionary<int, (string, string)>();
#if UNITY_EDITOR
    public void ProcessAudioData()
    {
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            int clipId = audioSource.clip.GetInstanceID();
            if (sharedAudioCache.ContainsKey(clipId))
            {
                (string cachedPath, string cachedName) = sharedAudioCache[clipId];
                audioPath = cachedPath;
                audioClipName = cachedName;
                Debug.Log($"[AutoAddressable] Используем кэшированные аудио данные для {gameObject.name}: {GetAudioAddress()}");
            }
            else
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(audioSource.clip);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    audioPath = assetPath;
                }
                audioClipName = audioSource.clip.name;
                sharedAudioCache[clipId] = (audioPath, audioClipName);
                
                Debug.Log($"[AutoAddressable] Извлечены аудио данные: {GetAudioAddress()} для {gameObject.name}");
            }

            EditorUtility.SetDirty(this);

            audioSource.clip = null;
        }
        else
        {
            Debug.LogWarning($"[AutoAddressable] AudioSource или clip отсутствует на {gameObject.name}");
        }
    }
#endif

    [Header("Texture Data")]
    public string texturePath;  
    public string textureName;    
    public static Dictionary<int, (string texturePath, string textureName)> sharedTextureCache = new Dictionary<int, (string, string)>();


    public string GetTextureAddress()
    {
        if (string.IsNullOrEmpty(texturePath) || string.IsNullOrEmpty(textureName))
            return "";
        string baseName = Path.GetFileNameWithoutExtension(texturePath);
        return $"{baseName}";
    }


    
#if UNITY_EDITOR
    public void ProcessTextureData()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            int matId = renderer.sharedMaterial.GetInstanceID();
            if (sharedTextureCache.ContainsKey(matId))
            {
                (string cachedPath, string cachedName) = sharedTextureCache[matId];
                texturePath = cachedPath;
                textureName = cachedName;
                Debug.Log($"[AutoAddressable] Используем кэшированную текстуру для {gameObject.name}: {GetTextureAddress()}");
            }
            else if (renderer.sharedMaterial.mainTexture != null)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    texturePath = assetPath;
                    sharedTextureCache[matId] = (texturePath, renderer.sharedMaterial.mainTexture.name);
                }
                textureName = renderer.sharedMaterial.mainTexture.name;
                Debug.Log($"[AutoAddressable] Извлечены данные текстуры: {GetTextureAddress()} для {gameObject.name}");
                renderer.sharedMaterial.mainTexture = null;

                EditorUtility.SetDirty(this);

            }
            else
            {
                Debug.LogWarning($"[AutoAddressable] Material у {gameObject.name} уже очищен и не найден в кэше.");
            }
        }
        else
        {
            Debug.LogWarning($"[AutoAddressable] Renderer или материал отсутствует на {gameObject.name}");
        }
    }
#endif
    public void ClearAddressableAssets()
    {

        var mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            mf.sharedMesh = null;
            Debug.Log($"[AutoAddressable] Меш очищен на {gameObject.name}");
        }

        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.clip = null;
            Debug.Log($"[AutoAddressable] Аудио очищено на {gameObject.name}");
        }

        var rend = GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            rend.sharedMaterial.mainTexture = null;
            Debug.Log($"[AutoAddressable] Текстура очищена на {gameObject.name}");
        }

        var animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.runtimeAnimatorController = null;
            Debug.Log($"[AutoAddressable] Текстура очищена на {gameObject.name}");
        }

    }

    [Header("Animator Data")]

    public string animatorControllerPath; 
    public string animatorControllerName; 
    public string GetAnimatorAddress()
    {
        if (string.IsNullOrEmpty(animatorControllerPath) || string.IsNullOrEmpty(animatorControllerName))
            return "";
        string baseName = Path.GetFileNameWithoutExtension(animatorControllerPath);

        return $"{baseName}";
    }


#if UNITY_EDITOR
    public void ProcessAnimatorData()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
            if (!string.IsNullOrEmpty(assetPath))
            {
                animatorControllerPath = assetPath;
            }
            animatorControllerName = animator.runtimeAnimatorController.name;
            animator.runtimeAnimatorController = null;
            Debug.Log($"[AutoAddressable] Извлечены данные аниматора: {GetAnimatorAddress()}");
        }
        else
        {
            Debug.LogWarning($"[AutoAddressable] Animator или контроллер отсутствует на {gameObject.name}");
        }
    }
#endif
}
