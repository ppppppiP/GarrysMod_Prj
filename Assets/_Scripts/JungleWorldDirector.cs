using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum JungleBiomeType
{
    [InspectorName("Джунгли")]
    Jungle,
    [InspectorName("Руины")]
    Ruins,
    [InspectorName("Болото")]
    Swamp,
    [InspectorName("Водопады")]
    Waterfall,
    [InspectorName("Древний храм")]
    Temple
}

public enum JungleTurnInputResult
{
    NotInWindow,
    Correct,
    Wrong
}

[Serializable]
public sealed class JungleBiomeZone
{
    [InspectorName("Название зоны")]
    public string displayName = "Джунгли";
    [InspectorName("Тип зоны")]
    public JungleBiomeType type = JungleBiomeType.Jungle;
    [InspectorName("Длина зоны"), Min(20f)]
    public float length = 70f;
    [InspectorName("Множитель деревьев"), Range(0f, 2f)]
    public float treeDensityMultiplier = 1f;
    [InspectorName("Множитель руин"), Range(0f, 3f)]
    public float ruinChanceMultiplier = 1f;
    [InspectorName("Множитель лиан"), Range(0f, 3f)]
    public float vineChanceMultiplier = 1f;
    [InspectorName("Цвет ближнего плана")]
    public Color nearTint = Color.white;
    [InspectorName("Цвет дальней дымки")]
    public Color farFogColor = new Color(0.30f, 0.58f, 0.44f, 1f);
    [InspectorName("Цвет окружения")]
    public Color ambientColor = new Color(0.45f, 0.48f, 0.38f, 1f);
    [InspectorName("Цвет солнца")]
    public Color sunColor = new Color(1f, 0.88f, 0.67f, 1f);
    [InspectorName("Яркость солнца"), Range(0.2f, 2f)]
    public float sunIntensity = 1.1f;
}

[DisallowMultipleComponent]
public sealed class JungleWorldDirector : MonoBehaviour
{
    private static readonly int GradientNearId = Shader.PropertyToID("_JR_DepthGradientNear");
    private static readonly int GradientFarId = Shader.PropertyToID("_JR_DepthGradientFar");
    private static readonly int GradientParametersId = Shader.PropertyToID("_JR_DepthGradientParameters");

    [Header("Цветовые зоны")]
    [InspectorName("Зоны по порядку"), Tooltip("После последней зоны последовательность начинается заново.")]
    public JungleBiomeZone[] zones;
    [InspectorName("Длина перехода"), Tooltip("Расстояние плавного смешивания между соседними зонами.")]
    [Min(1f)] public float zoneTransitionDistance = 18f;
    [InspectorName("Начало цветной глубины")]
    [Min(0f)] public float gradientStartDistance = 12f;
    [InspectorName("Конец цветной глубины")]
    [Min(10f)] public float gradientEndDistance = 105f;
    [InspectorName("Сила цветной дымки"), Range(0f, 1f)]
    public float gradientStrength = 0.72f;

    [Header("Редкие повороты")]
    [InspectorName("Включить повороты")]
    public bool turnsEnabled = true;
    [InspectorName("Минимальная дистанция между поворотами"), Min(50f)]
    public float minimumTurnSpacing = 180f;
    [InspectorName("Максимальная дистанция между поворотами"), Min(60f)]
    public float maximumTurnSpacing = 300f;
    [InspectorName("Дистанция предупреждения"), Min(20f)]
    public float turnWarningDistance = 58f;
    [InspectorName("Окно ввода"), Tooltip("На этой дистанции свайп в нужную сторону считается поворотом, а не сменой полосы.")]
    [Range(3f, 18f)] public float turnInputDistance = 10f;
    [InspectorName("Безопасная зона"), Tooltip("В этом радиусе вокруг поворота препятствия не генерируются.")]
    [Range(8f, 30f)] public float turnSafeRadius = 18f;

