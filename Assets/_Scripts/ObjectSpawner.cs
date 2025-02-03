using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    // Список объектов для активации/деактивации
    [Header("СПАВНИМ ПРЕФАБ")]
    public List<GameObject> objectPrefabs;
    [Header("СПАВНИМ СОБЫТИЕ НЕ ПОВТОРЯЮЩЕЕСЯ")]
    public List<GameObject> objectPrefabs2;

    public List<GameObject> Notices;

    // Время жизни объекта в первом режиме
    [Header("ВРЕМЯ ЖИЗНИ ПРЕФАБА")]
    public float objectLifetimeMode1 = 2f;

    // Интервал между активациями в первом режиме
    [Header("ИНТЕРВАЛ СПАВНА ПРЕФАБА")]
    public float spawnIntervalMode1 = 0f;

    // Время ожидания перед активацией во втором режиме
    [Header("ВРЕМЯ СТАРТА СПАВНА СОБЫТИЯ")]
    public float spawnDelayMode2 = 5f;

    // Время жизни объекта во втором режиме
    [Header("ВРЕМЯ ЖИЗНИ СОБЫТИЯ")]
    public float objectLifetimeMode2 = 3f;

    // Интервал между активациями во втором режиме (после деактивации объекта)
    [Header("ВРЕМЯ ПОВТОРНОГО СПАВНА СОБЫТИЯ")]
    public float spawnIntervalMode2 = 4f;

    void Start()
    {
        // Инициализируем объекты: делаем их изначально неактивными
        foreach (var obj in objectPrefabs)
        {
            obj.SetActive(false);
        }

        // Запускаем корутины для обоих режимов
        StartCoroutine(SpawnMode1());
        StartCoroutine(SpawnMode2());
    }

    // Корутина для первого режима
    IEnumerator SpawnMode1()
    {
        while (true)
        {
            // Выбираем случайный объект из списка
            int randomIndex = Random.Range(0, objectPrefabs.Count);
            GameObject selectedObject = objectPrefabs[randomIndex];

            // Активируем объект
            selectedObject.SetActive(true);

            // Ждем заданное время (если интервал больше 0)
            yield return new WaitForSeconds(spawnIntervalMode1);

            // Деактивируем объект после времени его жизни
            yield return new WaitForSeconds(objectLifetimeMode1);
            selectedObject.SetActive(false);
        }
    }

    // Корутина для второго режима
    IEnumerator SpawnMode2()
    {
        int last = 0;
        while (true)
        {
            int randomIndex = Random.Range(0, objectPrefabs2.Count);

            while(randomIndex == last) 
                randomIndex = Random.Range(0, objectPrefabs2.Count);

            Notices[randomIndex].SetActive(true);
            // Ждем перед активацией
            yield return new WaitForSeconds(spawnDelayMode2);

            // Выбираем случайный объект из списка
            

            GameObject selectedObject = objectPrefabs2[randomIndex];

            Notices[randomIndex].SetActive(false);
            // Активируем объект
            selectedObject.SetActive(true);

            // Ждем время жизни объекта
            yield return new WaitForSeconds(objectLifetimeMode2);

            // Деактивируем объект
           
            selectedObject.SetActive(false);
            last = randomIndex;
            // Ждем интервал между активациями
            yield return new WaitForSeconds(spawnIntervalMode2);
        }
    }
}