using UnityEngine;
using YG;

public class FullScrinAdd: MonoBehaviour
{

    private void Awake()
    {
        YandexGame.CloseFullAdEvent += HideCursor;
    }
    public void ShowAdd()
    {
        Debug.Log("РЕКЛАМА");
        YandexGame.FullscreenShow();
    }

    public void HideCursor()
    {
        if (YandexGame.EnvironmentData.isDesktop) {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        } 
    }
}