    [Header("Деревья на повороте")]
    [InspectorName("Префаб деревьев поворота"), Tooltip("Если не задан, используется основной префаб дерева генератора окружения.")]
    public GameObject cornerTreePrefab;
    [InspectorName("Количество маленьких деревьев"), Range(4, 14)]
    public int cornerTreeCount = 10;
    [InspectorName("Отступ деревьев от дороги"), Range(5.5f, 12f)]
    public float cornerTreeDistance = 7.2f;
    [InspectorName("Диапазон размера"), Tooltip("Маленькие и средние деревья, расставленные только по безопасным внешним сторонам поворота.")]
    public Vector2 cornerTreeScaleRange = new Vector2(0.55f, 1.05f);

    [Header("Редкие постановочные события")]
    [InspectorName("Включить события")]
    public bool stagedEventsEnabled = true;
    [InspectorName("Минимальный интервал"), Min(50f)]
    public float minimumEventSpacing = 180f;
    [InspectorName("Максимальный интервал"), Min(60f)]
    public float maximumEventSpacing = 320f;
    [InspectorName("Дистанция появления"), Range(35f, 100f)]
    public float eventSpawnDistance = 68f;
    [InspectorName("Префаб падающего дерева")]
    public GameObject fallingTreePrefab;
    [InspectorName("Префаб птицы")]
    public GameObject birdPrefab;
    [InspectorName("Префаб рушащейся колонны")]
    public GameObject collapsingColumnPrefab;
    [InspectorName("Префаб водопада")]
    public GameObject waterfallPrefab;

    private Camera runnerCamera;
    private Light sun;
    private JungleWorldBend worldBend;
    private Material woodMaterial;
    private Material stoneMaterial;
    private Material waterMaterial;
    private Material accentMaterial;
    private Material roadMaterial;
    private Material foliageMaterial;
    private float travelledDistance;
    private Vector3 trackOrigin;
    private Vector3 trackForward = Vector3.forward;

    private Transform turnMarker;
    private Transform turnArrow;
    private float turnRemainingDistance;
    private int requiredTurnDirection;
    private int turnVisualDirection;
    private bool turnAwaitingInput;
    private bool turnInputAccepted;
    private bool turnCommitQueued;
    private float turnExitBlend;
    private Transform cornerTreesRoot;
    private Vector3[] cornerTreeLayout;
    private int laidOutTurnDirection;

    private readonly Transform[] eventPool = new Transform[4];
    private Transform activeEvent;
    private int activeEventIndex = -1;
    private float eventRemainingDistance;
    private float eventAge;
    private int eventSide;
    private float activeEventDistance;
    private float activeEventLateral;
    private float activeEventHeight;

    public bool HasUpcomingTurn { get { return turnsEnabled && (turnAwaitingInput || turnInputAccepted); } }
    public float UpcomingTurnDistance { get { return turnRemainingDistance; } }
    public int UpcomingTurnDirection { get { return requiredTurnDirection; } }

    public float CycleLength
    {
        get
        {
            float total = 0f;
            if (zones != null)
                for (int i = 0; i < zones.Length; i++)
                    if (zones[i] != null) total += Mathf.Max(1f, zones[i].length);
            return Mathf.Max(1f, total);
        }
    }

    private void Reset()
    {
        CreateDefaultZones();
    }

    private void OnValidate()
    {
        maximumTurnSpacing = Mathf.Max(minimumTurnSpacing, maximumTurnSpacing);
        maximumEventSpacing = Mathf.Max(minimumEventSpacing, maximumEventSpacing);
        gradientEndDistance = Mathf.Max(gradientStartDistance + 1f, gradientEndDistance);
        if (zones == null || zones.Length == 0) CreateDefaultZones();
    }

    public void Initialize(Camera camera, Light directionalSun, JungleWorldBend bend, Material road, Material wood, Material stone, Material water, Material accent, Material foliage, GameObject defaultCornerTree)
    {
        runnerCamera = camera;
        sun = directionalSun;
        worldBend = bend;
        roadMaterial = road;
        woodMaterial = wood;
        stoneMaterial = stone;
        waterMaterial = water;
        accentMaterial = accent;
        foliageMaterial = foliage;
        if (cornerTreePrefab == null) cornerTreePrefab = defaultCornerTree;
        CreateTurnMarker();
        CreateEventPool();
        ResetRuntime();
    }

