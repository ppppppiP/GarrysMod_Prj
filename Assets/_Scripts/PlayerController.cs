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
    public bool isGrounded;
    public bool isHided;
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
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

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

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump(m_jumpForce);
        }

        velocity.y += m_gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void Jump(float force)
    {
        CameraEffects.instance.DoJumpFov();
        velocity.y = Mathf.Sqrt(force * -2f * m_gravity);
    }
}
