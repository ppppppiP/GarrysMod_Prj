using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public enum BuffType { None, SuperJump, SuperSpeed, Gravity }

    private BuffType activeBuff = BuffType.None;

    [Header("Настройки бафа Супер Прыжка")]
    public float superJumpForce = 10f;
    private float originalJumpForce;

    [Header("Настройки бафа Супер Скорости")]
    public float superMoveSpeed = 10f;
    private float originalMoveSpeed;

    [Header("Настройки бафа Гравитации")]
    [Range(-100, 0)] public float gravityBuffValue = -4.9f; // например, уменьшенная сила гравитации
    public float gravityBuffInAirMultiplier = 1.0f; // новый множитель скорости в полёте
   private float originalGravity;
    private float originalInAirMultiplier;

    public GameObject Buff1Outline;
    public GameObject Buff2Outline;
    public GameObject Buff3Outline;

    private PlayerController player;
    private BaffController baff;
    private CameraEffects cameraEffects;

    private void Start()
    {
        player = PlayerController.instance;
        baff = player.GetComponent<BaffController>();
        cameraEffects = CameraEffects.instance;

        // Сохраняем оригинальные значения из PlayerController
        originalJumpForce = player.m_jumpForce;
        originalMoveSpeed = player.m_MoveSpeed;
        // Для гравитации требуется добавить методы GetGravity/SetGravity в PlayerController
        originalGravity = player.m_gravity;
        originalInAirMultiplier = player.m_InAirSpeedMultiplier;
    }

    // Методы для UI кнопок

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            OnSuperJumpButton();
        if(Input.GetKeyDown(KeyCode.Alpha2))
            OnSuperSpeedButton();
        if(Input.GetKeyDown(KeyCode.Alpha3))
            OnGravityBuffButton();

    }

    public void OnSuperJumpButton()
    {
        if (activeBuff == BuffType.SuperJump)
        {
            DeactivateCurrentBuff();
        }
        else
        {
            DeactivateCurrentBuff();
            ActivateSuperJump();
        }
    }

    public void OnSuperSpeedButton()
    {
        if (activeBuff == BuffType.SuperSpeed)
        {
            DeactivateCurrentBuff();
        }
        else
        {
            DeactivateCurrentBuff();
            ActivateSuperSpeed();
        }
    }

    public void OnGravityBuffButton()
    {
        if (activeBuff == BuffType.Gravity)
        {
            DeactivateCurrentBuff();
        }
        else
        {
            DeactivateCurrentBuff();
            ActivateGravityBuff();
        }
    }

    private void ActivateSuperJump()
    {
        activeBuff = BuffType.SuperJump;
        player.m_jumpForce = superJumpForce;
        if (baff != null)
            baff.IsSuperJumpActive = true;
        Buff1Outline.active = true;
    }

    private void ActivateSuperSpeed()
    {
        activeBuff = BuffType.SuperSpeed;
        player.m_MoveSpeed = superMoveSpeed;
        cameraEffects?.SetSpeedFov();
        if (baff != null)
            baff.IsSuperSpeedActive = true;

        Buff2Outline.active = true;
    }

    private void ActivateGravityBuff()
    {
        activeBuff = BuffType.Gravity;
        player.m_gravity = (gravityBuffValue);
        player.m_InAirSpeedMultiplier = gravityBuffInAirMultiplier;
        if (baff != null)
            baff.IsElitraActive = true;

        Buff3Outline.active = true;
    }

    private void DeactivateCurrentBuff()
    {
        switch (activeBuff)
        {
            case BuffType.SuperJump:
                player.m_jumpForce = originalJumpForce;
                if (baff != null)
                    baff.IsSuperJumpActive = false;
                Buff1Outline.active = false;
                break;
            case BuffType.SuperSpeed:
                player.m_MoveSpeed = originalMoveSpeed;
                cameraEffects?.DoDefaultFov();
                if (baff != null)
                    baff.IsSuperSpeedActive = false;

                Buff2Outline.active = false;
                break;
            case BuffType.Gravity:
                player.m_gravity =(originalGravity);
                player.m_InAirSpeedMultiplier = originalInAirMultiplier;
                if (baff != null)
                    baff.IsElitraActive = false;

                Buff3Outline.active = false;
                break;
            default:
                break;
        }
        activeBuff = BuffType.None;
    }
}