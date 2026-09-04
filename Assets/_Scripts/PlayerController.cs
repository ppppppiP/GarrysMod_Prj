using JetBrains.Annotations;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float m_MoveSpeed = 5f;
    public float m_jumpForce = 5f;
    public float m_InAirSpeedMultiplier = 2f;
    public float m_gravity = -9.81f;

    [SerializeField] Transform m_cameraTransform;
    [SerializeField] float m_rotationSpeed = 10f; // �������� ��������

    private CharacterController characterController;
    private Vector3 velocity;
    private Vector2 mobileMoveInput;
    private bool mobileJumpQueued;
    public bool isGrounded;
    public bool isHided;
    public Vector2 MobileMoveInput => mobileMoveInput;
    public static PlayerController instance;
    BaffController baffController;

    private void Awake()
    {
        instance = this;
        baffController = GetComponent<BaffController>();
    }
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (m_cameraTransform == null && Camera.main != null) m_cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float moveX = Mathf.Clamp(Input.GetAxis("Horizontal") + mobileMoveInput.x, -1f, 1f);
        float moveZ = Mathf.Clamp(Input.GetAxis("Vertical") + mobileMoveInput.y, -1f, 1f);

        if (m_cameraTransform == null && Camera.main != null) m_cameraTransform = Camera.main.transform;
        if (m_cameraTransform == null) return;

        Vector3 forward = m_cameraTransform.forward;
        Vector3 right = m_cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveZ + right * moveX;

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_rotationSpeed * Time.deltaTime);
        }

        if (isGrounded)
        {
            characterController.Move(moveDirection * m_MoveSpeed * Time.deltaTime);
        }
        else
        {
            // Preserve the boosted horizontal speed in the air.
            float airMultiplier = baffController != null && baffController.IsSuperSpeedActive
                ? 1f
                : m_InAirSpeedMultiplier;
            characterController.Move(moveDirection * m_MoveSpeed * airMultiplier * Time.deltaTime);
        }

        if ((Input.GetButtonDown("Jump") || mobileJumpQueued) && isGrounded)
        {
            Jump(m_jumpForce);
        }
        mobileJumpQueued = false;

        velocity.y += m_gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void Jump(float force)
    {
        if (CameraEffects.instance != null) CameraEffects.instance.DoJumpFov();
        velocity.y = Mathf.Sqrt(force * -2f * m_gravity);
    }

    public void SetMobileHorizontal(float value) { mobileMoveInput.x = Mathf.Clamp(value, -1f, 1f); }
    public void SetMobileVertical(float value) { mobileMoveInput.y = Mathf.Clamp(value, -1f, 1f); }
    public void RequestMobileJump()
    {
        mobileJumpQueued = true;
        PlayerAnimator playerAnimator = GetComponent<PlayerAnimator>();
        if (playerAnimator != null) playerAnimator.NotifyJump();
    }

    public void ResetMotion()
    {
        velocity = Vector3.zero;
        mobileMoveInput = Vector2.zero;
        mobileJumpQueued = false;
    }
}
