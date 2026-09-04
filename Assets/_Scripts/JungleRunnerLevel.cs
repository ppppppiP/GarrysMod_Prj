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
        public bool checkedCollision;
    }

    private sealed class Pickup
    {
        public GameObject gameObject;
        public PickupKind kind;
        public int lane;
        public float baseY;
        public bool collected;
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
    [SerializeField, Range(30, 120)] private int targetFrameRate = 60;
    [SerializeField] private bool enableFog = true;
    [SerializeField] private bool enableRealtimeShadows;

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
    private int environmentClusterCount = 8;
    private float environmentClusterSpacing = 10f;

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly List<Pickup> pickups = new List<Pickup>();
    private readonly List<Transform> scrollingDecor = new List<Transform>();
    private readonly List<Transform> roadSegments = new List<Transform>();

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
        Application.targetFrameRate = targetFrameRate;
        Screen.orientation = ScreenOrientation.Portrait;
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
            coinPrefab = roadGenerator.coinPrefab;
            carModel = roadGenerator.carPrefab;
            obstacleModels = roadGenerator.GetObstaclePrefabs();
        }
        if (environmentGenerator != null)
        {
            environmentClusterCount = environmentGenerator.clusterCount;
            environmentClusterSpacing = environmentGenerator.clusterSpacing;
            treeModel = environmentGenerator.treePrefab;
            ruinModel = environmentGenerator.ruinPrefab;
            boulderModel = environmentGenerator.boulderPrefab;
        }
    }

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        roadMaterial = MakeMaterial(shader, new Color(0.18f, 0.16f, 0.12f), "Runner Road");
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
        return material;
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

        for (int i = 0; i < roadSegmentCount; i++)
        {
            float z = -roadSegmentLength + i * roadSegmentLength;
            Transform road;
            if (roadSegmentModel != null)
                road = CloneModel(roadSegmentModel, "Road Segment", transform, new Vector3(0f, 0f, z)).transform;
            else
                road = Primitive(PrimitiveType.Cube, "Road Segment", transform, new Vector3(0f, -0.18f, z), new Vector3(8f, 0.3f, 7.8f), roadMaterial).transform;
            roadSegments.Add(road);
            if (roadSegmentModel == null)
            {
                for (int lane = -1; lane <= 0; lane++)
                {
                    Transform marker = Primitive(PrimitiveType.Cube, "Lane Marker", road, new Vector3((lane + 0.5f) * laneWidth, 0.25f, 0f), new Vector3(0.08f, 0.025f, 5.4f), laneMaterial).transform;
                    marker.localPosition = new Vector3((lane + 0.5f) * laneWidth, 0.25f, 0f);
                }
            }
        }

        for (int i = 0; i < environmentClusterCount; i++)
            CreateRoadsideCluster(-8f + i * environmentClusterSpacing, i);

        CreateCar();
        CreateBoulder();

        for (int i = 0; i < obstacleCount; i++)
            CreateObstacle((ObstacleKind)(i % 6), Random.Range(-1, 2), 18f + i * obstacleSpacing);

        for (int i = 0; i < coinCount; i++)
            CreatePickup(PickupKind.Coin, (i % 3) - 1, 11f + i * coinSpacing, i % 8 == 4 ? 1.9f : 0.95f);

        CreatePickup(PickupKind.Magnet, -1, 42f, 1.05f);
        CreatePickup(PickupKind.DoubleCoins, 0, 76f, 1.05f);
        CreatePickup(PickupKind.Shield, 1, 108f, 1.05f);
    }

    private void CreateRoadsideCluster(float z, int index)
    {
        GameObject cluster = new GameObject("Jungle Cluster " + index);
        cluster.transform.SetParent(transform);
        cluster.transform.position = new Vector3(0f, 0f, z);
        scrollingDecor.Add(cluster.transform);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * (5.2f + (index % 3));
            if (treeModel != null)
                CloneModel(treeModel, "Tree", cluster.transform, new Vector3(x, 0f, 0f));
            else
            {
                GameObject trunk = Primitive(PrimitiveType.Cylinder, "Palm Trunk", cluster.transform, new Vector3(x, 1.5f, 0f), new Vector3(0.45f, 1.5f, 0.45f), woodMaterial);
                Primitive(PrimitiveType.Sphere, "Palm Crown", trunk.transform, new Vector3(0f, 1.2f, 0f), new Vector3(3f, 1.2f, 2.2f), index % 2 == 0 ? jungleMaterial : darkGreenMaterial);
            }
            if (index % 3 == 0)
            {
                if (ruinModel != null)
                    CloneModel(ruinModel, "Ruin", cluster.transform, new Vector3(side * 4.1f, 0f, 1.8f));
                else
                {
                    Primitive(PrimitiveType.Cube, "Ruin Pillar", cluster.transform, new Vector3(side * 4.1f, 1.35f, 1.8f), new Vector3(0.85f, 2.7f, 0.85f), stoneMaterial);
                    Primitive(PrimitiveType.Cube, "Ruin Cap", cluster.transform, new Vector3(side * 4.1f, 2.85f, 1.8f), new Vector3(1.25f, 0.28f, 1.25f), stoneMaterial);
                }
            }
        }
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
        Obstacle obstacle = new Obstacle { gameObject = root, kind = kind, lane = lane };
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
        pickups.Add(new Pickup { gameObject = pickupObject, kind = kind, lane = lane, baseY = y });
    }

    private static GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
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
        }
        return result;
    }

    private static GameObject CloneModel(GameObject model, string objectName, Transform parent, Vector3 localPosition)
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
        }
        return result;
    }

    private void BeginNewRun()
    {
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
            obstacles[i].gameObject.transform.position = new Vector3(obstacles[i].lane * laneWidth, 0f, 18f + i * obstacleSpacing);
            obstacles[i].checkedCollision = false;
            obstacles[i].gameObject.SetActive(true);
        }
        for (int i = 0; i < pickups.Count; i++)
        {
            Pickup pickup = pickups[i];
            float z = pickup.kind == PickupKind.Coin ? 11f + i * coinSpacing : 42f + i * 19f;
            pickup.gameObject.transform.position = new Vector3(pickup.lane * laneWidth, pickup.baseY, z);
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

    private void UpdateVehicle(float delta)
    {
        float targetX = targetLane * laneWidth;
        float x = Mathf.SmoothDamp(car.position.x, targetX, ref laneVelocity, 0.095f, Mathf.Infinity, delta);
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
        car.position = new Vector3(x, CarBaseY + jumpHeight - crouch + bob, 0f);
        carBody.localRotation = Quaternion.Euler(IsGrounded ? Mathf.Sin(runTime * 9f) * 1.5f : -verticalVelocity * 1.2f, laneVelocity * -2f, 0f);
        spring.localScale = new Vector3(0.32f, IsCrouching ? 0.14f : IsGrounded ? 0.34f + bob : 0.62f, 0.32f);
        boulder.Rotate(Vector3.right, currentSpeed * 18f * delta, Space.Self);

        bool visible = invulnerableRemaining <= 0f || ((int)(flashTimer * 12f) & 1) == 0;
        carBody.gameObject.SetActive(visible);
    }

    private void UpdateWorld(float delta)
    {
        float movement = currentSpeed * delta;
        float roadLoopLength = roadSegmentCount * roadSegmentLength;
        float environmentLoopLength = environmentClusterCount * environmentClusterSpacing;
        for (int i = 0; i < roadSegments.Count; i++)
        {
            Transform road = roadSegments[i];
            road.position += Vector3.back * movement;
            if (road.position.z < -12f) road.position += Vector3.forward * roadLoopLength;
        }
        for (int i = 0; i < scrollingDecor.Count; i++)
        {
            Transform decoration = scrollingDecor[i];
            decoration.position += Vector3.back * movement;
            if (decoration.position.z < -14f) decoration.position += Vector3.forward * environmentLoopLength;
        }

        if (!gameplayGenerationActive)
        {
            emptyRoadRemaining = Mathf.Max(0f, emptyRoadRemaining - delta);
            if (emptyRoadRemaining > 0f) return;
            gameplayGenerationActive = true;
            SetGameplayObjectsActive(true);
        }

        float farthestObstacle = 20f;
        for (int i = 0; i < obstacles.Count; i++) farthestObstacle = Mathf.Max(farthestObstacle, obstacles[i].gameObject.transform.position.z);
        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle obstacle = obstacles[i];
            obstacle.gameObject.transform.position += Vector3.back * movement;
            if (!obstacle.checkedCollision && obstacle.gameObject.transform.position.z < 1.15f && obstacle.gameObject.transform.position.z > -1.15f)
            {
                obstacle.checkedCollision = true;
                CheckObstacle(obstacle);
            }
            if (obstacle.kind == ObstacleKind.Saw) obstacle.gameObject.transform.Rotate(Vector3.up, 420f * delta);
            if (obstacle.kind == ObstacleKind.Rock) obstacle.gameObject.transform.Rotate(Vector3.right, -180f * delta);
            if (obstacle.gameObject.transform.position.z < -8f)
            {
                farthestObstacle += Random.Range(6.5f, 10.5f);
                obstacle.lane = Random.Range(-1, 2);
                obstacle.gameObject.transform.position = new Vector3(obstacle.lane * laneWidth, 0f, farthestObstacle);
                obstacle.checkedCollision = false;
            }
        }

        float farthestPickup = 16f;
        for (int i = 0; i < pickups.Count; i++) farthestPickup = Mathf.Max(farthestPickup, pickups[i].gameObject.transform.position.z);
        for (int i = 0; i < pickups.Count; i++)
        {
            Pickup pickup = pickups[i];
            if (!pickup.collected)
            {
                pickup.gameObject.transform.position += Vector3.back * movement;
                pickup.gameObject.transform.Rotate(Vector3.up, 150f * delta, Space.World);
                float zDistance = Mathf.Abs(pickup.gameObject.transform.position.z);
                bool magnetized = pickup.kind == PickupKind.Coin && magnetRemaining > 0f && zDistance < 10f;
                if (magnetized)
                {
                    pickup.gameObject.transform.position = Vector3.MoveTowards(pickup.gameObject.transform.position, car.position + Vector3.up * 0.4f, 15f * delta);
                }
                if (zDistance < 0.9f && (magnetized || (Mathf.Abs(pickup.gameObject.transform.position.x - car.position.x) < 0.9f && Mathf.Abs(pickup.gameObject.transform.position.y - car.position.y) < 1.15f)))
                    CollectPickup(pickup);
            }
            if (pickup.collected || pickup.gameObject.transform.position.z < -7f)
            {
                farthestPickup += pickup.kind == PickupKind.Coin ? Random.Range(2.4f, 4.0f) : Random.Range(32f, 48f);
                pickup.lane = Random.Range(-1, 2);
                pickup.baseY = pickup.kind == PickupKind.Coin && Random.value > 0.75f ? 1.85f : 0.95f;
                if (pickup.kind == PickupKind.Coin)
                    farthestPickup = FindSafeCoinZ(farthestPickup, pickup.lane);
                pickup.gameObject.transform.position = new Vector3(pickup.lane * laneWidth, pickup.baseY, farthestPickup);
                pickup.collected = false;
                pickup.gameObject.SetActive(true);
            }
        }
    }

    private void CheckObstacle(Obstacle obstacle)
    {
        if (Mathf.Abs(obstacle.gameObject.transform.position.x - car.position.x) > 1.0f) return;
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
                if (obstacle.lane == lane && Mathf.Abs(obstacle.gameObject.transform.position.z - desiredZ) < 2.5f)
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
            if (obstacles[i].gameObject.transform.position.z < 12f) obstacles[i].gameObject.transform.position += Vector3.forward * 24f;
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
