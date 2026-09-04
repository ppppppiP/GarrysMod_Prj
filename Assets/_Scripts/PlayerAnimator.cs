using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Animator animator;
    public string walkAnimation = "Walk";
    public string idleAnimation = "Idle";
    public string jumpAnimation = "Jump";

    private PlayerController playerController;
    private bool isWalking = false; // Флаг для отслеживания состояния ходьбы
    private bool wasGrounded = true; // Флаг для отслеживания состояния нахождения на земле
    private bool jumpRequested = false;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        // Получение входных данных для движения
        float moveZ = Input.GetAxisRaw("Vertical");
        float moveX = Input.GetAxisRaw("Horizontal");
        if (playerController != null)
        {
            moveX += playerController.MobileMoveInput.x;
            moveZ += playerController.MobileMoveInput.y;
        }
        bool isMoving = moveZ != 0 || moveX != 0;

        // Батуты вызывают PlayerController.Jump из физического события, поэтому
        // CharacterController.isGrounded может обновиться только на следующем кадре.
        // Обрабатываем такой прыжок сразу, чтобы Walk не успел перезаписать Jump.
        if (jumpRequested)
        {
            jumpRequested = false;
            animator.CrossFade(jumpAnimation, 0.2f);
            isWalking = false;
            wasGrounded = false;
            return;
        }

        // В воздухе нельзя включать ходьбу, даже если игрок продолжает нажимать WASD.
        // В Player.controller отдельного состояния полёта нет, поэтому используем Jump
        // как воздушную анимацию до момента приземления.
        if (!playerController.isGrounded)
        {
            if (wasGrounded || isWalking)
            {
                animator.CrossFade(jumpAnimation, 0.2f);
                isWalking = false;
            }

            wasGrounded = false;
            return;
        }

        // Если игрок только что приземлился, сразу выбрать правильную наземную анимацию.
        if (!wasGrounded)
        {
            animator.CrossFade(isMoving ? walkAnimation : idleAnimation, 0.2f);
            isWalking = isMoving;
            wasGrounded = true;
            return;
        }

        // Проверка состояния ходьбы
        if (isMoving && !isWalking)
        {
            // Переход к анимации ходьбы, если игрок начал двигаться
            animator.CrossFade(walkAnimation, 0.2f);
            isWalking = true;
        }
        else if (!isMoving && isWalking && playerController.isGrounded)
        {
            // Переход к анимации покоя, если игрок остановился
            animator.CrossFade(idleAnimation, 0.2f);
            isWalking = false;
        }

        // Проверка состояния прыжка
        if (Input.GetButtonDown("Jump") && playerController.isGrounded)
        {
            // Переход к анимации прыжка
            animator.CrossFade(jumpAnimation, 0.2f);
        }

    }

    public void NotifyJump()
    {
        jumpRequested = true;
    }
}
