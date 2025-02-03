using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    // ������ �������� ��� ���������/�����������
    [Header("������� ������")]
    public List<GameObject> objectPrefabs;
    [Header("������� ������� �� �������������")]
    public List<GameObject> objectPrefabs2;

    public List<GameObject> Notices;

    // ����� ����� ������� � ������ ������
    [Header("����� ����� �������")]
    public float objectLifetimeMode1 = 2f;

    // �������� ����� ����������� � ������ ������
    [Header("�������� ������ �������")]
    public float spawnIntervalMode1 = 0f;

    // ����� �������� ����� ���������� �� ������ ������
    [Header("����� ������ ������ �������")]
    public float spawnDelayMode2 = 5f;

    // ����� ����� ������� �� ������ ������
    [Header("����� ����� �������")]
    public float objectLifetimeMode2 = 3f;

    // �������� ����� ����������� �� ������ ������ (����� ����������� �������)
    [Header("����� ���������� ������ �������")]
    public float spawnIntervalMode2 = 4f;

    void Start()
    {
        // �������������� �������: ������ �� ���������� �����������
        foreach (var obj in objectPrefabs)
        {
            obj.SetActive(false);
        }

        // ��������� �������� ��� ����� �������
        StartCoroutine(SpawnMode1());
        StartCoroutine(SpawnMode2());
    }

    // �������� ��� ������� ������
    IEnumerator SpawnMode1()
    {
        while (true)
        {
            // �������� ��������� ������ �� ������
            int randomIndex = Random.Range(0, objectPrefabs.Count);
            GameObject selectedObject = objectPrefabs[randomIndex];

            // ���������� ������
            selectedObject.SetActive(true);

            // ���� �������� ����� (���� �������� ������ 0)
            yield return new WaitForSeconds(spawnIntervalMode1);

            // ������������ ������ ����� ������� ��� �����
            yield return new WaitForSeconds(objectLifetimeMode1);
            selectedObject.SetActive(false);
        }
    }

    // �������� ��� ������� ������
    IEnumerator SpawnMode2()
    {
        int last = 0;
        while (true)
        {
            int randomIndex = Random.Range(0, objectPrefabs2.Count);

            while(randomIndex == last) 
                randomIndex = Random.Range(0, objectPrefabs2.Count);

            Notices[randomIndex].SetActive(true);
            // ���� ����� ����������
            yield return new WaitForSeconds(spawnDelayMode2);

            // �������� ��������� ������ �� ������
            

            GameObject selectedObject = objectPrefabs2[randomIndex];

            Notices[randomIndex].SetActive(false);
            // ���������� ������
            selectedObject.SetActive(true);

            // ���� ����� ����� �������
            yield return new WaitForSeconds(objectLifetimeMode2);

            // ������������ ������
           
            selectedObject.SetActive(false);
            last = randomIndex;
            // ���� �������� ����� �����������
            yield return new WaitForSeconds(spawnIntervalMode2);
        }
    }
}