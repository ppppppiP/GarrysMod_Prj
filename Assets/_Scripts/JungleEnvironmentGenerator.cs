using UnityEngine;

[DisallowMultipleComponent]
public sealed class JungleEnvironmentGenerator : MonoBehaviour
{
    [Header("Background generation only")]
    [Range(4, 20)] public int clusterCount = 8;
    [Min(5f)] public float clusterSpacing = 10f;
    public GameObject treePrefab;
    public GameObject ruinPrefab;
    public GameObject boulderPrefab;
}
