using UnityEngine;
namespace UniversalMobileController
{
    public class LimitFrameRate : MonoBehaviour
    {
        [HideInInspector] public int limitFrameRate = -1;
        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
    }
}
