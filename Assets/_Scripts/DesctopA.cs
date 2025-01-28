using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class DesctopASetFalse : MonoBehaviour
{

    private void Start()
    {
        if (!YandexGame.EnvironmentData.isDesktop)
        {
            gameObject.SetActive(false);
        }
    }

}
