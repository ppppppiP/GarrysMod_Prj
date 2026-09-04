using UnityEngine;

[DisallowMultipleComponent]
public sealed class JungleRoadGenerator : MonoBehaviour
{
    [Header("Road and lanes")]
    [Min(1.8f)] public float laneWidth = 2.35f;
    [Range(6, 20)] public int roadSegmentCount = 10;
    [Min(4f)] public float roadSegmentLength = 8f;
    public GameObject roadSegmentPrefab;

    [Header("Gameplay generation")]
    [Range(6, 24)] public int obstacleCount = 10;
    [Range(8, 40)] public int coinCount = 18;
    [Min(4f)] public float obstacleSpacing = 8.5f;
    [Min(1.5f)] public float coinSpacing = 3.7f;
    public int generationSeed = 73015;

    [Header("Pickups")]
    public GameObject coinPrefab;

    [Header("Player")]
    public GameObject carPrefab;

    [Header("Obstacle prefabs (fixed gameplay order)")]
    public GameObject rootPrefab;
    public GameObject spikesPrefab;
    public GameObject barrierPrefab;
    public GameObject highLogPrefab;
    public GameObject floorSawPrefab;
    public GameObject rollingRockPrefab;

    public GameObject[] GetObstaclePrefabs()
    {
        return new[] { rootPrefab, spikesPrefab, barrierPrefab, highLogPrefab, floorSawPrefab, rollingRockPrefab };
    }
}
