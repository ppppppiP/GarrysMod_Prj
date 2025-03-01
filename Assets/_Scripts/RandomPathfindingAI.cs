using UnityEngine;
using UnityEngine.AI;

public class RandomPathfindingAI : MonoBehaviour
{
    public Transform target; // Конечная цель (точка назначения)
    public float maxRandomDistance = 5f; // Максимальное расстояние для случайных точек
    public float stopChance = 0.2f; // Вероятность остановки в каждой точке (0-1)
    public float minStopTime = 1f; // Минимальное время остановки
    public float maxStopTime = 3f; // Максимальное время остановки

    private NavMeshAgent agent;
    private Animator animator; // Компонент аниматора
    private bool isStopped = false;
    private float stopTimer = 0f;
    private Vector3 currentWaypoint;
    private string currentAnimation = "Idle"; // Текущая анимация

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Получаем аниматор
        if (!agent || !animator || !target)
        {
            Debug.LogError("NavMeshAgent, Animator или Target не установлены!");
            return;
        }

        // Установить первую случайную точку
        SetNextWaypoint();
    }

    void Update()
    {
        if (isStopped)
        {
            // Если ИИ стоит, отсчитываем время остановки
            stopTimer -= Time.deltaTime;
            if (stopTimer <= 0f)
            {
                isStopped = false;
                SetNextWaypoint();

                // Плавно переходим к анимации ходьбы, если текущая анимация не Walk
                if (currentAnimation != "Walk")
                {
                    animator.CrossFade("Walk", 0.2f); // "Walk" — название состояния анимации
                    currentAnimation = "Walk";
                }
            }
            else
            {
                // Оставляем Idle анимацию, если текущая анимация не Idle
                if (currentAnimation != "Idle")
                {
                    animator.CrossFade("Idle", 0.2f); // "Idle" — название состояния анимации
                    currentAnimation = "Idle";
                }
            }
        }
        else if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Если достигли текущей точки, решаем, что делать дальше
            if (Random.value < stopChance)
            {
                // Остановиться с определенной вероятностью
                isStopped = true;
                stopTimer = Random.Range(minStopTime, maxStopTime);

                // Плавно переходим к Idle анимации, если текущая анимация не Idle
                if (currentAnimation != "Idle")
                {
                    animator.CrossFade("Idle", 0.2f);
                    currentAnimation = "Idle";
                }
            }
            else
            {
                // Продолжить движение к следующей точке
                SetNextWaypoint();

                // Плавно переходим к анимации ходьбы, если текущая анимация не Walk
                if (currentAnimation != "Walk")
                {
                    animator.CrossFade("Walk", 0.2f);
                    currentAnimation = "Walk";
                }
            }
        }
        else
        {
            // Если ИИ движется, активируем анимацию ходьбы, если текущая анимация не Walk
            if (currentAnimation != "Walk")
            {
                animator.CrossFade("Walk", 0.2f);
                currentAnimation = "Walk";
            }
        }
    }

    void SetNextWaypoint()
    {
        if (Vector3.Distance(transform.position, target.position) > agent.stoppingDistance)
        {
            // Если еще не достигли конечной цели, выбираем случайную точку поблизости
            Vector3 randomDirection = Random.onUnitSphere * maxRandomDistance;
            randomDirection += transform.position;
            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomDirection, out hit, maxRandomDistance, NavMesh.AllAreas))
            {
                currentWaypoint = hit.position;
                agent.SetDestination(currentWaypoint);
            }
            else
            {
                // Если случайная точка недоступна, движемся прямо к цели
                agent.SetDestination(target.position);
            }
        }
        else
        {
            // Если достигли цели, остановиться
            agent.Stop();

            // Плавно переходим к Idle анимации, если текущая анимация не Idle
            if (currentAnimation != "Idle")
            {
                animator.CrossFade("Idle", 0.2f);
                currentAnimation = "Idle";
            }

            Debug.Log("Цель достигнута!");
        }
    }
}