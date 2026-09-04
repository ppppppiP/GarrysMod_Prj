using UnityEngine;

[DisallowMultipleComponent]
public sealed class JungleEnvironmentGenerator : MonoBehaviour
{
    [Header("Основные настройки окружения")]
    [InspectorName("Количество кластеров"), Tooltip("Количество повторяющихся групп окружения.")]
    [Range(1, 64)] public int clusterCount = 8;
    [InspectorName("Интервал кластеров"), Tooltip("Расстояние между группами вдоль дороги.")]
    [Min(5f)] public float clusterSpacing = 10f;

    [Header("Расстояние от центра дороги")]
    [InspectorName("Минимальная дистанция"), Tooltip("Ближайшее возможное расстояние спавна от X = 0.")]
    [Min(3f)] public float minimumDistanceFromCenter = 5.2f;
    [InspectorName("Максимальная дистанция"), Tooltip("Самое дальнее возможное расстояние спавна от X = 0.")]
    [Min(3f)] public float maximumDistanceFromCenter = 8f;
    [InspectorName("Отступ деревьев от края дороги"), Tooltip("Дополнительное свободное пространство между краем дороги и ближайшей частью дерева.")]
    [Min(0.5f)] public float treeRoadEdgeClearance = 2.5f;

    [Header("Плотность и размер деревьев")]
    [InspectorName("Деревьев с каждой стороны"), Tooltip("Количество деревьев с каждой стороны дороги в одном кластере.")]
    [Range(1, 8)] public int treesPerSide = 1;
    [InspectorName("Минимальный масштаб дерева")]
    [Min(0.1f)] public float minimumTreeScale = 0.9f;
    [InspectorName("Максимальный масштаб дерева")]
    [Min(0.1f)] public float maximumTreeScale = 1.15f;
    [InspectorName("Разброс вдоль дороги"), Tooltip("Случайное смещение дерева вперёд или назад внутри кластера.")]
    [Min(0f)] public float forwardJitter = 2.5f;
    [InspectorName("Случайный поворот деревьев"), Tooltip("Случайно поворачивает деревья вокруг вертикальной оси.")]
    public bool randomizeTreeRotation = true;

    [Header("Руины")]
    [InspectorName("Вероятность появления"), Tooltip("Вероятность появления группы руин в каждом кластере окружения.")]
    [Range(0f, 1f)] public float ruinSpawnChance = 0.35f;
    [InspectorName("Минимум руин в кластере"), Tooltip("Минимальное количество руин, если группа появилась.")]
    [Range(0, 8)] public int minimumRuinsPerCluster = 1;
    [InspectorName("Максимум руин в кластере"), Tooltip("Максимальное количество руин, если группа появилась.")]
    [Range(0, 8)] public int maximumRuinsPerCluster = 2;
    [InspectorName("Минимальная дистанция от центра"), Tooltip("Ближайшее возможное расстояние руин от центра дороги.")]
    [Min(0f)] public float minimumRuinDistanceFromCenter = 5.5f;
    [InspectorName("Максимальная дистанция от центра"), Tooltip("Самое дальнее возможное расстояние руин от центра дороги.")]
    [Min(0f)] public float maximumRuinDistanceFromCenter = 9f;
    [InspectorName("Разброс вдоль дороги"), Tooltip("Случайное смещение руин вперёд или назад относительно центра кластера.")]
    [Min(0f)] public float ruinForwardJitter = 4f;
    [InspectorName("Минимальный масштаб")]
    [Min(0.1f)] public float minimumRuinScale = 0.85f;
    [InspectorName("Максимальный масштаб")]
    [Min(0.1f)] public float maximumRuinScale = 1.25f;
    [InspectorName("Смещение по высоте"), Tooltip("Позволяет поднять или опустить все руины относительно поверхности.")]
    public float ruinHeightOffset;
    [InspectorName("Случайный поворот"), Tooltip("Случайно поворачивает каждую руину вокруг вертикальной оси.")]
    public bool randomizeRuinRotation = true;

