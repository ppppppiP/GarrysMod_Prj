using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public sealed class JungleRunnerLevel : MonoBehaviour
{
    private enum RunState { Intro, Countdown, Playing, Paused, Revive, Results }
    private enum ObstacleKind { Root, Spikes, Barrier, OverheadLog, Saw, Rock }
    private enum PickupKind { Coin, Magnet, DoubleCoins, Shield }

    private sealed class Obstacle
    {
        public GameObject gameObject;
        public ObstacleKind kind;
        public int lane;
        public float distance;
        public float spin;
        public bool checkedCollision;
    }

    private sealed class Pickup
    {
        public GameObject gameObject;
        public PickupKind kind;
        public int lane;
        public float baseY;
        public float distance;
        public float spin;
        public Quaternion baseRotation;
        public bool collected;
    }

    private sealed class TrackVisual
    {
        public Transform transform;
        public float distance;
        public float lateral;
        public float height;
        public float loopLength;
        public Quaternion baseRotation;
        public int environmentLayer;
        public int clearedTurnSide;
    }

    private const float CarBaseY = 0.62f;
    private const string TotalCoinsKey = "GarrysMod.TotalCoins";
    private const string BestTimeKey = "GarrysMod.JungleRunner.BestTime";
    private const string MagnetInventoryKey = "GarrysMod.Inventory.Magnet";
    private const string DoubleInventoryKey = "GarrysMod.Inventory.DoubleCoins";
    private const string ShieldInventoryKey = "GarrysMod.Inventory.Shield";

    [Header("Difficulty")]
    [SerializeField] private float startSpeed = 9f;
    [SerializeField] private float acceleration = 0.16f;
    [SerializeField] private float maximumSpeed = 22f;

    [Header("Performance")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private bool enableRealtimeShadows;
    [InspectorName("Изгиб мира"), SerializeField] private JungleWorldBend worldBend;
    [InspectorName("Зоны, события и повороты"), SerializeField] private JungleWorldDirector worldDirector;

    [Header("Separate generators")]
    [SerializeField] private JungleRoadGenerator roadGenerator;
    [SerializeField] private JungleEnvironmentGenerator environmentGenerator;
    [SerializeField] private JungleRunnerHud hud;
    [SerializeField] private JungleIntroCutscene introCutscene;

    [HideInInspector, Header("Legacy generation cache")]
    [SerializeField, Min(1.8f)] private float laneWidth = 2.35f;
    [SerializeField, Range(6, 20)] private int roadSegmentCount = 10;
    [SerializeField] private float roadSegmentLength = 8f;
    [SerializeField, Range(6, 24)] private int obstacleCount = 12;
    [SerializeField, Range(8, 40)] private int coinCount = 24;
    [SerializeField, Min(4f)] private float obstacleSpacing = 8f;
    [SerializeField, Min(1.5f)] private float coinSpacing = 3.5f;
    [SerializeField] private int generationSeed = 73015;

    [Header("Bonuses")]
    [SerializeField] private float magnetDuration = 9f;
    [SerializeField] private float doubleCoinsDuration = 10f;
    [SerializeField] private float shieldDuration = 12f;

    [HideInInspector, Header("Shared project assets")]
    [SerializeField] private GameObject coinPrefab;

    [HideInInspector, Header("Editable model templates")]
    [Tooltip("Edit these inactive objects under Editable Generation Models, or replace the references with your prefabs.")]
    [SerializeField] private GameObject carModel;
    [SerializeField] private GameObject boulderModel;
    [SerializeField] private GameObject roadSegmentModel;
    [SerializeField] private GameObject treeModel;
    [SerializeField] private GameObject ruinModel;
    [SerializeField] private GameObject[] obstacleModels = new GameObject[6];
    private JungleEnvironmentGenerator[] environmentGenerators;

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly List<Pickup> pickups = new List<Pickup>();
    private readonly List<TrackVisual> scrollingDecor = new List<TrackVisual>();
    private readonly List<TrackVisual> roadSegments = new List<TrackVisual>();

    private Transform car;
    private Transform carBody;
    private Transform spring;
    private Transform boulder;
    private Camera runnerCamera;
    private Material roadMaterial;
    private Material laneMaterial;
    private Material jungleMaterial;
    private Material darkGreenMaterial;
    private Material stoneMaterial;
    private Material woodMaterial;
    private Material goldMaterial;
    private Material redMaterial;
    private Material cyanMaterial;
    private Material violetMaterial;
    private Texture2D roadTexture;
    private Vector2 roadTextureTiling = Vector2.one;

    private RunState state;
    private int currentLane;
    private int targetLane;
    private float laneVelocity;
    private float verticalVelocity;
    private float jumpHeight;
    private float crouchRemaining;
    private float runTime;
    private int runCoins;
    private float currentSpeed;
    private float countdownEndsAt;
    private float magnetRemaining;
    private float doubleCoinsRemaining;
    private float shieldRemaining;
    private float invulnerableRemaining;
    private int paidRevives;
    private bool adReviveUsed;
    private bool analyticsSent;
    private bool swipeTracking;
    private Vector2 swipeStart;
    private float flashTimer;
    private float emptyRoadRemaining;
    private bool gameplayGenerationActive;
    private Vector3 trackForward = Vector3.forward;
    private Vector3 trackRight = Vector3.right;
    private Vector3 cameraTurnStartPosition;
    private Quaternion cameraTurnStartRotation;
    private float cameraTurnProgress = 1f;
    [SerializeField, InspectorName("Длительность поворота камеры"), Range(0.08f, 0.6f)] private float cameraTurnDuration = 0.28f;

    private bool IsGrounded { get { return jumpHeight <= 0.001f; } }
    private bool IsCrouching { get { return crouchRemaining > 0f; } }

    public int ActiveSeconds { get { return Mathf.FloorToInt(runTime); } }
    public int TotalCoins { get { return GetTotalCoins(); } }
    public int RunCoins { get { return runCoins; } }
    public int BestSeconds { get { return Mathf.FloorToInt(PlayerPrefs.GetFloat(BestTimeKey, 0f)); } }
    public int ReviveCost { get { return 50 * (1 << Mathf.Min(paidRevives, 20)); } }
    public bool CanUseAdRevive { get { return !adReviveUsed; } }
    public bool IsCountingDown { get { return state == RunState.Countdown; } }
    public bool IsIntroPlaying { get { return state == RunState.Intro; } }
    public bool IsPaused { get { return state == RunState.Paused; } }
    public bool IsWaitingForRevive { get { return state == RunState.Revive; } }
    public bool IsShowingResults { get { return state == RunState.Results; } }
    public int CountdownNumber { get { return Mathf.Clamp(Mathf.CeilToInt(countdownEndsAt - Time.unscaledTime), 1, 3); } }
    public float MagnetSeconds { get { return magnetRemaining; } }
    public float DoubleSeconds { get { return doubleCoinsRemaining; } }
    public float ShieldSeconds { get { return shieldRemaining; } }
    public int MagnetInventory { get { return PlayerPrefs.GetInt(MagnetInventoryKey, 1); } }
    public int DoubleInventory { get { return PlayerPrefs.GetInt(DoubleInventoryKey, 1); } }
    public int ShieldInventory { get { return PlayerPrefs.GetInt(ShieldInventoryKey, 1); } }

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Screen.orientation = ScreenOrientation.Portrait;
        if (worldBend == null) worldBend = GetComponent<JungleWorldBend>();
        if (worldDirector == null) worldDirector = GetComponent<JungleWorldDirector>();
        ApplyGeneratorSettings();
        Random.InitState(generationSeed);
        CreateMaterials();
        CreateWorld();
        BeginNewRun();
    }

    private void ApplyGeneratorSettings()
    {
        if (roadGenerator != null)
        {
            laneWidth = roadGenerator.laneWidth;
            roadSegmentCount = roadGenerator.roadSegmentCount;
            roadSegmentLength = roadGenerator.roadSegmentLength;
            obstacleCount = roadGenerator.obstacleCount;
            coinCount = roadGenerator.coinCount;
            obstacleSpacing = roadGenerator.obstacleSpacing;
            coinSpacing = roadGenerator.coinSpacing;
            generationSeed = roadGenerator.generationSeed;
            roadSegmentModel = roadGenerator.roadSegmentPrefab;
            roadTexture = roadGenerator.roadTexture;
            roadTextureTiling = roadGenerator.roadTextureTiling;
            coinPrefab = roadGenerator.coinPrefab;
            carModel = roadGenerator.carPrefab;
            obstacleModels = roadGenerator.GetObstaclePrefabs();
        }
        environmentGenerators = GetComponentsInChildren<JungleEnvironmentGenerator>(true);
        JungleEnvironmentGenerator primaryEnvironment = environmentGenerator != null
            ? environmentGenerator
            : environmentGenerators != null && environmentGenerators.Length > 0 ? environmentGenerators[0] : null;
        if (primaryEnvironment != null)
        {
            treeModel = primaryEnvironment.treePrefab;
            ruinModel = primaryEnvironment.ruinPrefab;
            boulderModel = primaryEnvironment.boulderPrefab;
        }
    }

    private void CreateMaterials()
    {
        Shader shader = worldBend != null ? worldBend.MobileShader : null;
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        roadMaterial = MakeMaterial(shader, new Color(0.18f, 0.16f, 0.12f), "Runner Road");
        ApplyRoadTexture(roadMaterial);
        laneMaterial = MakeMaterial(shader, new Color(0.75f, 0.62f, 0.32f), "Runner Lane Marking");
        jungleMaterial = MakeMaterial(shader, new Color(0.16f, 0.48f, 0.19f), "Runner Jungle");
        darkGreenMaterial = MakeMaterial(shader, new Color(0.04f, 0.24f, 0.08f), "Runner Dark Jungle");
        stoneMaterial = MakeMaterial(shader, new Color(0.36f, 0.40f, 0.34f), "Runner Ruins");
        woodMaterial = MakeMaterial(shader, new Color(0.28f, 0.13f, 0.055f), "Runner Wood");
        goldMaterial = MakeMaterial(shader, new Color(1f, 0.67f, 0.04f), "Runner Gold");
        redMaterial = MakeMaterial(shader, new Color(0.85f, 0.08f, 0.04f), "Runner Danger");
        cyanMaterial = MakeMaterial(shader, new Color(0.05f, 0.85f, 1f), "Runner Magnet");
        violetMaterial = MakeMaterial(shader, new Color(0.65f, 0.18f, 1f), "Runner Shield");
    }

    private static Material MakeMaterial(Shader shader, Color color, string materialName)
    {
        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        material.enableInstancing = true;
        return material;
    }

    private void ApplyRoadTexture(Material material)
    {
        if (material == null || roadTexture == null) return;
        Vector2 tiling = new Vector2(Mathf.Max(0.01f, roadTextureTiling.x), Mathf.Max(0.01f, roadTextureTiling.y));
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", roadTexture);
            material.SetTextureScale("_BaseMap", tiling);
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", roadTexture);
            material.SetTextureScale("_MainTex", tiling);
        }
        material.color = Color.white;
    }

    private void CreateWorld()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = new Color(0.28f, 0.48f, 0.32f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 92f;
        RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.38f);

        GameObject cameraObject = new GameObject("Runner Camera");
        runnerCamera = cameraObject.AddComponent<Camera>();
        runnerCamera.tag = "MainCamera";
        runnerCamera.fieldOfView = 57f;
        runnerCamera.nearClipPlane = 0.15f;
        runnerCamera.farClipPlane = 130f;
        runnerCamera.backgroundColor = new Color(0.26f, 0.46f, 0.31f);
        cameraObject.transform.position = new Vector3(0f, 6.8f, -9.2f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.28f, 1f));

        GameObject lightObject = new GameObject("Jungle Sun");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.88f, 0.67f);
        sun.intensity = 1.25f;
        sun.shadows = enableRealtimeShadows ? LightShadows.Soft : LightShadows.None;
        lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

        if (worldDirector != null)
        {
            worldDirector.SetTrackFrame(Vector3.zero, trackForward);
            worldDirector.Initialize(runnerCamera, sun, worldBend, roadMaterial, woodMaterial, stoneMaterial, cyanMaterial, redMaterial, jungleMaterial, treeModel);
        }

        for (int i = 0; i < roadSegmentCount; i++)
        {
            float z = -roadSegmentLength + i * roadSegmentLength;
            Transform road;
            if (roadSegmentModel != null)
                road = CloneModel(roadSegmentModel, "Road Segment", transform, new Vector3(0f, 0f, z)).transform;
            else
                road = Primitive(PrimitiveType.Cube, "Road Segment", transform, new Vector3(0f, -0.18f, z), new Vector3(8f, 0.3f, 7.8f), roadMaterial).transform;
            roadSegments.Add(new TrackVisual
            {
                transform = road,
                distance = z,
                lateral = 0f,
                height = road.position.y,
                loopLength = roadSegmentCount * roadSegmentLength,
                baseRotation = road.rotation
            });
            if (roadSegmentModel == null)
            {
                for (int lane = -1; lane <= 0; lane++)
                {
                    Transform marker = Primitive(PrimitiveType.Cube, "Lane Marker", road, new Vector3((lane + 0.5f) * laneWidth, 0.25f, 0f), new Vector3(0.08f, 0.025f, 5.4f), laneMaterial).transform;
                    marker.localPosition = new Vector3((lane + 0.5f) * laneWidth, 0.25f, 0f);
                }
            }
        }

        if (environmentGenerators != null)
        {
            for (int generatorIndex = 0; generatorIndex < environmentGenerators.Length; generatorIndex++)
            {
                JungleEnvironmentGenerator generator = environmentGenerators[generatorIndex];
                if (generator == null || !generator.isActiveAndEnabled) continue;
                for (int i = 0; i < generator.clusterCount; i++)
                    CreateRoadsideCluster(generator, -8f + i * generator.clusterSpacing, i, generatorIndex);
            }
        }

        CreateCar();
        CreateBoulder();

        for (int i = 0; i < obstacleCount; i++)
        {
            float obstacleZ = 18f + i * obstacleSpacing;
            if (worldDirector != null) obstacleZ = worldDirector.MoveOutsideTurnSafeZone(obstacleZ);
            CreateObstacle((ObstacleKind)(i % 6), Random.Range(-1, 2), obstacleZ);
        }

        for (int i = 0; i < coinCount; i++)
            CreatePickup(PickupKind.Coin, (i % 3) - 1, 11f + i * coinSpacing, i % 8 == 4 ? 1.9f : 0.95f);

        CreatePickup(PickupKind.Magnet, -1, 42f, 1.05f);
        CreatePickup(PickupKind.DoubleCoins, 0, 76f, 1.05f);
        CreatePickup(PickupKind.Shield, 1, 108f, 1.05f);
    }

    private void CreateRoadsideCluster(JungleEnvironmentGenerator generator, float z, int index, int generatorIndex)
    {
        JungleBiomeZone biome = worldDirector != null ? worldDirector.GetBiomeAtDistance(Mathf.Max(0f, z + 8f)) : null;
        GameObject cluster = new GameObject(generator.name + " Cluster " + index);
        cluster.transform.SetParent(transform);
        cluster.transform.position = new Vector3(0f, 0f, z);
        scrollingDecor.Add(new TrackVisual
        {
            transform = cluster.transform,
            distance = z,
            lateral = 0f,
            height = 0f,
            loopLength = Mathf.Max(generator.clusterSpacing, generator.clusterCount * generator.clusterSpacing),
            baseRotation = Quaternion.identity,
            environmentLayer = generatorIndex
        });

        Vector3 leftVineAnchor = Vector3.zero;
        Vector3 rightVineAnchor = Vector3.zero;
        bool hasLeftVineAnchor = false;
        bool hasRightVineAnchor = false;
        int treesPerSide = biome != null
            ? Mathf.Clamp(Mathf.RoundToInt(generator.treesPerSide * biome.treeDensityMultiplier), 0, 8)
            : generator.treesPerSide;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int treeIndex = 0; treeIndex < treesPerSide; treeIndex++)
            {
                float roadHalfWidth = laneWidth * 1.7f;
                float minimumTreeDistance = Mathf.Max(generator.minimumDistanceFromCenter, roadHalfWidth + generator.treeRoadEdgeClearance);
                float maximumTreeDistance = Mathf.Max(minimumTreeDistance, generator.maximumDistanceFromCenter);
                float distance = Random.Range(minimumTreeDistance, maximumTreeDistance);
                float x = side * distance;
                float localZ = Random.Range(-generator.forwardJitter, generator.forwardJitter);
                GameObject tree;
                if (generator.treePrefab != null)
                    tree = CloneModel(generator.treePrefab, "Tree", cluster.transform, new Vector3(x, 0f, localZ));
                else
                {
                    tree = Primitive(PrimitiveType.Cylinder, "Palm Trunk", cluster.transform, new Vector3(x, 1.5f, localZ), new Vector3(0.45f, 1.5f, 0.45f), woodMaterial);
                    Primitive(PrimitiveType.Sphere, "Palm Crown", tree.transform, new Vector3(0f, 1.2f, 0f), new Vector3(3f, 1.2f, 2.2f), index % 2 == 0 ? jungleMaterial : darkGreenMaterial);
                }

                float treeScale = Random.Range(generator.minimumTreeScale, generator.maximumTreeScale);
                tree.transform.localScale *= treeScale;
                if (generator.randomizeTreeRotation)
                    tree.transform.localRotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                if (treeIndex == 0)
                {
                    if (side < 0)
                    {
                        leftVineAnchor = tree.transform.localPosition;
                        hasLeftVineAnchor = true;
                    }
                    else
                    {
                        rightVineAnchor = tree.transform.localPosition;
                        hasRightVineAnchor = true;
                    }
                }
            }

        }

        float ruinChance = generator.ruinSpawnChance * (biome != null ? biome.ruinChanceMultiplier : 1f);
        if (generator.ruinPrefab != null && Random.value < Mathf.Clamp01(ruinChance))
        {
            int ruinCount = Random.Range(generator.minimumRuinsPerCluster, generator.maximumRuinsPerCluster + 1);
            for (int ruinIndex = 0; ruinIndex < ruinCount; ruinIndex++)
            {
                int ruinSide = Random.value < 0.5f ? -1 : 1;
                float ruinDistance = Random.Range(generator.minimumRuinDistanceFromCenter, generator.maximumRuinDistanceFromCenter);
                float ruinZ = Random.Range(-generator.ruinForwardJitter, generator.ruinForwardJitter);
                GameObject ruin = CloneModel(generator.ruinPrefab, "Ruin", cluster.transform,
                    new Vector3(ruinSide * ruinDistance, generator.ruinHeightOffset, ruinZ));

                float ruinScale = Random.Range(generator.minimumRuinScale, generator.maximumRuinScale);
                ruin.transform.localScale *= ruinScale;
                if (generator.randomizeRuinRotation)
                    ruin.transform.localRotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }

        float vineChance = generator.vineSpawnChance * (biome != null ? biome.vineChanceMultiplier : 1f);
        if (generator.vinePrefab != null && hasLeftVineAnchor && hasRightVineAnchor && Random.value < Mathf.Clamp01(vineChance))
        {
            float vineY = generator.vineHeight + Random.Range(-generator.vineHeightVariation, generator.vineHeightVariation);
            leftVineAnchor.y = vineY + Random.Range(-generator.vineEndHeightVariation, generator.vineEndHeightVariation);
            rightVineAnchor.y = vineY + Random.Range(-generator.vineEndHeightVariation, generator.vineEndHeightVariation);
            float depthDirection = Random.value < 0.5f ? -1f : 1f;
            float depthDifference = Random.Range(generator.vineDepthVariation * 0.35f, generator.vineDepthVariation) * depthDirection;
            leftVineAnchor.z -= depthDifference * 0.5f;
            rightVineAnchor.z += depthDifference * 0.5f;

            if (runnerCamera != null)
            {
                float halfFovTangent = Mathf.Tan(runnerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                Vector3 cameraPosition = runnerCamera.transform.position;
                Vector3 cameraForward = runnerCamera.transform.forward;
                Vector3 cameraRight = runnerCamera.transform.right;

                Vector3 leftWorld = cluster.transform.TransformPoint(leftVineAnchor);
                float leftDepth = Mathf.Max(0.1f, Vector3.Dot(leftWorld - cameraPosition, cameraForward));
                float leftHalfWidth = halfFovTangent * leftDepth * runnerCamera.aspect;
                float leftCurrent = Vector3.Dot(leftWorld - cameraPosition, cameraRight);
                float leftTarget = -leftHalfWidth - generator.vineEndpointExtension;
                leftWorld += cameraRight * Mathf.Min(0f, leftTarget - leftCurrent);
                leftVineAnchor = cluster.transform.InverseTransformPoint(leftWorld);

                Vector3 rightWorld = cluster.transform.TransformPoint(rightVineAnchor);
                float rightDepth = Mathf.Max(0.1f, Vector3.Dot(rightWorld - cameraPosition, cameraForward));
                float rightHalfWidth = halfFovTangent * rightDepth * runnerCamera.aspect;
                float rightCurrent = Vector3.Dot(rightWorld - cameraPosition, cameraRight);
                float rightTarget = rightHalfWidth + generator.vineEndpointExtension;
                rightWorld += cameraRight * Mathf.Max(0f, rightTarget - rightCurrent);
                rightVineAnchor = cluster.transform.InverseTransformPoint(rightWorld);
            }
            else
            {
                leftVineAnchor.x -= generator.vineEndpointExtension;
                rightVineAnchor.x += generator.vineEndpointExtension;
            }

            Vector3 direction = rightVineAnchor - leftVineAnchor;
            float randomSag = Mathf.Max(0.1f, generator.vineSag + Random.Range(-generator.vineSagVariation, generator.vineSagVariation));
            GameObject vine = CloneModel(generator.vinePrefab, "Vines Across Road", cluster.transform, Vector3.zero);
            vine.transform.localRotation = Quaternion.identity;
            vine.transform.localScale = Vector3.one;
            LineRenderer line = vine.GetComponentInChildren<LineRenderer>(true);
            if (line != null)
            {
                float waveDirection = Random.value < 0.5f ? -1f : 1f;
                float waveAmount = Random.Range(generator.vineSideWave * 0.35f, generator.vineSideWave) * waveDirection;
                bool broken = Random.value < generator.brokenVineChance;
                float gapHalf = generator.brokenVineGap * 0.5f;
                if (broken)
                {
                    ConfigureVineLine(line, leftVineAnchor, rightVineAnchor, randomSag, waveAmount, generator.vineWidth, generator.vineCurveSegments, 0f, 0.5f - gapHalf);
                    LineRenderer brokenEnd = AddMatchingLineRenderer(line);
                    ConfigureVineLine(brokenEnd, leftVineAnchor, rightVineAnchor, randomSag, waveAmount, generator.vineWidth * 0.9f, generator.vineCurveSegments, 0.5f + gapHalf, 1f);
                }
                else
                    ConfigureVineLine(line, leftVineAnchor, rightVineAnchor, randomSag, waveAmount, generator.vineWidth, generator.vineCurveSegments, 0f, 1f);

                if (!broken && Random.value < generator.doubleVineChance)
                {
                    LineRenderer secondLine = AddMatchingLineRenderer(line);
                    Vector3 secondLeft = leftVineAnchor + Vector3.up * generator.doubleVineOffset;
                    Vector3 secondRight = rightVineAnchor + Vector3.up * (generator.doubleVineOffset * Random.Range(0.65f, 1.25f));
                    ConfigureVineLine(secondLine, secondLeft, secondRight, randomSag * Random.Range(0.75f, 1.2f), -waveAmount * 0.7f, generator.vineWidth * 0.82f, generator.vineCurveSegments, 0f, 1f);
                }

                if (Random.value < generator.vineLeavesChance)
                    CreateVineLeaves(line.transform, line.sharedMaterial, leftVineAnchor, rightVineAnchor, randomSag, waveAmount, generator.vineLeafCount);
            }
            else
            {
                vine.transform.localPosition = (leftVineAnchor + rightVineAnchor) * 0.5f;
                vine.transform.localRotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
                vine.transform.localScale = new Vector3(direction.magnitude, randomSag, 1f);
            }
        }

        if (generatorIndex == 0 && biome != null)
            CreateBiomeDecoration(cluster.transform, biome, index, generator.clusterSpacing);
    }

    private static void ConfigureVineLine(LineRenderer line, Vector3 left, Vector3 right, float sag, float wave, float width, int segments, float startT, float endT)
    {
        int pointCount = Mathf.Max(4, Mathf.RoundToInt(Mathf.Max(6, segments) * Mathf.Max(0.3f, endT - startT)));
        line.useWorldSpace = false;
        line.positionCount = pointCount;
        line.startWidth = width;
        line.endWidth = width * 0.82f;
        line.numCornerVertices = 1;
        line.numCapVertices = 0;
        for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
        {
            float t = Mathf.Lerp(startT, endT, pointIndex / (pointCount - 1f));
            line.SetPosition(pointIndex, EvaluateVinePoint(left, right, sag, wave, t));
        }
    }

    private static Vector3 EvaluateVinePoint(Vector3 left, Vector3 right, float sag, float wave, float t)
    {
        Vector3 point = Vector3.Lerp(left, right, t);
        point.y -= Mathf.Sin(t * Mathf.PI) * sag;
        point.z += Mathf.Sin(t * Mathf.PI * 2f) * wave;
        return point;
    }

    private static LineRenderer AddMatchingLineRenderer(LineRenderer source)
    {
        GameObject lineObject = new GameObject("Дополнительная линия лианы");
        lineObject.transform.SetParent(source.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = source.sharedMaterial;
        line.textureMode = source.textureMode;
        line.alignment = source.alignment;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        return line;
    }

    private void CreateBiomeDecoration(Transform cluster, JungleBiomeZone biome, int index, float spacing)
    {
        if (biome.type == JungleBiomeType.Swamp && index % 2 == 0)
        {
            float stripLength = Mathf.Max(3f, spacing * 0.98f);
            Mesh puddles = JungleProceduralMeshFactory.CreatePuddlePair(9f, 4.8f, stripLength, generationSeed + index * 19);
            CreateMeshVisual("Неровные болотные лужи", cluster, new Vector3(0f, -0.27f, 0f), puddles, cyanMaterial);
        }
        else if (biome.type == JungleBiomeType.Waterfall && index % 4 == 0)
        {
            int side = (index & 1) == 0 ? -1 : 1;
            CreateMeshVisual("Дальний водопад", cluster, new Vector3(side * 12f, 4.4f, 1.5f), JungleProceduralMeshFactory.CreateWaterfall(3.5f, 8.8f), cyanMaterial);
            CreateMeshVisual("Скала водопада", cluster, new Vector3(side * 12f, 9f, 1.7f), JungleProceduralMeshFactory.CreateRock(2.7f, generationSeed + index), stoneMaterial);
        }
    }

    private static void CreateVineLeaves(Transform parent, Material material, Vector3 left, Vector3 right, float sag, float wave, int leafCount)
    {
        leafCount = Mathf.Clamp(leafCount, 1, 6);
        const int verticesPerLeaf = 7;
        const int indicesPerLeaf = 36;
        Vector3[] vertices = new Vector3[leafCount * verticesPerLeaf];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[leafCount * indicesPerLeaf];
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < leafCount; i++)
        {
            float t = Mathf.Lerp(0.16f, 0.84f, (i + 0.5f) / leafCount) + Random.Range(-0.055f, 0.055f);
            Vector3 center = EvaluateVinePoint(left, right, sag, wave, Mathf.Clamp01(t));
            float width = Random.Range(0.16f, 0.25f);
            float height = Random.Range(0.26f, 0.42f);
            float rotation = Random.Range(-55f, 55f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rotation);
            float sin = Mathf.Sin(rotation);
            Vector2[] shape =
            {
                Vector2.zero, new Vector2(0f, height), new Vector2(width * 0.72f, height * 0.22f),
                new Vector2(width * 0.42f, -height * 0.58f), new Vector2(0f, -height),
                new Vector2(-width * 0.42f, -height * 0.58f), new Vector2(-width * 0.72f, height * 0.22f)
            };
            int vertex = i * verticesPerLeaf;
            for (int point = 0; point < verticesPerLeaf; point++)
            {
                Vector2 p = shape[point];
                Vector2 rotated = new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
                vertices[vertex + point] = center + new Vector3(rotated.x, rotated.y, point == 0 ? -0.025f : 0f);
                uvs[vertex + point] = new Vector2(rotated.x / (width * 2f) + 0.5f, rotated.y / (height * 2f) + 0.5f);
                normals[vertex + point] = Vector3.back;
            }
            int triangle = i * indicesPerLeaf;
            for (int edge = 0; edge < 6; edge++)
            {
                int a = vertex + 1 + edge;
                int b = vertex + 1 + (edge + 1) % 6;
                int offset = triangle + edge * 6;
                triangles[offset] = vertex;
                triangles[offset + 1] = b;
                triangles[offset + 2] = a;
                triangles[offset + 3] = vertex;
                triangles[offset + 4] = a;
                triangles[offset + 5] = b;
            }
        }

        Mesh mesh = new Mesh { name = "Vine Leaves Combined" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        GameObject leaves = new GameObject("Листья лианы (объединённый меш)");
        leaves.transform.SetParent(parent, false);
        leaves.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = leaves.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    private static GameObject CreateMeshVisual(string objectName, Transform parent, Vector3 localPosition, Mesh mesh, Material material)
    {
        GameObject result = new GameObject(objectName);
        result.transform.SetParent(parent, false);
        result.transform.localPosition = localPosition;
        result.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = result.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        return result;
    }

    private void CreateCar()
    {
        car = new GameObject("Spring Jungle Car").transform;
        car.SetParent(transform);
        car.position = new Vector3(0f, CarBaseY, 0f);

        spring = Primitive(PrimitiveType.Cylinder, "Spring", car, new Vector3(0f, -0.20f, 0f), new Vector3(0.32f, 0.38f, 0.32f), stoneMaterial).transform;
        carBody = new GameObject("Car Body Pivot").transform;
        carBody.SetParent(car);
        carBody.localPosition = Vector3.zero;
        if (carModel != null)
            CloneModel(carModel, "Car Visual", carBody, Vector3.zero);
        else
        {
            Primitive(PrimitiveType.Cube, "Body", carBody, new Vector3(0f, 0.35f, 0f), new Vector3(1.75f, 0.48f, 2.35f), redMaterial);
            Primitive(PrimitiveType.Cube, "Cabin", carBody, new Vector3(0f, 0.82f, -0.15f), new Vector3(1.35f, 0.62f, 1.15f), cyanMaterial);
            Primitive(PrimitiveType.Sphere, "Driver Head", carBody, new Vector3(0f, 1.30f, -0.05f), new Vector3(0.48f, 0.48f, 0.48f), goldMaterial);
            for (int side = -1; side <= 1; side += 2)
            for (int front = -1; front <= 1; front += 2)
            {
                GameObject wheel = Primitive(PrimitiveType.Cylinder, "Wheel", carBody, new Vector3(side * 0.92f, 0.1f, front * 0.72f), new Vector3(0.42f, 0.18f, 0.42f), roadMaterial);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }
    }

    private void CreateBoulder()
    {
        boulder = boulderModel != null
            ? CloneModel(boulderModel, "Chasing Boulder (Visual Only)", transform, new Vector3(0f, 1.55f, -4.8f)).transform
            : Primitive(PrimitiveType.Sphere, "Chasing Boulder (Visual Only)", transform, new Vector3(0f, 1.55f, -4.8f), new Vector3(2.7f, 2.7f, 2.7f), stoneMaterial).transform;
        if (boulderModel != null) return;
        for (int i = 0; i < 7; i++)
        {
            Transform chip = Primitive(PrimitiveType.Cube, "Boulder Chip", boulder, Random.onUnitSphere * 0.48f, Vector3.one * Random.Range(0.12f, 0.25f), roadMaterial).transform;
            chip.localRotation = Random.rotation;
        }
    }

    private void CreateObstacle(ObstacleKind kind, int lane, float z)
    {
        GameObject root = new GameObject("Obstacle " + kind);
        root.transform.SetParent(transform);
        root.transform.position = new Vector3(lane * laneWidth, 0f, z);
        Obstacle obstacle = new Obstacle { gameObject = root, kind = kind, lane = lane, distance = z };
        obstacles.Add(obstacle);

        GameObject customModel = obstacleModels != null && (int)kind < obstacleModels.Length ? obstacleModels[(int)kind] : null;
        if (customModel != null)
        {
            CloneModel(customModel, kind + " Model", root.transform, Vector3.zero);
            return;
        }

        switch (kind)
        {
            case ObstacleKind.Root:
                Primitive(PrimitiveType.Cylinder, "Root", root.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.28f, 1.0f, 0.28f), woodMaterial).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case ObstacleKind.Spikes:
                for (int i = -1; i <= 1; i++)
                {
                    Transform spike = Primitive(PrimitiveType.Cube, "Spike", root.transform, new Vector3(i * 0.42f, 0.28f, 0f), new Vector3(0.32f, 0.65f, 0.32f), stoneMaterial).transform;
                    spike.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
                break;
            case ObstacleKind.Barrier:
                Primitive(PrimitiveType.Cube, "Stone Barrier", root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(1.85f, 1.6f, 0.65f), stoneMaterial);
                break;
            case ObstacleKind.OverheadLog:
                Primitive(PrimitiveType.Cube, "High Log", root.transform, new Vector3(0f, 1.65f, 0f), new Vector3(2.0f, 0.42f, 0.55f), woodMaterial);
                Primitive(PrimitiveType.Cube, "Warning Shadow", root.transform, new Vector3(0f, 0.025f, -2.0f), new Vector3(1.7f, 0.03f, 2.2f), redMaterial);
                break;
            case ObstacleKind.Saw:
                Transform saw = Primitive(PrimitiveType.Cylinder, "Floor Saw", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.75f, 0.12f, 0.75f), redMaterial).transform;
                saw.localRotation = Quaternion.Euler(90f, 0f, 0f);
                break;
            case ObstacleKind.Rock:
                Primitive(PrimitiveType.Sphere, "Rolling Rock", root.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.45f, 1.45f, 1.45f), stoneMaterial);
                Primitive(PrimitiveType.Cube, "Warning Crack", root.transform, new Vector3(0f, 0.025f, -2.4f), new Vector3(1.4f, 0.03f, 1.7f), redMaterial);
                break;
        }
    }

    private void CreatePickup(PickupKind kind, int lane, float z, float y)
    {
        if (kind == PickupKind.Coin)
            z = FindSafeCoinZ(z, lane);
        Material material = kind == PickupKind.Coin ? goldMaterial : kind == PickupKind.Magnet ? cyanMaterial : kind == PickupKind.DoubleCoins ? redMaterial : violetMaterial;
        PrimitiveType shape = kind == PickupKind.Coin ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
        GameObject pickupObject;
        if (kind == PickupKind.Coin && coinPrefab != null)
        {
            pickupObject = CloneModel(coinPrefab, "Pickup " + kind, transform, new Vector3(lane * laneWidth, y, z));
        }
        else
        {
            pickupObject = Primitive(shape, "Pickup " + kind, transform, new Vector3(lane * laneWidth, y, z), kind == PickupKind.Coin ? new Vector3(0.42f, 0.10f, 0.42f) : Vector3.one * 0.72f, material);
            if (kind == PickupKind.Coin) pickupObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        pickups.Add(new Pickup { gameObject = pickupObject, kind = kind, lane = lane, baseY = y, distance = z, baseRotation = pickupObject.transform.rotation });
    }

    private GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject result = GameObject.CreatePrimitive(type);
        result.name = objectName;
        result.transform.SetParent(parent);
        result.transform.localPosition = position;
        result.transform.localScale = scale;
        Collider collider = result.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = result.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
        return result;
    }

    private GameObject CloneModel(GameObject model, string objectName, Transform parent, Vector3 localPosition)
    {
        GameObject result = Instantiate(model, parent);
        result.name = objectName;
        result.transform.localPosition = localPosition;
        result.transform.localRotation = model.transform.localRotation;
        result.SetActive(true);
        foreach (Collider modelCollider in result.GetComponentsInChildren<Collider>(true)) modelCollider.enabled = false;
        foreach (MonoBehaviour behaviour in result.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (Renderer modelRenderer in result.GetComponentsInChildren<Renderer>(true))
        {
            modelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            modelRenderer.receiveShadows = false;
            modelRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            modelRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            modelRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            Material[] materials = modelRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                materials[i].enableInstancing = true;
                if (worldBend != null) materials[i] = worldBend.GetCurvedMaterial(materials[i]);
            }
            modelRenderer.sharedMaterials = materials;
        }
        return result;
    }

    private void BeginNewRun()
    {
        trackForward = Vector3.forward;
        trackRight = Vector3.right;
        cameraTurnProgress = 1f;
        if (worldBend != null) worldBend.SetTrackOrientation(trackForward, trackRight);
        if (worldDirector != null) worldDirector.SetTrackFrame(Vector3.zero, trackForward);
        currentLane = targetLane = 0;
        laneVelocity = 0f;
        jumpHeight = 0f;
        verticalVelocity = 0f;
        crouchRemaining = 0f;
        runTime = 0f;
        runCoins = 0;
        paidRevives = 0;
        adReviveUsed = false;
        analyticsSent = false;
        ResetTemporaryBonuses();
        car.position = new Vector3(0f, CarBaseY, 0f);
        car.rotation = Quaternion.LookRotation(trackForward, Vector3.up);
        if (worldDirector != null) worldDirector.ResetRuntime();
        ResetObjects();
        StartIntro();
    }

    private void StartIntro()
    {
        state = RunState.Intro;
        gameplayGenerationActive = false;
        emptyRoadRemaining = introCutscene != null ? introCutscene.EmptyRoadDuration : 0f;
        SetGameplayObjectsActive(false);
        if (introCutscene != null)
        {
            introCutscene.Begin(runnerCamera, boulder);
            return;
        }

        state = RunState.Playing;
    }

    private void UpdateIntro()
    {
        if (introCutscene == null || introCutscene.Tick()) state = RunState.Playing;
    }

    private void SetGameplayObjectsActive(bool value)
    {
        for (int i = 0; i < obstacles.Count; i++) obstacles[i].gameObject.SetActive(value);
        for (int i = 0; i < pickups.Count; i++) pickups[i].gameObject.SetActive(value && !pickups[i].collected);
    }

    private void ResetObjects()
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            obstacles[i].lane = (i % 3) - 1;
            float z = 18f + i * obstacleSpacing;
            if (worldDirector != null) z = worldDirector.MoveOutsideTurnSafeZone(z);
            obstacles[i].distance = z;
            ApplyTrackPose(obstacles[i].gameObject.transform, obstacles[i].distance, obstacles[i].lane * laneWidth, 0f, Quaternion.identity);
            obstacles[i].checkedCollision = false;
            obstacles[i].gameObject.SetActive(true);
        }
        for (int i = 0; i < pickups.Count; i++)
        {
            Pickup pickup = pickups[i];
            float z = pickup.kind == PickupKind.Coin ? 11f + i * coinSpacing : 42f + i * 19f;
            pickup.distance = z;
            ApplyTrackPose(pickup.gameObject.transform, pickup.distance, pickup.lane * laneWidth, pickup.baseY, Quaternion.identity);
            pickup.collected = false;
            pickup.gameObject.SetActive(true);
        }
    }

    private void StartCountdown()
    {
        state = RunState.Countdown;
        countdownEndsAt = Time.unscaledTime + 3.2f;
    }

    private void Update()
    {
        float unscaledDelta = Time.unscaledDeltaTime;
        if (state == RunState.Intro)
        {
            UpdateIntro();
            return;
        }
        if (state == RunState.Countdown)
        {
            if (Time.unscaledTime >= countdownEndsAt) state = RunState.Playing;
            return;
        }
        if (state != RunState.Playing) return;

        float delta = Time.deltaTime;
        HandleInput();
        runTime += delta;
        currentSpeed = Mathf.Min(maximumSpeed, startSpeed + acceleration * runTime);
        UpdateVehicle(delta);
        UpdateWorld(delta);
        UpdateBonuses(delta);
        flashTimer += unscaledDelta;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(1);
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) Jump();
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Crouch();

        if (Input.touchCount == 0)
        {
            swipeTracking = false;
            return;
        }
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            swipeStart = touch.position;
            swipeTracking = true;
        }
        else if (swipeTracking && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
        {
            Vector2 swipe = touch.position - swipeStart;
            swipeTracking = false;
            if (swipe.magnitude < 45f) return;
            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y)) ChangeLane(swipe.x > 0 ? 1 : -1);
            else if (swipe.y > 0) Jump(); else Crouch();
        }
    }

    private void ChangeLane(int direction)
    {
        if (worldDirector != null)
        {
            JungleTurnInputResult turnResult = worldDirector.HandleTurnInput(direction);
            if (turnResult == JungleTurnInputResult.Correct)
            {
                return;
            }
            if (turnResult == JungleTurnInputResult.Wrong)
            {
                Die();
                return;
            }
        }
        targetLane = Mathf.Clamp(targetLane + direction, -1, 1);
    }

    private void Jump()
    {
        if (!IsGrounded || IsCrouching) return;
        verticalVelocity = 7.25f;
    }

    private void Crouch()
    {
        if (!IsGrounded) return;
        crouchRemaining = 0.85f;
    }

    private void CommitTurn(int direction)
    {
        cameraTurnStartPosition = runnerCamera != null ? runnerCamera.transform.position : Vector3.zero;
        cameraTurnStartRotation = runnerCamera != null ? runnerCamera.transform.rotation : Quaternion.identity;
        Quaternion turn = Quaternion.AngleAxis(direction * 90f, Vector3.up);
        trackForward = (turn * trackForward).normalized;
        trackRight = (turn * trackRight).normalized;
        cameraTurnProgress = 0f;
        if (worldDirector != null) worldDirector.SetTrackFrame(Vector3.zero, trackForward);
        if (worldBend != null)
        {
            worldBend.bendEnabled = true;
            worldBend.SetTrackOrientation(trackForward, trackRight);
        }
        RefreshTrackTransforms();
    }

    private void UpdateCameraTurn(float delta)
    {
        if (runnerCamera == null) return;
        Vector3 targetPosition = -trackForward * 9.2f + Vector3.up * 6.8f;
        Quaternion targetRotation = Quaternion.LookRotation(trackForward + Vector3.down * 0.28f, Vector3.up);
        if (cameraTurnProgress < 1f)
        {
            cameraTurnProgress = Mathf.Min(1f, cameraTurnProgress + delta / Mathf.Max(0.05f, cameraTurnDuration));
            float smooth = cameraTurnProgress * cameraTurnProgress * (3f - 2f * cameraTurnProgress);
            runnerCamera.transform.position = Vector3.Lerp(cameraTurnStartPosition, targetPosition, smooth);
            runnerCamera.transform.rotation = Quaternion.Slerp(cameraTurnStartRotation, targetRotation, smooth);
        }
        else
        {
            runnerCamera.transform.position = targetPosition;
            runnerCamera.transform.rotation = targetRotation;
        }
    }

    private void RefreshTrackTransforms()
    {
        for (int i = 0; i < roadSegments.Count; i++)
        {
            TrackVisual visual = roadSegments[i];
            ApplyTrackPose(visual.transform, visual.distance, visual.lateral, visual.height, visual.baseRotation);
        }
        for (int i = 0; i < scrollingDecor.Count; i++)
        {
            TrackVisual visual = scrollingDecor[i];
            ApplyTrackPose(visual.transform, visual.distance, visual.lateral, visual.height, visual.baseRotation);
        }
        for (int i = 0; i < obstacles.Count; i++)
            ApplyTrackPose(obstacles[i].gameObject.transform, obstacles[i].distance, obstacles[i].lane * laneWidth, 0f, Quaternion.identity);
        for (int i = 0; i < pickups.Count; i++)
            ApplyTrackPose(pickups[i].gameObject.transform, pickups[i].distance, pickups[i].lane * laneWidth, pickups[i].baseY, pickups[i].baseRotation);
    }

    private void ApplyTrackPose(Transform target, float distance, float lateral, float height, Quaternion baseRotation)
    {
        Vector3 segmentForward = trackForward;
        Vector3 segmentRight = trackRight;
        Vector3 position;
        if (worldDirector != null && worldDirector.HasUpcomingTurn && distance > worldDirector.UpcomingTurnDistance)
        {
            float turnDistance = worldDirector.UpcomingTurnDistance;
            Vector3 pivot = trackForward * turnDistance;
            Quaternion turn = Quaternion.AngleAxis(worldDirector.UpcomingTurnDirection * 90f, Vector3.up);
            segmentForward = (turn * trackForward).normalized;
            segmentRight = (turn * trackRight).normalized;
            position = pivot + segmentForward * (distance - turnDistance) + segmentRight * lateral + Vector3.up * height;
        }
        else
            position = trackForward * distance + trackRight * lateral + Vector3.up * height;

        target.position = position;
        target.rotation = Quaternion.LookRotation(segmentForward, Vector3.up) * baseRotation;
    }

    private static void SetCornerDecorationClearance(Transform cluster, int blockedSide)
    {
        for (int i = 0; i < cluster.childCount; i++)
        {
            Transform child = cluster.GetChild(i);
            bool isLargeGroundDecoration = child.name == "Tree" || child.name == "Ruin";
            if (!isLargeGroundDecoration) continue;
            bool occupiesTurningSide = blockedSide == 2 || (blockedSide != 0 && Mathf.Sign(child.localPosition.x) == blockedSide);
            child.gameObject.SetActive(!occupiesTurningSide);
        }
    }

    private void UpdateVehicle(float delta)
    {
        float targetX = targetLane * laneWidth;
        float currentLateral = Vector3.Dot(car.position, trackRight);
        float x = Mathf.SmoothDamp(currentLateral, targetX, ref laneVelocity, 0.095f, Mathf.Infinity, delta);
        if (Mathf.Abs(x - targetX) < 0.03f) currentLane = targetLane;

        if (!IsGrounded || verticalVelocity > 0f)
        {
            verticalVelocity -= 17.5f * delta;
            jumpHeight += verticalVelocity * delta;
            if (jumpHeight <= 0f)
            {
                jumpHeight = 0f;
                verticalVelocity = 0f;
            }
        }
        crouchRemaining = Mathf.Max(0f, crouchRemaining - delta);
        float crouch = IsCrouching ? 0.42f : 0f;
        float bob = IsGrounded && !IsCrouching ? Mathf.Sin(runTime * currentSpeed * 0.7f) * 0.025f : 0f;
        car.position = trackRight * x + Vector3.up * (CarBaseY + jumpHeight - crouch + bob);
        car.rotation = Quaternion.LookRotation(trackForward, Vector3.up);
        carBody.localRotation = Quaternion.Euler(IsGrounded ? Mathf.Sin(runTime * 9f) * 1.5f : -verticalVelocity * 1.2f, laneVelocity * -2f, 0f);
        spring.localScale = new Vector3(0.32f, IsCrouching ? 0.14f : IsGrounded ? 0.34f + bob : 0.62f, 0.32f);
        boulder.position = -trackForward * 4.8f + Vector3.up * 1.55f;
        boulder.Rotate(Vector3.right, currentSpeed * 18f * delta, Space.Self);
        UpdateCameraTurn(delta);

        bool visible = invulnerableRemaining <= 0f || ((int)(flashTimer * 12f) & 1) == 0;
        carBody.gameObject.SetActive(visible);
    }

    private void UpdateWorld(float delta)
    {
        float movement = currentSpeed * delta;
        if (worldDirector != null) worldDirector.SetTrackFrame(Vector3.zero, trackForward);
        if (worldDirector != null && worldDirector.Advance(movement, delta))
        {
            Die();
            return;
        }
        if (worldDirector != null && worldDirector.TryConsumeTurnCommit(out int committedTurn))
            CommitTurn(committedTurn);
        for (int i = 0; i < roadSegments.Count; i++)
        {
            TrackVisual road = roadSegments[i];
            road.distance -= movement;
            if (road.distance < -12f) road.distance += road.loopLength;
            ApplyTrackPose(road.transform, road.distance, road.lateral, road.height, road.baseRotation);
        }
        for (int i = 0; i < scrollingDecor.Count; i++)
        {
            TrackVisual decoration = scrollingDecor[i];
            decoration.distance -= movement;
            if (decoration.distance < -14f) decoration.distance += decoration.loopLength;
            ApplyTrackPose(decoration.transform, decoration.distance, decoration.lateral, decoration.height, decoration.baseRotation);
            int clearSide = 0;
            if (decoration.environmentLayer == 0 && worldDirector != null && worldDirector.IsInsideTurnSafeZone(decoration.distance))
                clearSide = 2;
            if (decoration.clearedTurnSide != clearSide)
            {
                SetCornerDecorationClearance(decoration.transform, clearSide);
                decoration.clearedTurnSide = clearSide;
            }
        }

        if (!gameplayGenerationActive)
        {
            emptyRoadRemaining = Mathf.Max(0f, emptyRoadRemaining - delta);
            if (emptyRoadRemaining > 0f) return;
            gameplayGenerationActive = true;
            SetGameplayObjectsActive(true);
        }

        float farthestObstacle = 20f;
        for (int i = 0; i < obstacles.Count; i++) farthestObstacle = Mathf.Max(farthestObstacle, obstacles[i].distance);
        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle obstacle = obstacles[i];
            obstacle.distance -= movement;
            if (worldDirector != null && worldDirector.IsInsideTurnSafeZone(obstacle.distance))
            {
                farthestObstacle = worldDirector.MoveOutsideTurnSafeZone(farthestObstacle + Random.Range(6.5f, 10.5f));
                obstacle.distance = farthestObstacle;
                ApplyTrackPose(obstacle.gameObject.transform, obstacle.distance, obstacle.lane * laneWidth, 0f, Quaternion.identity);
                obstacle.checkedCollision = false;
                continue;
            }
            if (obstacle.kind == ObstacleKind.Saw) obstacle.spin += 420f * delta;
            else if (obstacle.kind == ObstacleKind.Rock) obstacle.spin -= 180f * delta;
            Quaternion obstacleRotation = obstacle.kind == ObstacleKind.Saw
                ? Quaternion.Euler(0f, obstacle.spin, 0f)
                : obstacle.kind == ObstacleKind.Rock ? Quaternion.Euler(obstacle.spin, 0f, 0f) : Quaternion.identity;
            ApplyTrackPose(obstacle.gameObject.transform, obstacle.distance, obstacle.lane * laneWidth, 0f, obstacleRotation);
            if (!obstacle.checkedCollision && obstacle.distance < 1.15f && obstacle.distance > -1.15f)
            {
                obstacle.checkedCollision = true;
                CheckObstacle(obstacle);
            }
            if (obstacle.distance < -8f)
            {
                farthestObstacle += Random.Range(6.5f, 10.5f);
                if (worldDirector != null) farthestObstacle = worldDirector.MoveOutsideTurnSafeZone(farthestObstacle);
                obstacle.lane = Random.Range(-1, 2);
                obstacle.distance = farthestObstacle;
                ApplyTrackPose(obstacle.gameObject.transform, obstacle.distance, obstacle.lane * laneWidth, 0f, Quaternion.identity);
                obstacle.checkedCollision = false;
            }
        }

        float farthestPickup = 16f;
        for (int i = 0; i < pickups.Count; i++) farthestPickup = Mathf.Max(farthestPickup, pickups[i].distance);
        for (int i = 0; i < pickups.Count; i++)
        {
            Pickup pickup = pickups[i];
            if (!pickup.collected)
            {
                pickup.distance -= movement;
                pickup.spin += 150f * delta;
                ApplyTrackPose(pickup.gameObject.transform, pickup.distance, pickup.lane * laneWidth, pickup.baseY,
                    Quaternion.Euler(0f, pickup.spin, 0f) * pickup.baseRotation);
                float zDistance = Mathf.Abs(pickup.distance);
                bool magnetized = pickup.kind == PickupKind.Coin && magnetRemaining > 0f && zDistance < 10f;
                if (magnetized)
                {
                    pickup.gameObject.transform.position = Vector3.MoveTowards(pickup.gameObject.transform.position, car.position + Vector3.up * 0.4f, 15f * delta);
                }
                float lateralDistance = Mathf.Abs(Vector3.Dot(pickup.gameObject.transform.position - car.position, trackRight));
                if (zDistance < 0.9f && (magnetized || (lateralDistance < 0.9f && Mathf.Abs(pickup.gameObject.transform.position.y - car.position.y) < 1.15f)))
                    CollectPickup(pickup);
            }
            if (pickup.collected || pickup.distance < -7f)
            {
                farthestPickup += pickup.kind == PickupKind.Coin ? Random.Range(2.4f, 4.0f) : Random.Range(32f, 48f);
                pickup.lane = Random.Range(-1, 2);
                pickup.baseY = pickup.kind == PickupKind.Coin && Random.value > 0.75f ? 1.85f : 0.95f;
                if (pickup.kind == PickupKind.Coin)
                    farthestPickup = FindSafeCoinZ(farthestPickup, pickup.lane);
                pickup.distance = farthestPickup;
                ApplyTrackPose(pickup.gameObject.transform, pickup.distance, pickup.lane * laneWidth, pickup.baseY, Quaternion.identity);
                pickup.collected = false;
                pickup.gameObject.SetActive(true);
            }
        }
    }

    private void CheckObstacle(Obstacle obstacle)
    {
        float lateralDistance = Mathf.Abs(Vector3.Dot(obstacle.gameObject.transform.position - car.position, trackRight));
        if (lateralDistance > 1.0f) return;
        bool avoided = obstacle.kind == ObstacleKind.OverheadLog ? IsCrouching : (obstacle.kind == ObstacleKind.Root || obstacle.kind == ObstacleKind.Spikes || obstacle.kind == ObstacleKind.Saw) && jumpHeight > 0.72f;
        if (avoided || invulnerableRemaining > 0f) return;
        if (shieldRemaining > 0f)
        {
            shieldRemaining = 0f;
            invulnerableRemaining = 3f;
            flashTimer = 0f;
            return;
        }
        Die();
    }

    private void CollectPickup(Pickup pickup)
    {
        pickup.collected = true;
        pickup.gameObject.SetActive(false);
        if (pickup.kind == PickupKind.Coin)
        {
            int value = doubleCoinsRemaining > 0f ? 2 : 1;
            runCoins += value;
            SetTotalCoins(GetTotalCoins() + value);
        }
        else if (pickup.kind == PickupKind.Magnet) magnetRemaining += magnetDuration;
        else if (pickup.kind == PickupKind.DoubleCoins) doubleCoinsRemaining += doubleCoinsDuration;
        else shieldRemaining += shieldDuration;
    }

    private float FindSafeCoinZ(float desiredZ, int lane)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            bool overlapsObstacle = false;
            for (int i = 0; i < obstacles.Count; i++)
            {
                Obstacle obstacle = obstacles[i];
                if (obstacle.lane == lane && Mathf.Abs(obstacle.distance - desiredZ) < 2.5f)
                {
                    overlapsObstacle = true;
                    break;
                }
            }
            if (!overlapsObstacle) return desiredZ;
            desiredZ += 3f;
        }
        return desiredZ;
    }

    private void UpdateBonuses(float delta)
    {
        magnetRemaining = Mathf.Max(0f, magnetRemaining - delta);
        doubleCoinsRemaining = Mathf.Max(0f, doubleCoinsRemaining - delta);
        shieldRemaining = Mathf.Max(0f, shieldRemaining - delta);
        invulnerableRemaining = Mathf.Max(0f, invulnerableRemaining - delta);
    }

    private void ResetTemporaryBonuses()
    {
        magnetRemaining = doubleCoinsRemaining = shieldRemaining = invulnerableRemaining = 0f;
    }

    private void Die()
    {
        ResetTemporaryBonuses();
        SaveSharedWallet();
        state = RunState.Revive;
        carBody.localRotation = Quaternion.Euler(22f, 0f, 35f);
    }

    private void Revive(bool byAd)
    {
        if (byAd) adReviveUsed = true;
        else
        {
            int cost = 50 * (1 << Mathf.Min(paidRevives, 20));
            int total = GetTotalCoins();
            if (total < cost) return;
            SetTotalCoins(total - cost);
            paidRevives++;
        }
        ResetTemporaryBonuses();
        invulnerableRemaining = 3f;
        flashTimer = 0f;
        currentLane = targetLane = 0;
        car.position = new Vector3(0f, CarBaseY, 0f);
        for (int i = 0; i < obstacles.Count; i++)
            if (obstacles[i].distance < 12f)
            {
                obstacles[i].distance += 24f;
                ApplyTrackPose(obstacles[i].gameObject.transform, obstacles[i].distance, obstacles[i].lane * laneWidth, 0f, Quaternion.identity);
            }
        StartCountdown();
    }

    private void FinishRun()
    {
        state = RunState.Results;
        float best = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        if (runTime > best) PlayerPrefs.SetFloat(BestTimeKey, runTime);
        PlayerPrefs.Save();
        SaveSharedWallet();
        if (!analyticsSent)
        {
            analyticsSent = true;
            Debug.Log("JungleRunnerAttempt: activeSeconds=" + Mathf.FloorToInt(runTime) + ", coins=" + runCoins);
        }
    }

    private void ActivateInventoryBonus(PickupKind kind)
    {
        string key = kind == PickupKind.Magnet ? MagnetInventoryKey : kind == PickupKind.DoubleCoins ? DoubleInventoryKey : ShieldInventoryKey;
        int amount = PlayerPrefs.GetInt(key, 1);
        if (amount <= 0 || state != RunState.Playing) return;
        PlayerPrefs.SetInt(key, amount - 1);
        if (kind == PickupKind.Magnet) magnetRemaining += magnetDuration;
        else if (kind == PickupKind.DoubleCoins) doubleCoinsRemaining += doubleCoinsDuration;
        else shieldRemaining += shieldDuration;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && state == RunState.Playing) state = RunState.Paused;
        else if (hasFocus && state == RunState.Paused) StartCountdown();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveSharedWallet();
            if (state == RunState.Playing) state = RunState.Paused;
        }
    }



    public void TogglePause()
    {
        if (state == RunState.Playing) state = RunState.Paused;
        else if (state == RunState.Paused) StartCountdown();
    }

    public void RequestJump() { Jump(); }
    public void ActivateMagnet() { ActivateInventoryBonus(PickupKind.Magnet); }
    public void ActivateDoubleCoins() { ActivateInventoryBonus(PickupKind.DoubleCoins); }
    public void ActivateShield() { ActivateInventoryBonus(PickupKind.Shield); }
    public void RequestAdRevive() { if (state == RunState.Revive && !adReviveUsed) Revive(true); }
    public void RequestPaidRevive() { if (state == RunState.Revive) Revive(false); }
    public void RequestFinish() { if (state == RunState.Revive) FinishRun(); }
    public void RequestRestart() { if (state == RunState.Results) BeginNewRun(); }
    public void ReturnToCamp() { SceneManager.LoadScene("00 - MENU"); }









    private static int GetTotalCoins()
    {
        if (YandexGame.Instance != null && YandexGame.savesData != null)
            return YandexGame.savesData.money;
        return PlayerPrefs.GetInt(TotalCoinsKey, YandexGame.savesData != null ? YandexGame.savesData.money : 0);
    }

    private static void SetTotalCoins(int value)
    {
        value = Mathf.Max(0, value);
        PlayerPrefs.SetInt(TotalCoinsKey, value);
        if (YandexGame.savesData == null) return;
        YandexGame.savesData.money = value;
    }

    private static void SaveSharedWallet()
    {
        PlayerPrefs.Save();
        if (YandexGame.Instance != null)
            YandexGame.SaveProgress();
    }

    private void OnDestroy()
    {
        SaveSharedWallet();
        if (roadMaterial != null) Destroy(roadMaterial);
        if (laneMaterial != null) Destroy(laneMaterial);
        if (jungleMaterial != null) Destroy(jungleMaterial);
        if (darkGreenMaterial != null) Destroy(darkGreenMaterial);
        if (stoneMaterial != null) Destroy(stoneMaterial);
        if (woodMaterial != null) Destroy(woodMaterial);
        if (goldMaterial != null) Destroy(goldMaterial);
        if (redMaterial != null) Destroy(redMaterial);
        if (cyanMaterial != null) Destroy(cyanMaterial);
        if (violetMaterial != null) Destroy(violetMaterial);
    }
}
