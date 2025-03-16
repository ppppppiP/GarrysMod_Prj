#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;

public class AutoAddressablePrebuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
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
        Debug.Log($"[AutoAddressableProcessor] ���������� {countMesh} �������� ��� �����, {countAudio} ��� ������ � {countTexture} ��� �������.");

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings �� �������!");
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
        Debug.Log($"[AutoAddressableProcessor] ���������� {countRegister} �������� ��� ����������� � Addressables.");


        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[AutoAddressableProcessor] ����������� Addressable ������� ���������.");

        foreach (var obj in autoObjs)
        {
            if (obj.processMesh)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) => { comp.ProcessMeshData(); });
                countMesh++;
            }
            if (obj.processAudio)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) => { comp.ProcessAudioData(); });
                countAudio++;
            }
            if (obj.processAnimator)
            {
                AutoAddressableUtils.ExecuteWithActive(obj, (comp) => comp.ProcessAnimatorData());
            }
        } // ��������� ���� ����� ������ �� ������� ���� ���������
    }
}

#endif