    public void ResetRuntime()
    {
        travelledDistance = 0f;
        turnExitBlend = 0f;
        turnInputAccepted = false;
        turnCommitQueued = false;
        ScheduleTurn();
        eventRemainingDistance = UnityEngine.Random.Range(minimumEventSpacing, maximumEventSpacing);
        StopActiveEvent();
        ApplyZoneVisuals();
    }

    public JungleBiomeZone GetBiomeAtDistance(float distance)
    {
        if (zones == null || zones.Length == 0) return null;
        float cursor = Mathf.Repeat(distance, CycleLength);
        for (int i = 0; i < zones.Length; i++)
        {
            JungleBiomeZone zone = zones[i];
            if (zone == null) continue;
            if (cursor < zone.length) return zone;
            cursor -= zone.length;
        }
        return zones[0];
    }

    public bool Advance(float movement, float delta)
    {
        travelledDistance += movement;
        ApplyZoneVisuals();
        bool missedTurn = UpdateTurn(movement, delta);
        UpdateStagedEvent(movement, delta);
        return missedTurn;
    }

    public void SetTrackFrame(Vector3 origin, Vector3 forward)
    {
        trackOrigin = origin;
        trackForward = forward.sqrMagnitude > 0.1f ? forward.normalized : Vector3.forward;
    }

    public JungleTurnInputResult HandleTurnInput(int direction)
    {
        if (turnInputAccepted) return JungleTurnInputResult.Correct;
        if (!turnsEnabled || !turnAwaitingInput || turnRemainingDistance > turnInputDistance)
            return JungleTurnInputResult.NotInWindow;

        if (direction != requiredTurnDirection)
            return JungleTurnInputResult.Wrong;

        turnVisualDirection = requiredTurnDirection;
        turnAwaitingInput = false;
        turnInputAccepted = true;
        return JungleTurnInputResult.Correct;
    }

    public bool TryConsumeTurnCommit(out int direction)
    {
        direction = 0;
        if (!turnCommitQueued) return false;
        direction = requiredTurnDirection;
        turnCommitQueued = false;
        turnInputAccepted = false;
        turnExitBlend = 1f;
        if (turnMarker != null) turnMarker.gameObject.SetActive(false);
        ScheduleTurn();
        return true;
    }

    public bool IsInsideTurnSafeZone(float z)
    {
        return turnsEnabled && (turnAwaitingInput || turnInputAccepted) && Mathf.Abs(z - turnRemainingDistance) < turnSafeRadius;
    }

    public float MoveOutsideTurnSafeZone(float z)
    {
        if (!IsInsideTurnSafeZone(z)) return z;
        return turnRemainingDistance + turnSafeRadius + 4f;
    }

    private bool UpdateTurn(float movement, float delta)
    {
        if (!turnsEnabled)
        {
            if (turnMarker != null) turnMarker.gameObject.SetActive(false);
            return false;
        }

        turnRemainingDistance -= movement;
        bool warningVisible = (turnAwaitingInput || turnInputAccepted) && turnRemainingDistance <= turnWarningDistance;
        if (turnMarker != null)
        {
                turnMarker.gameObject.SetActive(warningVisible);
            if (warningVisible)
            {
                turnMarker.position = trackOrigin + trackForward * turnRemainingDistance;
                turnMarker.localScale = Vector3.one;
                turnMarker.rotation = Quaternion.LookRotation(trackForward, Vector3.up);
                if (turnArrow != null) turnArrow.localRotation = Quaternion.Euler(0f, requiredTurnDirection > 0 ? 180f : 0f, 0f);
                LayoutCornerTrees(requiredTurnDirection);
                JungleBiomeZone turnZone = GetBiomeAtDistance(travelledDistance + Mathf.Max(0f, turnRemainingDistance));
                bool allowTrees = turnZone == null || (turnZone.type != JungleBiomeType.Swamp && turnZone.type != JungleBiomeType.Waterfall);
                if (cornerTreesRoot != null) cornerTreesRoot.gameObject.SetActive(allowTrees);
            }
        }

        if (turnInputAccepted && turnRemainingDistance <= 0f)
        {
            turnCommitQueued = true;
            return false;
        }

        if (turnExitBlend > 0f) turnExitBlend = Mathf.Max(0f, turnExitBlend - delta * 1.8f);
        return turnAwaitingInput && turnRemainingDistance < -1.2f;
    }

