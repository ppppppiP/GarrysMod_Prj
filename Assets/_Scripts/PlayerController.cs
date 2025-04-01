using JetBrains.Annotations;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float m_MoveSpeed = 5f;
    public float m_jumpForce = 5f;
    public float m_InAirSpeedMultiplier = 2f;
    public float m_gravity = -9.81f;

    float originalSpeed;
    [SerializeField] Transform m_cameraTransform;
    [SerializeField] float m_rotationSpeed = 10f; // Скорость поворота

    private CharacterController characterController;
    private Vector3 velocity;
    public bool isGrounded;
    public bool isHided;
    private float deafoulteSpeed;
    public static PlayerController instance;
    BaffController baffController;

    private void Awake()
    {
        instance = this;
        originalSpeed = m_MoveSpeed;
        baffController = GetComponent<BaffController>();
    }
    void Start()
    {
        deafoulteSpeed = m_InAirSpeedMultiplier;
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Проверка, находится ли игрок на земле
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Небольшая гравитация для предотвращения "парения"
        }

        // Получение входных данных от игрока
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Расчет направления движения относительно камеры
        Vector3 forward = m_cameraTransform.forward;
        Vector3 right = m_cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveZ + right * moveX;

        // Если игрок движется, поворачиваем его в направлении движения
        if (moveDirection.magnitude > 0.1f) // Проверяем, что игрок действительно движется
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_rotationSpeed * Time.deltaTime);
        }

        // Движение игрока
        if (isGrounded)
        {
            // На земле используем m_MoveSpeed (с учётом бафа супер скорости, если он активен)
            characterController.Move(moveDirection * m_MoveSpeed * Time.deltaTime);
        }
        else
        {
            // В воздухе: если активен супер скорость, сбрасываем эффект и используем исходную скорость
            float speed = (baffController != null && baffController.IsSuperSpeedActive) ? originalSpeed : m_MoveSpeed;
            characterController.Move(moveDirection * speed * m_InAirSpeedMultiplier * Time.deltaTime);
        }

        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump(m_jumpForce);
        }

        // Применение гравитации
        velocity.y += m_gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void Jump(float force)
    {
        CameraEffects.instance.DoJumpFov();
        velocity.y = Mathf.Sqrt(force * -2f * m_gravity);
    }
}
