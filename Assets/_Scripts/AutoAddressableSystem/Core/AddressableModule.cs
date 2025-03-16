#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System.IO;
using System;

public interface IAddressableModule
{
    void Process(GameObject go, AddressableAssetSettings settings);
}

public class GenericAddressableModule<T> : IAddressableModule
{
    private static HashSet<string> processedGuids = new HashSet<string>();

    /// <summary>
    /// Функция для получения пути к ассету, если он есть.
    /// </summary>
    public Func<GameObject, string> GetAssetPathFunc { get; set; }

    /// <summary>
    /// Функция для формирования адреса из пути или из других данных.
    /// </summary>
    public Func<GameObject, string> GetAddressFunc { get; set; }

    /// <summary>
    /// Лейбл, который будет назначаться ассету.
    /// </summary>
    public string Label { get; set; }

    public void Process(GameObject go, AddressableAssetSettings settings)
    {
        string assetPath = GetAssetPathFunc(go);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"[GenericAddressableModule] Для объекта {go.name} не найден путь к ассету.");
            return;
        }
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning($"[GenericAddressableModule] Для объекта {go.name} не удалось получить GUID.");
            return;
        }
        if (!processedGuids.Contains(guid))
        {
            processedGuids.Add(guid);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            }
            string address = GetAddressFunc(go);
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning($"[GenericAddressableModule] Для объекта {go.name} не сформировался адрес.");
                return;
            }
            entry.address = address;
            entry.SetLabel(Label, true);
            Debug.Log($"[GenericAddressableModule] Зарегистрирован {Label}: {entry.address} ({assetPath})");
        }
    }
}

public static class AddressableModulesFactory
{
    public static IAddressableModule[] GetModules()
    {

        var meshModule = new GenericAddressableModule<Mesh>
        {
            Label = "Mesh",
            GetAssetPathFunc = (go) =>
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                    return AssetDatabase.GetAssetPath(mf.sharedMesh);
                return "";
            },
            GetAddressFunc = (go) =>
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mf.sharedMesh));
                }
                return "";
            }
        };


        var audioModule = new GenericAddressableModule<AudioClip>
        {
            Label = "Audio",
            GetAssetPathFunc = (go) =>
            {
                var src = go.GetComponent<AudioSource>();
                if (src != null && src.clip != null)
                  
                    return AssetDatabase.GetAssetPath(src.clip);
                return "";
            },
            GetAddressFunc = (go) =>
            {
                var src = go.GetComponent<AudioSource>();
                if (src != null && src.clip != null)
                    return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(src.clip));
                return "";
            }
        };


        var textureModule = new GenericAddressableModule<Texture>
        {
            Label = "Texture",
            GetAssetPathFunc = (go) =>
            {
                var auto = go.GetComponent<AutoAddressableProcessData>();
                return auto != null ? auto.texturePath : "";
            },
            GetAddressFunc = (go) =>
            {
                var auto = go.GetComponent<AutoAddressableProcessData>();
                return auto != null ? auto.GetTextureAddress() : "";
            }
        };


        var animatorModule = new GenericAddressableModule<RuntimeAnimatorController>
        {
            Label = "Animator",
            GetAssetPathFunc = (go) =>
            {
                var animator = go.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                    return AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                return "";
            },
            GetAddressFunc = (go) =>
            {
                var animator = go.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
                }
                return "";
            }
        };



        return new IAddressableModule[]
        {
            meshModule,
            audioModule,
            textureModule,
            animatorModule
        };
    }//ДОБАВЛЯЕМ НОВЫЕ МОДУЛИ ПО ПРИМЕРУ ЕСЛИ ТРЕБУЕТСЯ
}

#endif