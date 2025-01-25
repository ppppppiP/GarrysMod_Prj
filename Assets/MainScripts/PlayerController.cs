using UnityEngine;
using System.Collections.Generic;
using UniversalMobileController;
using YG;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    public float crouchSpeed = 2f;
    public float mouseSensitivity = 2f;
    public float crouchHeight = 1f;
    public float climbSpeed = 3f;
    //public Transform cameraTransform;
    public float headClearance = 0.5f;
    public float characterHeight = 2f;
    public float gravity = -9.81f;
    private float jumpGravityMultiplier = 1.5f;
    private float fallSpeed = 2f;
    public float slideSpeed = 10f;
    public float slideAngleThreshold = 20f;
    private CharacterController controller;
    private Vector3 velocity;
    public static PlayerController instance;
    private bool isGrounded;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private float currentSpeed;
    private bool isCrouching = false;
    private bool isClimbing = false;
    private Vector3 smoothedSlideDirection = Vector3.zero;
    private const int smoothingFrames = 5;
    private Queue<Vector3> slideDirectionQueue = new Queue<Vector3>();
    [SerializeField] FloatingJoyStick joyStick;
    float horizontal;
    float vertical;
    void Start()
    {
        instance = this;
        controller = GetComponent<CharacterController>();
        if (YandexGame.EnvironmentData.isDesktop)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        currentSpeed = speed;
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isClimbing)
        {
            HandleClimbing();
        }
        else
        {
            HandleMovement();
            if(Input.GetButtonDown("Jump"))
                Jump();
            ApplyGravity();
        }
        //HandleCameraRotation();
    }

    private void HandleMovement()
    {
       
        if (YandexGame.EnvironmentData.isDesktop)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
        }
        else
        {
            horizontal = joyStick.GetHorizontalValue();
            vertical = joyStick.GetVerticalValue();
        }
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        if (isGrounded)
        {
            RaycastHit hit;
            Vector3 sphereOrigin = transform.position + Vector3.down * 0.1f;
            bool hasHit = Physics.SphereCast(sphereOrigin, controller.radius + 0.5f, Vector3.down, out hit, controller.height / 2 + 0.1f);

            if (hasHit)
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > slideAngleThreshold)
                {
                    Vector3 slideDirection = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                    slideDirectionQueue.Enqueue(slideDirection);
                    if (slideDirectionQueue.Count > smoothingFrames) slideDirectionQueue.Dequeue();
                    smoothedSlideDirection = Vector3.zero;
                    foreach (var dir in slideDirectionQueue) smoothedSlideDirection += dir;
                    smoothedSlideDirection /= slideDirectionQueue.Count;
                    controller.Move(smoothedSlideDirection * slideSpeed * Time.deltaTime);
                    return;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (isCrouching && !IsOverheadObstructed())
            {
                StandUp();
            }
            else if (!IsOverheadObstructed())
            {
                Crouch();
            }
        }
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    public void Jump()
    {
        if (isGrounded)
        {
            velocity.y = jumpForce;
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -fallSpeed;
        }
        else
        {
            float gravityMultiplier = isGrounded ? 1 : jumpGravityMultiplier;
            velocity.y += gravity * gravityMultiplier * Time.deltaTime;
        }
        controller.Move(velocity * Time.deltaTime);
    }

    private void Crouch()
    {
        isCrouching = true;
        currentSpeed = crouchSpeed;
        controller.height = crouchHeight;
    }
    private void StandUp()
    {
        isCrouching = false;
        currentSpeed = speed;
        controller.height = characterHeight;
    }
    private bool IsOverheadObstructed()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * (controller.height);
        RaycastHit hit;
        return Physics.Raycast(rayOrigin, Vector3.up, out hit, headClearance);
    }

    private void HandleClimbing()
    {
        Vector3 climbMove = new Vector3(0, Input.GetAxis("Vertical") * climbSpeed * Time.deltaTime, 0);
        controller.Move(climbMove);

        if (Input.GetKeyDown(KeyCode.E))
        {
            isClimbing = false;
        }
    }

    //private void HandleCameraRotation()
    //{
    //    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
    //    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

    //    xRotation -= mouseY;
    //    xRotation = Mathf.Clamp(xRotation, -90f, 90f);
    //    cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    //    transform.Rotate(Vector3.up * mouseX);
    //}
}
