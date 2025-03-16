using UnityEngine;

public class MeteorSpawnerActivator: MonoBehaviour
{
    private void OnEnable()
    {
        MeteorSpawner.Instance.StartSpawning();
    }
    private void OnDisable()
    {
        MeteorSpawner.Instance.StopSpawning();
    }
}