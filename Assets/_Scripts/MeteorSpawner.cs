using Lean.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    // Параметры спавна
    public List<GameObject> spawnPoints; // Точки спавна (пустышки)
    public GameObject objectToSpawn; // Объект для спавна
    public int maxActiveObjects = 5; // Максимальное количество активных объектов одновременно
    public float spawnIntervalMin = 1f; // Минимальный интервал между спавнами
    public float spawnIntervalMax = 5f; // Максимальный интервал между спавнами
    public float despawnTime = 10f; // Время, через которое объект деспавнится (не случайное)
    public float gizmoLineLength = 3f; // Длина линий гизмо

    private Queue<GameObject> spawnedObjects = new Queue<GameObject>(); // Очередь спавненных объектов

    void Start()
    {
        // Запускаем корутину для спавна объектов
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // Если достигнут максимум активных объектов, ждем
            if (spawnedObjects.Count >= maxActiveObjects)
            {
                yield return null;
                continue;
            }

            // Выбираем случайную точку спавна
            if (spawnPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, spawnPoints.Count);
                Transform spawnPoint = spawnPoints[randomIndex].transform;
               
                // Спавним объект
                GameObject spawnedObject = LeanPool.Spawn(objectToSpawn, spawnPoint.position, Quaternion.identity);
                spawnedObjects.Enqueue(spawnedObject);

                // Уничтожаем объект через заданное время
                LeanPool.Despawn(spawnedObject, despawnTime);

                // Ждем случайное время до следующего спавна
                float waitTime = Random.Range(spawnIntervalMin, spawnIntervalMax);
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                Debug.LogWarning("Нет точек спавна!");
                yield return null;
            }
        }
    }

    void OnDrawGizmos()
    {
        // Рисуем красные линии и сферы для каждой точки спавна
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.color = Color.red;
                Vector3 startPoint = spawnPoint.transform.position;
                Vector3 endPoint = startPoint - Vector3.up * gizmoLineLength;

                // Линия
                Gizmos.DrawLine(startPoint, endPoint);

                // Сфера на конце линии
                Gizmos.DrawSphere(endPoint, 0.2f); // Размер сферы можно изменить
            }
        }
    }
}
