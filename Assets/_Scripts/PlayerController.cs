using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float m_MoveSpeed = 5f;
    [SerializeField] float m_jumpForce = 5f;
    [SerializeField] float m_gravity = -9.81f;
    [SerializeField] Transform m_cameraTransform;
    [SerializeField] float m_rotationSpeed = 10f; // �������� ��������

    private CharacterController characterController;
    private Vector3 velocity;
    public bool isGrounded;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // ��������, ��������� �� ����� �� �����
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // ��������� ���������� ��� �������������� "�������"
        }

        // ��������� ������� ������ �� ������
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // ������ ����������� �������� ������������ ������
        Vector3 forward = m_cameraTransform.forward;
        Vector3 right = m_cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveZ + right * moveX;

        // ���� ����� ��������, ������������ ��� � ����������� ��������
        if (moveDirection.magnitude > 0.1f) // ���������, ��� ����� ������������� ��������
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_rotationSpeed * Time.deltaTime);
        }

        // �������� ������
        characterController.Move(moveDirection * m_MoveSpeed * Time.deltaTime);

        // ������
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(m_jumpForce * -2f * m_gravity);
        }

        // ���������� ����������
        velocity.y += m_gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}