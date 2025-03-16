#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;


public static class AutoAddressableProcessorEditor
{
    [MenuItem("Tools/AutoAdressableSystem/Process AutoAddressable Data + Register")]
    public static void ProcessAndRegister()
    {
        AutoAddressableProcessData[] autoObjs = GameObject.FindObjectsOfType<AutoAddressableProcessData>(true);

        int countMesh = 0;
        int countAudio = 0;
        int countTexture = 0;
        foreach (var obj in autoObjs)
        {
            if (obj.processTexture)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) => { comp.ProcessTextureData(); });
                countTexture++;
            }
        }
        Debug.Log($"[AutoAddressableProcessor] Обработано {countMesh} объектов для мешей, {countAudio} для звуков и {countTexture} для текстур.");

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings не найдены!");
            return;
        }

        IAddressableModule[] modules = AddressableModulesFactory.GetModules();
        int countRegister = 0;
        foreach (var autoObj in autoObjs)
        {
            foreach (var module in modules)
            {
                module.Process(autoObj.gameObject, settings);
                
            }
            countRegister++;
        }
        Debug.Log($"[AutoAddressableProcessor] Обработано {countRegister} объектов для регистрации в Addressables.");

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[AutoAddressableProcessor] Регистрация Addressable ассетов завершена.");

        foreach (var obj in autoObjs)
        {
            if (obj.processMesh)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) => comp.ProcessMeshData());
                countMesh++;
            }
            if (obj.processAudio)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) => comp.ProcessAudioData());
                countAudio++;
            }
            if (obj.processAnimator)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) => comp.ProcessAnimatorData());
            }
        } //ДОБАВИТЬ СЮДА МЕТОД ОБРАБОТКИ НУЖНЫХ КОМПОНЕНТОВ ЕСЛИ ПОЯВИЛИСЬ НОВЫЕ
        }
}
#endif