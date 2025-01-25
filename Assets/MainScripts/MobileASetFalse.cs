using UnityEngine;
using UnityEngine.Rendering.Universal;
using YG;

public class MobileASetFalse : MonoBehaviour
{
    private void Start()
    {

        if (!YandexGame.EnvironmentData.isMobile)
        {
            gameObject.SetActive(false);
        }
    }
}