    private void ScheduleTurn()
    {
        if (!turnsEnabled)
        {
            turnAwaitingInput = false;
            turnInputAccepted = false;
            turnCommitQueued = false;
            return;
        }
        turnRemainingDistance = UnityEngine.Random.Range(minimumTurnSpacing, maximumTurnSpacing);
        requiredTurnDirection = UnityEngine.Random.value < 0.5f ? -1 : 1;
        if (turnExitBlend <= 0f) turnVisualDirection = requiredTurnDirection;
        turnAwaitingInput = true;
        turnInputAccepted = false;
        turnCommitQueued = false;
        if (turnMarker != null) turnMarker.gameObject.SetActive(false);
    }

    private void ApplyZoneVisuals()
    {
        if (zones == null || zones.Length == 0) return;
        float cursor = Mathf.Repeat(travelledDistance, CycleLength);
        int index = 0;
        while (index < zones.Length - 1 && zones[index] != null && cursor >= zones[index].length)
        {
            cursor -= zones[index].length;
            index++;
        }
        JungleBiomeZone current = zones[index] ?? zones[0];
        JungleBiomeZone next = zones[(index + 1) % zones.Length] ?? current;
        float blendStart = Mathf.Max(0f, current.length - zoneTransitionDistance);
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(blendStart, current.length, cursor));
        Color near = Color.Lerp(current.nearTint, next.nearTint, blend);
        Color far = Color.Lerp(current.farFogColor, next.farFogColor, blend);
        Color ambient = Color.Lerp(current.ambientColor, next.ambientColor, blend);
        Color sunlight = Color.Lerp(current.sunColor, next.sunColor, blend);
        float intensity = Mathf.Lerp(current.sunIntensity, next.sunIntensity, blend);

