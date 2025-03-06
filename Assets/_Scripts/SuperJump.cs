using Cysharp.Threading.Tasks;
using UnityEngine;

public class SuperJump: MonoBehaviour
{
    public float m_Duration;
    public float newJumpFroce;
    bool isActive;
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<BaffController>(out BaffController baff))
        {
            if (!baff.IsSuperJumpActive)
            {
                StartBaff(baff);
            }
            baff.IsSuperJumpActive = true;
            
        }
    }

    async UniTask StartBaff(BaffController baff)
    {
        float oldForce = PlayerController.instance.m_jumpForce;
        PlayerController.instance.m_jumpForce = newJumpFroce;
        
        await UniTask.WaitForSeconds(m_Duration);
        PlayerController.instance.m_jumpForce = oldForce;
        baff.IsSuperJumpActive = false;
    }
}
