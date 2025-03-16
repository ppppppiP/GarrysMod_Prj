
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GradualAssetsLoader : MonoBehaviour
{
    // тут указываем лоадеры и задаем в старте порядок загрузки ассетов
    public GlobalMeshLoader meshLoader;
    public GlobalTextureLoader textureLoader;
    public GlobalAudioLoader audioLoader;
    public GlobalAnimatorLoader animatorLoader;
    private IEnumerator Start()
    {
        yield return meshLoader.LoadMeshesForAutoAddressable();

        yield return textureLoader.LoadTexturesForAutoAddressable();

        yield return animatorLoader.LoadAnimatorsForAutoAddressable();

        yield return audioLoader.LoadAudioForAutoAddressable();

        Debug.Log("Загрузка ассетов завершена!");
    }

}