        RenderSettings.fogColor = far;
        RenderSettings.ambientLight = ambient;
        if (runnerCamera != null) runnerCamera.backgroundColor = far;
        if (sun != null)
        {
            sun.color = sunlight;
            sun.intensity = intensity;
        }
        Shader.SetGlobalVector(GradientNearId, near);
        Shader.SetGlobalVector(GradientFarId, far);
        Shader.SetGlobalVector(GradientParametersId, new Vector4(gradientStartDistance, gradientEndDistance, gradientStrength, 0f));
    }

    private void CreateTurnMarker()
    {
        if (turnMarker != null) return;
        turnMarker = new GameObject("Поворот — визуальный указатель").transform;
        turnMarker.SetParent(transform);
        CreatePrimitive(PrimitiveType.Cube, "Угловая секция дороги", turnMarker, new Vector3(0f, -0.18f, 0f), new Vector3(8f, 0.3f, 8f), roadMaterial);
        CreateMeshObject("Каменный завал впереди", turnMarker, new Vector3(0f, 0f, 1f), JungleProceduralMeshFactory.CreateRockBarrier(), stoneMaterial);
        turnArrow = new GameObject("Стрелка поворота").transform;
        turnArrow.SetParent(turnMarker, false);
        CreateMeshObject("Цельная стрелка", turnArrow, new Vector3(0f, 2.7f, 0f), JungleProceduralMeshFactory.CreateArrow(), accentMaterial);
        CreateCornerTrees();
        turnMarker.gameObject.SetActive(false);
    }

    private void CreateCornerTrees()
    {
        cornerTreesRoot = new GameObject("Безопасные деревья поворота").transform;
        cornerTreesRoot.SetParent(turnMarker, false);
        int count = Mathf.Clamp(cornerTreeCount, 4, 14);
        cornerTreeLayout = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            GameObject tree;
            if (cornerTreePrefab != null)
            {
                tree = Instantiate(cornerTreePrefab, cornerTreesRoot);
                tree.name = "Маленькое дерево поворота " + (i + 1);
                tree.SetActive(true);
                OptimizeVisual(tree);
            }
            else
            {
                tree = new GameObject("Маленькое дерево поворота " + (i + 1));
                tree.transform.SetParent(cornerTreesRoot, false);
                CreateMeshObject("Ствол", tree.transform, new Vector3(0f, 1.4f, 0f), JungleProceduralMeshFactory.CreateColumn(0.22f, 2.8f, 6), woodMaterial);
                CreateMeshObject("Крона", tree.transform, new Vector3(0f, 3.1f, 0f), JungleProceduralMeshFactory.CreateRock(1.1f, 1700 + i), foliageMaterial);
            }
            float scale = Mathf.Lerp(cornerTreeScaleRange.x, cornerTreeScaleRange.y, Mathf.Repeat(i * 0.618f + 0.21f, 1f));
            tree.transform.localScale = Vector3.one * Mathf.Max(0.2f, scale);
            tree.transform.localRotation = Quaternion.Euler(0f, Mathf.Repeat(i * 137.5f, 360f), 0f);
        }
        laidOutTurnDirection = 0;
    }

    private void LayoutCornerTrees(int direction)
    {
        if (cornerTreesRoot == null || cornerTreeLayout == null || laidOutTurnDirection == direction) return;
        laidOutTurnDirection = direction;
        float d = Mathf.Max(5.5f, cornerTreeDistance);
        int count = cornerTreesRoot.childCount;
        for (int i = 0; i < count; i++)
        {
            Vector3 p;
            if (i < 3)
            {
                p = new Vector3(-d - (i % 2) * 1.3f, 0f, -11f + i * 8f);
            }
            else
            {
                float exit = Mathf.Lerp(d, d + 18f, (i - 3f) / Mathf.Max(1f, count - 4f));
                float outside = (i & 1) == 0 ? d + 1.2f : -d;
                p = new Vector3(exit, 0f, outside);
            }
            p.x *= direction;
            cornerTreeLayout[i] = p;
            Transform tree = cornerTreesRoot.GetChild(i);
            tree.localPosition = p;
            tree.localRotation = Quaternion.Euler(0f, Mathf.Repeat(i * 137.5f + direction * 19f, 360f), 0f);
        }
    }

    private void CreateEventPool()
    {
        if (eventPool[0] != null) return;
        eventPool[0] = CreateEventObject(0, fallingTreePrefab, "Событие — падающее дерево");
        eventPool[1] = CreateEventObject(1, birdPrefab, "Событие — птица");
        eventPool[2] = CreateEventObject(2, collapsingColumnPrefab, "Событие — колонна");
        eventPool[3] = CreateEventObject(3, waterfallPrefab, "Событие — водопад");
        for (int i = 0; i < eventPool.Length; i++) eventPool[i].gameObject.SetActive(false);
    }

    private Transform CreateEventObject(int type, GameObject prefab, string objectName)
    {
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.name = objectName;
            OptimizeVisual(instance);
            return instance.transform;
        }

        Transform root = new GameObject(objectName).transform;
        root.SetParent(transform);
        if (type == 0)
        {
            Transform trunk = CreateMeshObject("Неровный ствол", root, new Vector3(0f, 3.5f, 0f), JungleProceduralMeshFactory.CreateColumn(0.55f, 7f, 7), woodMaterial).transform;
            trunk.localRotation = Quaternion.Euler(0f, 0f, 12f);
            CreateMeshObject("Крона", trunk, new Vector3(0f, 3.2f, 0f), JungleProceduralMeshFactory.CreateRock(1.8f, 741), foliageMaterial);
        }
        else if (type == 1)
        {
            CreateMeshObject("Силуэт птицы", root, Vector3.zero, JungleProceduralMeshFactory.CreateBird(), stoneMaterial);
        }
        else if (type == 2)
        {
            CreateMeshObject("Сужающаяся колонна", root, new Vector3(0f, 3f, 0f), JungleProceduralMeshFactory.CreateColumn(0.75f, 6f, 8), stoneMaterial);
            CreateMeshObject("Обломок колонны", root, new Vector3(1.1f, 0.5f, 0.4f), JungleProceduralMeshFactory.CreateRock(0.7f, 993), stoneMaterial);
        }
        else
        {
            CreateMeshObject("Неровный поток воды", root, new Vector3(0f, 5f, 0f), JungleProceduralMeshFactory.CreateWaterfall(4.5f, 10f), waterMaterial);
            CreateMeshObject("Скала слева", root, new Vector3(-2f, 9.6f, 0.5f), JungleProceduralMeshFactory.CreateRock(2.2f, 1201), stoneMaterial);
            CreateMeshObject("Скала справа", root, new Vector3(2f, 9.8f, 0.7f), JungleProceduralMeshFactory.CreateRock(2f, 1207), stoneMaterial);
        }
        return root;
    }

    private void UpdateStagedEvent(float movement, float delta)
    {
        if (!stagedEventsEnabled || eventPool[0] == null) return;
        if (activeEvent == null)
        {
            eventRemainingDistance -= movement;
            if (eventRemainingDistance <= eventSpawnDistance && (!HasUpcomingTurn || turnRemainingDistance > eventSpawnDistance + 25f))
                StartRandomEvent();
            return;
        }

        eventAge += delta;
        activeEventDistance -= movement;
        float progress = Mathf.Clamp01(eventAge / 3.2f);
        Quaternion pathRotation;
        if (activeEventIndex == 0)
        {
            activeEvent.position = GetTrackPosition(activeEventDistance, activeEventLateral, activeEventHeight, out pathRotation);
            activeEvent.rotation = pathRotation * Quaternion.Euler(0f, 0f, eventSide * Mathf.SmoothStep(0f, 78f, progress));
        }
        else if (activeEventIndex == 1)
        {
            activeEventLateral += -eventSide * delta * 12f;
            activeEvent.position = GetTrackPosition(activeEventDistance, activeEventLateral, activeEventHeight + Mathf.Sin(eventAge * 8f) * 0.18f, out pathRotation);
            activeEvent.rotation = pathRotation;
        }
        else if (activeEventIndex == 2)
        {
            activeEvent.position = GetTrackPosition(activeEventDistance, activeEventLateral, activeEventHeight, out pathRotation);
            activeEvent.rotation = pathRotation * Quaternion.Euler(0f, 0f, -eventSide * Mathf.SmoothStep(0f, 68f, progress));
        }
        else
        {
            activeEvent.position = GetTrackPosition(activeEventDistance, activeEventLateral, activeEventHeight, out pathRotation);
            activeEvent.rotation = pathRotation;
        }

        if (eventAge >= 4.5f || activeEventDistance < -14f)
        {
            StopActiveEvent();
            eventRemainingDistance = UnityEngine.Random.Range(minimumEventSpacing, maximumEventSpacing);
        }
    }

    private void StartRandomEvent()
    {
        activeEventIndex = UnityEngine.Random.Range(0, eventPool.Length);
        activeEvent = eventPool[activeEventIndex];
        eventSide = UnityEngine.Random.value < 0.5f ? -1 : 1;
        activeEventLateral = activeEventIndex == 1 ? eventSide * 22f : eventSide * (activeEventIndex == 3 ? 18f : 11f);
        activeEventHeight = activeEventIndex == 1 ? 12f : 0f;
        activeEventDistance = eventSpawnDistance;
        Quaternion pathRotation;
        activeEvent.position = GetTrackPosition(activeEventDistance, activeEventLateral, activeEventHeight, out pathRotation);
        activeEvent.rotation = pathRotation;
        activeEvent.localScale = Vector3.one;
        activeEvent.gameObject.SetActive(true);
        eventAge = 0f;
    }

    private Vector3 GetTrackPosition(float distance, float lateral, float height, out Quaternion rotation)
    {
        Vector3 forward = trackForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 position;
        if (HasUpcomingTurn && distance > turnRemainingDistance)
        {
            Vector3 pivot = trackOrigin + forward * turnRemainingDistance;
            Quaternion turn = Quaternion.AngleAxis(requiredTurnDirection * 90f, Vector3.up);
            forward = (turn * forward).normalized;
            right = (turn * right).normalized;
            position = pivot + forward * (distance - turnRemainingDistance) + right * lateral + Vector3.up * height;
        }
        else
            position = trackOrigin + forward * distance + right * lateral + Vector3.up * height;
        rotation = Quaternion.LookRotation(forward, Vector3.up);
        return position;
    }

    private void StopActiveEvent()
    {
        if (activeEvent != null) activeEvent.gameObject.SetActive(false);
        activeEvent = null;
        activeEventIndex = -1;
        eventAge = 0f;
    }

    private GameObject CreatePrimitive(PrimitiveType type, string objectName, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject result = GameObject.CreatePrimitive(type);
        result.name = objectName;
        result.transform.SetParent(parent);
        result.transform.localPosition = localPosition;
        result.transform.localScale = localScale;
        Collider collider = result.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = result.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
        return result;
    }

    private GameObject CreateMeshObject(string objectName, Transform parent, Vector3 localPosition, Mesh mesh, Material material)
    {
        GameObject result = new GameObject(objectName);
        result.transform.SetParent(parent, false);
        result.transform.localPosition = localPosition;
        result.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = result.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return result;
    }

    private void OptimizeVisual(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != null && worldBend != null) materials[i] = worldBend.GetCurvedMaterial(materials[i]);
            renderer.sharedMaterials = materials;
        }
    }

    private void CreateDefaultZones()
    {
        zones = new[]
        {
            NewZone("Густые джунгли", JungleBiomeType.Jungle, 76f, 1.25f, 0.35f, 1.2f, new Color(0.88f, 1f, 0.86f), new Color(0.24f, 0.55f, 0.34f), new Color(0.34f, 0.46f, 0.30f), new Color(1f, 0.88f, 0.63f), 1.15f),
            NewZone("Заросшие руины", JungleBiomeType.Ruins, 70f, 0.75f, 2.4f, 0.8f, new Color(0.96f, 0.98f, 0.83f), new Color(0.55f, 0.58f, 0.34f), new Color(0.42f, 0.42f, 0.28f), new Color(1f, 0.78f, 0.48f), 1.2f),
            NewZone("Бирюзовое болото", JungleBiomeType.Swamp, 68f, 0f, 0.55f, 1.45f, new Color(0.78f, 1f, 0.91f), new Color(0.18f, 0.58f, 0.56f), new Color(0.24f, 0.42f, 0.38f), new Color(0.76f, 1f, 0.82f), 0.9f),
            NewZone("Долина водопадов", JungleBiomeType.Waterfall, 64f, 0f, 0.7f, 1.5f, new Color(0.86f, 0.98f, 1f), new Color(0.34f, 0.66f, 0.72f), new Color(0.34f, 0.46f, 0.48f), new Color(0.86f, 0.95f, 1f), 1.05f),
            NewZone("Древний храм", JungleBiomeType.Temple, 78f, 0.45f, 2.8f, 0.55f, new Color(1f, 0.91f, 0.73f), new Color(0.62f, 0.43f, 0.24f), new Color(0.46f, 0.34f, 0.23f), new Color(1f, 0.68f, 0.36f), 1.3f)
        };
    }

    private static JungleBiomeZone NewZone(string name, JungleBiomeType type, float length, float trees, float ruins, float vines, Color near, Color far, Color ambient, Color sunlight, float intensity)
    {
        return new JungleBiomeZone
        {
            displayName = name,
            type = type,
            length = length,
            treeDensityMultiplier = trees,
            ruinChanceMultiplier = ruins,
            vineChanceMultiplier = vines,
            nearTint = near,
            farFogColor = far,
            ambientColor = ambient,
            sunColor = sunlight,
            sunIntensity = intensity
        };
    }
}
