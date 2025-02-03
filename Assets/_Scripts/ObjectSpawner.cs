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
        if (objectPrefabs.Count == 0 || objectPrefabs2.Count == 0 || Notices.Count == 0)
        {
            Debug.LogError("Один из списков объектов пуст! Пожалуйста, добавьте объекты в инспекторе.");
            return;
        }

        if (objectPrefabs2.Count != Notices.Count)
        {
            Debug.LogError("Списки objectPrefabs2 и Notices должны иметь одинаковую длину!");
            return;
        }

        foreach (var obj in objectPrefabs)
        {
            obj.SetActive(false);
        }

        StartCoroutine(SpawnMode1());
        StartCoroutine(SpawnMode2());
    }

    IEnumerator SpawnMode1()
    {
        while (true)
        {
            int randomIndex = Random.Range(0, objectPrefabs.Count);
            GameObject selectedObject = objectPrefabs[randomIndex];

            selectedObject.SetActive(true);
            yield return new WaitForSeconds(spawnIntervalMode1);
            yield return new WaitForSeconds(objectLifetimeMode1);
            selectedObject.SetActive(false);
        }
    }

    IEnumerator SpawnMode2()
    {
        int last = -1;

        while (true)
        {
            int randomIndex;

            if (objectPrefabs2.Count > 1)
            {
                do
                {
                    randomIndex = Random.Range(0, objectPrefabs2.Count);
                } while (randomIndex == last);
            }
            else
            {
                randomIndex = 0;
            }

            Notices[randomIndex].SetActive(true);
            yield return new WaitForSeconds(spawnDelayMode2);

            GameObject selectedObject = objectPrefabs2[randomIndex];
            selectedObject.SetActive(true);

            Notices[randomIndex].SetActive(false);
            yield return new WaitForSeconds(objectLifetimeMode2);

            selectedObject.SetActive(false);

            last = randomIndex;
            yield return new WaitForSeconds(spawnIntervalMode2);
        }
    }
}