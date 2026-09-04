using UnityEngine;

[DisallowMultipleComponent]
public sealed class JungleRoadGenerator : MonoBehaviour
{
    [Header("Дорога и полосы")]
    [InspectorName("Ширина полосы"), Tooltip("Расстояние между центрами соседних полос.")]
    [Min(1.8f)] public float laneWidth = 2.35f;
    [InspectorName("Количество секций дороги"), Tooltip("Сколько секций дороги одновременно находится в цикле.")]
    [Range(6, 20)] public int roadSegmentCount = 10;
    [InspectorName("Длина секции дороги"), Tooltip("Длина одной секции по направлению движения.")]
    [Min(4f)] public float roadSegmentLength = 8f;
    [InspectorName("Префаб секции дороги")]
    public GameObject roadSegmentPrefab;
    [InspectorName("Текстура покрытия"), Tooltip("Повторяющаяся текстура применяется и к прямым секциям, и к секции поворота.")]
    public Texture2D roadTexture;
    [InspectorName("Повтор текстуры"), Tooltip("Количество повторов текстуры по ширине и длине одной секции дороги.")]
    public Vector2 roadTextureTiling = Vector2.one;

    [Header("Генерация игрового процесса")]
    [InspectorName("Количество препятствий")]
    [Range(6, 24)] public int obstacleCount = 10;
    [InspectorName("Количество монет")]
    [Range(8, 40)] public int coinCount = 18;
    [InspectorName("Интервал препятствий")]
    [Min(4f)] public float obstacleSpacing = 8.5f;
    [InspectorName("Интервал монет")]
    [Min(1.5f)] public float coinSpacing = 3.7f;
    [InspectorName("Ключ генерации"), Tooltip("Одинаковое значение создаёт одинаковую последовательность объектов.")]
    public int generationSeed = 73015;

    [Header("Подбираемые предметы")]
    [InspectorName("Префаб монеты")]
    public GameObject coinPrefab;

    [Header("Игрок")]
    [InspectorName("Префаб машины")]
    public GameObject carPrefab;

    [Header("Префабы препятствий")]
    [InspectorName("Корни")]
    public GameObject rootPrefab;
    [InspectorName("Шипы")]
    public GameObject spikesPrefab;
    [InspectorName("Барьер")]
    public GameObject barrierPrefab;
    [InspectorName("Высокое бревно")]
    public GameObject highLogPrefab;
    [InspectorName("Напольная пила")]
    public GameObject floorSawPrefab;
    [InspectorName("Катящийся камень")]
    public GameObject rollingRockPrefab;

    public GameObject[] GetObstaclePrefabs()
    {
        return new[] { rootPrefab, spikesPrefab, barrierPrefab, highLogPrefab, floorSawPrefab, rollingRockPrefab };
    }
}
