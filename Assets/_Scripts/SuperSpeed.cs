using Cysharp.Threading.Tasks;
using UnityEngine;

public class SuperSpeed : MonoBehaviour
{
    public float m_Duration;
    public float newSpeed;
    bool isActive;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<BaffController>(out BaffController baff))
        {
            if (!baff.IsSuperSpeedActive)
            {
                StartBaff(baff);
            }
            baff.IsSuperSpeedActive = true;

        }
    }

    async UniTask StartBaff(BaffController baff)
    {
        float oldForce = PlayerController.instance.m_MoveSpeed;
        PlayerController.instance.m_MoveSpeed = newSpeed;
        CameraEffects.instance.SetSpeedFov();
        await UniTask.WaitForSeconds(m_Duration);
        PlayerController.instance.m_MoveSpeed = oldForce;
        CameraEffects.instance.DoDefaultFov();
        baff.IsSuperSpeedActive = false;
    }
}
