//using UnityEngine;
//using UnityEngine.UI;
//using YG;

//public class SensitivitySlider : MonoBehaviour
//{
//    [SerializeField] private Slider sensitivitySlider;

//    void Start()
//    {
//        if (YandexGame.EnvironmentData.isMobile)
//        {
//            sensitivitySlider.minValue = 0.2f;
//            sensitivitySlider.maxValue = 25f;
//        }
//        else
//        {
//            sensitivitySlider.minValue = 10f;
//            sensitivitySlider.maxValue = 500f;
//        }

//        sensitivitySlider.value = YandexGame.savesData.sensitivity;
//        sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
//    }

//    private void UpdateSensitivity(float newSensitivity)
//    {
//        CameraLook.Instance.UpdateSensitivity(newSensitivity);
//    }
//}