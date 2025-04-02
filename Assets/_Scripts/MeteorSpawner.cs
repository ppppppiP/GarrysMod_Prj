using Lean.Pool;
using System.Collections;
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    public static MeteorSpawner Instance { get; private set; }

    [Header("Настройки метеорита")]
    [Tooltip("Префаб метеорита с анимацией падения")]
    public GameObject meteorPrefab;
    [Tooltip("Вертикальное расстояние над землей, на котором спавнится метеорит")]
    public float spawnHeight = 20f;
    [Tooltip("Радиус вокруг игрока, в пределах которого происходит спавн")]
    public float spawnRadius = 10f;

    [Header("Настройки спавна")]
    [Tooltip("Интервал между спавнами метеоритов")]
    public float spawnInterval = 1f;
    [Tooltip("Длительность анимации падения (после чего метеорит деспавнится)")]
    public float fallDuration = 2f;

    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

    private void Awake()
    {

        Instance = this;
    }

    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            spawnCoroutine = StartCoroutine(SpawnMeteorRoutine());
        }
    }

    public void StopSpawning()
    {
        if (isSpawning)
        {
            isSpawning = false;
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
        }
    }

    private IEnumerator SpawnMeteorRoutine()
    {
        while (isSpawning)
        {
            SpawnMeteor();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Метод спавна метеорита с учётом расстояния от земли
    private void SpawnMeteor()
    {
        // Вычисляем случайную позицию в круге вокруг игрока
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        float targetX = transform.position.x + randomCircle.x;
        float targetZ = transform.position.z + randomCircle.y;

        // Определяем высоту земли в данной позиции с помощью лучевого просчёта
        Vector3 rayStart = new Vector3(targetX, 1000f, targetZ);
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
        {
            float groundY = hit.point.y;
            // Спавним метеорит на расстоянии spawnHeight над найденной землей
            Vector3 spawnPosition = new Vector3(targetX, groundY + spawnHeight, targetZ);
            GameObject meteor = LeanPool.Spawn(meteorPrefab, spawnPosition, Quaternion.identity);
            meteor.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Запускаем корутину для деспавна метеорита после завершения анимации
            StartCoroutine(DespawnMeteor(meteor, fallDuration));
        }
        else
        {
            Debug.LogWarning("Не удалось определить уровень земли в точке: " + targetX + ", " + targetZ);
        }
    }

    private IEnumerator DespawnMeteor(GameObject meteor, float delay)
    {
        yield return new WaitForSeconds(delay);
        LeanPool.Despawn(meteor);
    }
}
