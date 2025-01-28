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

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        // Получение входных данных для движения
        float moveZ = Input.GetAxisRaw("Vertical");
        float moveX = Input.GetAxisRaw("Horizontal");
        bool isMoving = moveZ != 0 || moveX != 0;

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

        // Обновление флага нахождения на земле
        if (playerController.isGrounded != wasGrounded)
        {
            wasGrounded = playerController.isGrounded;

            // Если игрок приземлился, вернуться к анимации покоя или ходьбы
            if (playerController.isGrounded)
            {
                if (isMoving)
                {
                    animator.CrossFade(walkAnimation, 0.2f);
                }
                else
                {
                    animator.CrossFade(idleAnimation, 0.2f);
                }
            }
        }
    }
}