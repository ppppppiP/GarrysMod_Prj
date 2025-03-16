using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using System.Collections;

public class GlobalAnimatorLoader : MonoBehaviour
{


    public IEnumerator LoadAnimatorsForAutoAddressable()
    {
        AutoAddressableProcessData[] autoObjs = GameObject.FindObjectsOfType<AutoAddressableProcessData>(true);
        foreach (AutoAddressableProcessData autoAddr in autoObjs)
        {
            if (autoAddr.processAnimator)
            {
                yield return StartCoroutine(LoadAndAssignAnimator(autoAddr));
            }
        }
    }

    private IEnumerator LoadAndAssignAnimator(AutoAddressableProcessData autoAddr)
    {
        GameObject go = autoAddr.gameObject;
        bool wasActive = go.activeSelf;
        if (!wasActive)
            go.SetActive(true);

        string address = autoAddr.GetAnimatorAddress();
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError($"[GlobalAnimatorLoader] Адрес контроллера не задан для объекта {go.name}");
            if (!wasActive)
                go.SetActive(false);
            yield break;
        }
        Debug.Log($"[GlobalAnimatorLoader] Загружаем контроллер по адресу: {address}");

        AsyncOperationHandle<RuntimeAnimatorController> handle = Addressables.LoadAssetAsync<RuntimeAnimatorController>(address);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            RuntimeAnimatorController controller = handle.Result;
            Animator animator = autoAddr.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                Debug.Log($"[GlobalAnimatorLoader] Контроллер {controller.name} назначен объекту {go.name}");
            }
            else
            {
                Debug.LogError($"[GlobalAnimatorLoader] Не найден компонент Animator у объекта {go.name}");
            }
        }
        else
        {
            Debug.LogError($"[GlobalAnimatorLoader] Не удалось загрузить контроллер по адресу: {address}");
        }
        if (!wasActive)
            go.SetActive(false);
    }
}