    [Header("Лианы над дорогой")]
    [InspectorName("Префаб лиан"), Tooltip("Лёгкий префаб, который растягивается между дальними сторонами дороги.")]
    public GameObject vinePrefab;
    [InspectorName("Вероятность появления")]
    [Range(0f, 1f)] public float vineSpawnChance = 0.15f;
    [InspectorName("Средняя высота")]
    [Min(1f)] public float vineHeight = 5.2f;
    [InspectorName("Разброс высоты")]
    [Min(0f)] public float vineHeightVariation = 0.8f;
    [InspectorName("Среднее провисание")]
    [Min(0.1f)] public float vineSag = 1.4f;
    [InspectorName("Разброс провисания"), Tooltip("Случайно увеличивает или уменьшает провисание каждой связки.")]
    [Min(0f)] public float vineSagVariation = 0.65f;
    [InspectorName("Запас за краями камеры"), Tooltip("Дополнительный запас за границей экрана. Необходимая длина рассчитывается автоматически.")]
    [Min(0f)] public float vineEndpointExtension = 6f;
    [InspectorName("Диагональ по глубине"), Tooltip("Один конец оказывается дальше по дороге, а другой ближе к игроку.")]
    [Min(0f)] public float vineDepthVariation = 10f;
    [InspectorName("Разница высоты концов"), Tooltip("Независимое вертикальное смещение каждого конца лианы.")]
    [Min(0f)] public float vineEndHeightVariation = 1.2f;
    [InspectorName("Толщина линии"), Tooltip("Фиксированная толщина LineRenderer, не зависит от длины и провисания.")]
    [Min(0.005f)] public float vineWidth = 0.055f;
    [InspectorName("Плавность дуги"), Tooltip("Количество точек LineRenderer. 10–14 обычно достаточно для мобильных устройств.")]
    [Range(6, 20)] public int vineCurveSegments = 12;
    [InspectorName("Изгиб в сторону"), Tooltip("Небольшая случайная волна, чтобы лианы не выглядели одинаковыми.")]
    [Min(0f)] public float vineSideWave = 0.45f;

    [Header("Варианты лиан")]
    [InspectorName("Вероятность двойной лианы"), Tooltip("Добавляет вторую дугу в тот же объект. Используется общий материал.")]
    [Range(0f, 1f)] public float doubleVineChance = 0.28f;
    [InspectorName("Вероятность оборванной лианы"), Tooltip("Создаёт разрыв в середине лианы.")]
    [Range(0f, 1f)] public float brokenVineChance = 0.18f;
    [InspectorName("Вероятность листвы"), Tooltip("Добавляет один объединённый меш листьев, а не отдельный Renderer для каждого листа.")]
    [Range(0f, 1f)] public float vineLeavesChance = 0.35f;
    [InspectorName("Количество листьев")]
    [Range(1, 6)] public int vineLeafCount = 3;
    [InspectorName("Расстояние между двойными лианами")]
    [Min(0.05f)] public float doubleVineOffset = 0.55f;
    [InspectorName("Размер разрыва")]
    [Range(0.05f, 0.45f)] public float brokenVineGap = 0.18f;

    [Header("Префабы окружения")]
    [InspectorName("Дерево")]
    public GameObject treePrefab;
    [InspectorName("Руины")]
    public GameObject ruinPrefab;
    [InspectorName("Камень")]
    public GameObject boulderPrefab;

    private void OnValidate()
    {
        maximumDistanceFromCenter = Mathf.Max(minimumDistanceFromCenter, maximumDistanceFromCenter);
        maximumTreeScale = Mathf.Max(minimumTreeScale, maximumTreeScale);
        maximumRuinsPerCluster = Mathf.Max(minimumRuinsPerCluster, maximumRuinsPerCluster);
        maximumRuinDistanceFromCenter = Mathf.Max(minimumRuinDistanceFromCenter, maximumRuinDistanceFromCenter);
        maximumRuinScale = Mathf.Max(minimumRuinScale, maximumRuinScale);
    }
}
