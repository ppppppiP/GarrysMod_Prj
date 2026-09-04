#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AncientTrapHallSceneBuilder
{
    private const string Root = "Assets/_Scenes/Chips/Ancient Trap Hall";
    private const string Materials = Root + "/Materials";
    private const string Prefabs = Root + "/Prefabs";
    private const string Textures = Root + "/Textures";
    private const string Meshes = Root + "/Meshes";
    private const string Shaders = Root + "/Shaders";
    private const string ScenePath = "Assets/_Scenes/Chips/8 - ANCIENT TRAP HALL.unity";

    [MenuItem("Tools/Chips/Создать зал древних ловушек")]
    public static void Build()
    {
        EnsureFolder("Assets/_Scenes/Chips", "Ancient Trap Hall");
        EnsureFolder(Root, "Materials");
        EnsureFolder(Root, "Prefabs");
        EnsureFolder(Root, "Textures");
        EnsureFolder(Root, "Meshes");
        EnsureFolder(Root, "Shaders");

        Texture2D floorTexture = ConfigureTexture(Textures + "/ATH_Floor_Runes.png");
        Texture2D wallTexture = ConfigureTexture(Textures + "/ATH_Wall_Masonry.png");

        Material floor = MaterialAsset("ATH_Floor", Color.white, false, floorTexture, new Vector2(4f, 4f));
        Material stone = MaterialAsset("ATH_Stone", new Color(0.82f, 0.84f, 0.78f), false, wallTexture, new Vector2(3f, 1f));
        Material dark = MaterialAsset("ATH_DarkStone", new Color(0.42f, 0.47f, 0.40f), false, wallTexture, new Vector2(2f, 2f));
        Material gold = MaterialAsset("ATH_Gold", new Color(0.95f, 0.60f, 0.08f), true);
        Material bronze = MaterialAsset("ATH_Bronze", new Color(0.55f, 0.42f, 0.24f), false, floorTexture, Vector2.one);
        Material danger = MaterialAsset("ATH_Danger", new Color(1f, 0.12f, 0.035f), true);
        Material warning = MaterialAsset("ATH_Warning", new Color(1f, 0.42f, 0.04f), true);
        Material cyan = MaterialAsset("ATH_Magnet", new Color(0.05f, 0.85f, 1f), true);
        Material violet = MaterialAsset("ATH_Shield", new Color(0.57f, 0.18f, 1f), true);

        GameObject[] trapPrefabs = new GameObject[7];
        trapPrefabs[0] = CreateTrapPrefab(AncientTrapCycle.TrapKind.FloorSpikes, stone, warning, danger);
        trapPrefabs[1] = CreateTrapPrefab(AncientTrapCycle.TrapKind.FallingRock, stone, warning, danger);
        trapPrefabs[2] = CreateTrapPrefab(AncientTrapCycle.TrapKind.SweepingBeam, stone, warning, danger);
        trapPrefabs[3] = CreateTrapPrefab(AncientTrapCycle.TrapKind.WallBlock, stone, warning, danger);
        trapPrefabs[4] = CreateTrapPrefab(AncientTrapCycle.TrapKind.RotatingBeam, stone, warning, danger);
        trapPrefabs[5] = CreateTrapPrefab(AncientTrapCycle.TrapKind.Shockwave, stone, warning, danger);
        trapPrefabs[6] = CreateTrapPrefab(AncientTrapCycle.TrapKind.FallingColumn, stone, warning, danger);

        AncientHallPickup coinPrefab = CreatePickupPrefab("Coin", AncientHallPickup.PickupKind.Coin, gold, PrimitiveType.Cylinder);
        AncientHallPickup magnetPrefab = CreatePickupPrefab("Magnet", AncientHallPickup.PickupKind.Magnet, cyan, PrimitiveType.Capsule);
        AncientHallPickup doublePrefab = CreatePickupPrefab("DoubleCoins", AncientHallPickup.PickupKind.DoubleCoins, gold, PrimitiveType.Cube);
        AncientHallPickup shieldPrefab = CreatePickupPrefab("Shield", AncientHallPickup.PickupKind.Shield, violet, PrimitiveType.Sphere);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "8 - ANCIENT TRAP HALL";

        GameObject levelObject = new GameObject("Ancient Trap Hall Level");
        AncientTrapHallLevel level = levelObject.AddComponent<AncientTrapHallLevel>();

        Transform arena = new GameObject("Arena").transform;
        CreateArenaFloor(arena, floor);
        CreateCurvedBar("Wall North", arena, new Vector3(0f, 2.8f, 18f), new Vector3(37f, 5.6f, 1f), false, stone, true);
        CreateCurvedBar("Wall South", arena, new Vector3(0f, 2.8f, -18f), new Vector3(37f, 5.6f, 1f), false, stone, true);
        CreateCurvedBar("Wall East", arena, new Vector3(18f, 2.8f, 0f), new Vector3(37f, 5.6f, 1f), true, stone, true);
        CreateCurvedBar("Wall West", arena, new Vector3(-18f, 2.8f, 0f), new Vector3(37f, 5.6f, 1f), true, stone, true);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Corner Column", arena, new Vector3(x * 16.2f, 2.5f, z * 16.2f), new Vector3(1.5f, 2.5f, 1.5f), dark, false);
            CreatePrimitive(PrimitiveType.Cube, "Rune Plate", arena, new Vector3(x * 12.7f, 0.04f, z * 12.7f), new Vector3(2.8f, 0.08f, 2.8f), bronze, false);
        }

        Transform architecture = new GameObject("Architecture").transform;
        architecture.SetParent(arena);
        CreateCurvedBar("Inner Border North", architecture, new Vector3(0f, 0.18f, 16.6f), new Vector3(33.2f, 0.36f, 0.55f), false, dark, false);
        CreateCurvedBar("Inner Border South", architecture, new Vector3(0f, 0.18f, -16.6f), new Vector3(33.2f, 0.36f, 0.55f), false, dark, false);
        CreateCurvedBar("Inner Border East", architecture, new Vector3(16.6f, 0.18f, 0f), new Vector3(33.2f, 0.36f, 0.55f), true, dark, false);
        CreateCurvedBar("Inner Border West", architecture, new Vector3(-16.6f, 0.18f, 0f), new Vector3(33.2f, 0.36f, 0.55f), true, dark, false);

        Transform altar = new GameObject("Central Ritual Altar").transform;
        altar.SetParent(architecture, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Lower Step", altar, new Vector3(0f, 0.07f, 0f), new Vector3(3.0f, 0.07f, 3.0f), dark, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Rune Step", altar, new Vector3(0f, 0.15f, 0f), new Vector3(2.25f, 0.08f, 2.25f), stone, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Golden Seal", altar, new Vector3(0f, 0.245f, 0f), new Vector3(1.25f, 0.025f, 1.25f), gold, false);

        GameObject griffinAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Scenes/HML/3D/My_model/griffin-statue/source/test.obj");
        PlaceModel(griffinAsset, "Guardian Statue NW", architecture, new Vector3(-14.2f, 0f, 14.2f), Quaternion.Euler(0f, 135f, 0f), Vector3.one * 0.043f, stone);
        PlaceModel(griffinAsset, "Guardian Statue NE", architecture, new Vector3(14.2f, 0f, 14.2f), Quaternion.Euler(0f, -135f, 0f), Vector3.one * 0.043f, stone);

        GameObject ruinAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Scenes/Chips/Jungle Runner/Prefabs/Environment/Ruin.prefab");
        PlaceModel(ruinAsset, "Ruined Arch West", architecture, new Vector3(-16.4f, 0f, 6.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.90f, stone);
        PlaceModel(ruinAsset, "Ruined Arch East", architecture, new Vector3(16.4f, 0f, -6.8f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 0.90f, stone);

        Transform gate = new GameObject("Monumental Gate").transform;
        gate.SetParent(architecture, false);
        gate.localPosition = new Vector3(0f, 0f, 17.45f);
        CreatePrimitive(PrimitiveType.Cylinder, "Gate Column Left", gate, new Vector3(-4f, 3.1f, 0f), new Vector3(0.9f, 3.1f, 0.9f), stone, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Gate Column Right", gate, new Vector3(4f, 3.1f, 0f), new Vector3(0.9f, 3.1f, 0.9f), stone, false);
        CreatePrimitive(PrimitiveType.Cube, "Carved Architrave", gate, new Vector3(0f, 6.1f, 0f), new Vector3(9.5f, 1.1f, 1.5f), dark, false);
        CreatePrimitive(PrimitiveType.Cube, "Glowing Gate Sigil", gate, new Vector3(0f, 6.15f, -0.8f), new Vector3(2.2f, 0.65f, 0.08f), gold, false);

        Transform atmosphere = new GameObject("Atmosphere").transform;
        atmosphere.SetParent(arena);
        CreateBrazier(atmosphere, new Vector3(-14.0f, 0f, -14.0f), danger, gold, dark, true);
        CreateBrazier(atmosphere, new Vector3(14.0f, 0f, -14.0f), danger, gold, dark, true);
        CreateBrazier(atmosphere, new Vector3(-14.0f, 0f, 9.0f), danger, gold, dark, false);
        CreateBrazier(atmosphere, new Vector3(14.0f, 0f, 9.0f), danger, gold, dark, false);
        SetBatchingStatic(arena.gameObject);

        Transform respawn = new GameObject("Spawn and Revive Point").transform;
        respawn.SetParent(arena);
        respawn.position = new Vector3(0f, 1.75f, 0f);
        level.respawnPoint = respawn;

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.2f;
        camera.farClipPlane = 80f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.065f, 0.055f);
        camera.allowHDR = false;
        cameraObject.transform.position = new Vector3(0f, 23f, -26f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 2f, 0f) - cameraObject.transform.position, Vector3.up);
        CameraEffects effects = cameraObject.AddComponent<CameraEffects>();
        SerializedObject effectsObject = new SerializedObject(effects);
        effectsObject.FindProperty("_camera").objectReferenceValue = camera;
        effectsObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject lightObject = new GameObject("Temple Sun");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.78f, 0.48f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.None;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        RenderSettings.ambientLight = new Color(0.30f, 0.34f, 0.28f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.11f, 0.15f, 0.13f);
        RenderSettings.fogStartDistance = 36f;
        RenderSettings.fogEndDistance = 82f;

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabes/Player.prefab");
        GameObject playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        playerObject.name = "Player";
        playerObject.transform.localScale *= 0.72f;
        playerObject.transform.SetPositionAndRotation(respawn.position, Quaternion.identity);
        PlayerController player = playerObject.GetComponent<PlayerController>();
        player.m_MoveSpeed = 7.2f;
        player.m_jumpForce = 5.5f;
        player.m_InAirSpeedMultiplier = 1f;
        SerializedObject playerObjectSerialized = new SerializedObject(player);
        playerObjectSerialized.FindProperty("m_cameraTransform").objectReferenceValue = cameraObject.transform;
        playerObjectSerialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (MonoBehaviour behaviour in playerObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is PlayerController || behaviour is PlayerAnimator || behaviour is BaffController) continue;
            behaviour.enabled = false;
        }
        AncientHallPlayerAdapter adapter = playerObject.AddComponent<AncientHallPlayerAdapter>();
        adapter.level = level;
        adapter.arenaHalfSize = 16.5f;
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null) animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        level.player = player;

        AncientHallBowl bowl = levelObject.AddComponent<AncientHallBowl>();
        bowl.bowlCenter = respawn;
        bowl.player = player;
        bowl.curvature = 0.0075f;
        bowl.flatRadius = 1.5f;
        bowl.maximumLift = 3.2f;

        Transform trapRoot = new GameObject("Traps").transform;
        List<AncientTrapCycle> traps = new List<AncientTrapCycle>();
        Vector3[] positions = {
            new Vector3(-8f,0f,-6.5f), new Vector3(8f,0f,6.5f),
            Vector3.zero, new Vector3(-17f,0f,0f),
            Vector3.zero, Vector3.zero, new Vector3(-11.5f,0f,-10f),
            new Vector3(8f,0f,-9f), new Vector3(-7f,0f,9f),
            new Vector3(11.5f,0f,10f), new Vector3(-10f,0f,11.5f),
            new Vector3(10f,0f,-11.5f), new Vector3(17f,0f,0f),
            new Vector3(0f,0f,4.5f)
        };
        int[] kinds = {0,1,2,3,4,5,6,0,1,6,0,1,3,2};
        for (int i = 0; i < kinds.Length; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(trapPrefabs[kinds[i]], scene);
            instance.name = (i + 1).ToString("00") + " - " + instance.name.Replace("(Clone)", "");
            instance.transform.SetParent(trapRoot);
            instance.transform.position = positions[i];
            if (kinds[i] == 3 && positions[i].x > 0f) instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            AncientTrapCycle trap = instance.GetComponent<AncientTrapCycle>();
            trap.initialDelay = 0.4f + (i % 3) * 0.45f;
            traps.Add(trap);
        }
        level.traps = traps.ToArray();
        level.startingTrapCount = 3;
        level.trapRotationInterval = 5f;

        Transform pointRoot = new GameObject("Pickup Spawn Points").transform;
        List<Transform> points = new List<Transform>();
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Transform point = new GameObject("Pickup Point " + (i + 1).ToString("00")).transform;
            point.SetParent(pointRoot);
            float radius = i % 2 == 0 ? 6.5f : 12f;
            point.position = new Vector3(Mathf.Sin(angle) * radius, 1.1f, Mathf.Cos(angle) * radius);
            points.Add(point);
        }
        level.pickupPoints = points.ToArray();
        level.coinPrefab = coinPrefab;
        level.magnetPrefab = magnetPrefab;
        level.doublePrefab = doublePrefab;
        level.shieldPrefab = shieldPrefab;

        CreateHud(level, player, gold, danger, cyan, violet);
        CreateEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = levelObject;
        Debug.Log("Зал древних ловушек создан: " + ScenePath);
    }

    private static GameObject CreateTrapPrefab(AncientTrapCycle.TrapKind kind, Material stone, Material warningMaterial, Material danger)
    {
        string name = "Trap_" + kind;
        string path = Prefabs + "/" + name + ".prefab";
        GameObject root = new GameObject(name);
        AncientTrapCycle cycle = root.AddComponent<AncientTrapCycle>();
        cycle.kind = kind;
        cycle.waitingDuration = 2.7f;
        cycle.warningDuration = 1.25f;
        cycle.attackDuration = 1f;
        cycle.returnDuration = 0.75f;

        GameObject warning = CreatePrimitive(PrimitiveType.Cylinder, "Warning", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.025f, 2.4f), warningMaterial, false);
        cycle.warningVisual = warning;
        GameObject hazard;

        if (kind == AncientTrapCycle.TrapKind.FloorSpikes)
        {
            hazard = CreatePrimitive(PrimitiveType.Cube, "Hazard", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(3.5f, 0.12f, 3.5f), stone, true);
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
            {
                CreateMeshVisual("Stone Spike", hazard.transform, new Vector3(x * 0.22f, 5.8f, z * 0.22f), Vector3.one * 0.22f, Quaternion.identity, GetConeMesh(), stone);
            }
            hazard.transform.localScale = new Vector3(1f, 0.08f, 1f);
            cycle.attackScale = new Vector3(1f, 12.5f, 1f);
        }
        else if (kind == AncientTrapCycle.TrapKind.FallingRock)
        {
            hazard = CreatePrimitive(PrimitiveType.Sphere, "Hazard", root.transform, new Vector3(0f, 16f, 0f), new Vector3(2.3f, 2.3f, 2.3f), stone, true);
            Renderer ballRenderer = hazard.GetComponent<Renderer>();
            if (ballRenderer != null) ballRenderer.enabled = false;
            GameObject rockAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Scenes/HML/3D/DIMA LISEVICH_Garrymod/Prefabs/SM_DL_Stone_1.prefab");
            PlaceModel(rockAsset, "Falling Boulder Visual", hazard.transform, Vector3.zero, Quaternion.Euler(18f, 37f, 9f), Vector3.one * 2.2f, stone);
            cycle.attackOffset = new Vector3(0f, -15f, 0f);
            cycle.attackEuler = new Vector3(360f, 220f, 0f);
        }
        else if (kind == AncientTrapCycle.TrapKind.SweepingBeam)
        {
            warning.transform.localScale = new Vector3(14f, 0.02f, 0.35f);
            hazard = CreatePrimitive(PrimitiveType.Cylinder, "Hazard", root.transform, new Vector3(-16f, 1.05f, 0f), new Vector3(0.45f, 13.5f, 0.45f), danger, true);
            hazard.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cylinder, "Beam Cap", hazard.transform, new Vector3(0f, 0.95f, 0f), new Vector3(1.7f, 0.05f, 1.7f), stone, false);
            cycle.attackOffset = new Vector3(32f, 0f, 0f);
        }
        else if (kind == AncientTrapCycle.TrapKind.WallBlock)
        {
            warning.transform.localPosition = new Vector3(4f, 0.04f, 0f);
            warning.transform.localScale = new Vector3(4f, 0.02f, 4f);
            hazard = CreatePrimitive(PrimitiveType.Cube, "Hazard", root.transform, new Vector3(0f, 1.55f, 0f), new Vector3(3.0f, 3.1f, 4.5f), stone, true);
            cycle.attackOffset = new Vector3(6f, 0f, 0f);
        }
        else if (kind == AncientTrapCycle.TrapKind.RotatingBeam)
        {
            warning.transform.localScale = new Vector3(9f, 0.02f, 0.35f);
            hazard = new GameObject("Hazard Pivot");
            hazard.transform.SetParent(root.transform, false);
            GameObject beam = CreatePrimitive(PrimitiveType.Cylinder, "Damage Beam", hazard.transform, new Vector3(6.5f, 1.05f, 0f), new Vector3(0.38f, 6.5f, 0.38f), danger, true);
            beam.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreatePrimitive(PrimitiveType.Sphere, "Heavy Beam Tip", beam.transform, new Vector3(0f, 1f, 0f), new Vector3(1.7f, 0.11f, 1.7f), stone, false);
            cycle.attackEuler = new Vector3(0f, 540f, 0f);
            cycle.damageCollider = beam.GetComponent<Collider>();
            beam.AddComponent<AncientTrapDamage>();
        }
        else if (kind == AncientTrapCycle.TrapKind.Shockwave)
        {
            warning.transform.localScale = new Vector3(2f, 0.02f, 2f);
            hazard = new GameObject("Hazard");
            hazard.transform.SetParent(root.transform, false);
            hazard.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            hazard.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            List<Collider> ringColliders = new List<Collider>();
            for (int segment = 0; segment < 16; segment++)
            {
                float angle = segment * 360f / 16f;
                float radians = angle * Mathf.Deg2Rad;
                GameObject part = CreatePrimitive(PrimitiveType.Cube, "Fire Ring Segment", hazard.transform,
                    new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)), new Vector3(0.42f, 0.35f, 0.16f), danger, true);
                part.transform.localRotation = Quaternion.Euler(0f, -angle, 0f);
                BoxCollider partCollider = part.GetComponent<BoxCollider>();
                partCollider.isTrigger = true; partCollider.enabled = false; part.AddComponent<AncientTrapDamage>(); ringColliders.Add(partCollider);
            }
            cycle.additionalDamageColliders = ringColliders.ToArray();
            cycle.attackScale = new Vector3(18f, 1f, 18f);
        }
        else
        {
            warning.transform.localScale = new Vector3(2.5f, 0.02f, 5f);
            hazard = CreatePrimitive(PrimitiveType.Cylinder, "Hazard", root.transform, new Vector3(0f, 3.4f, 0f), new Vector3(1.1f, 3.4f, 1.1f), stone, true);
            CreatePrimitive(PrimitiveType.Cylinder, "Column Capital", hazard.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.45f, 0.08f, 1.45f), stone, false);
            CreatePrimitive(PrimitiveType.Cylinder, "Column Base", hazard.transform, new Vector3(0f, -0.52f, 0f), new Vector3(1.35f, 0.08f, 1.35f), stone, false);
            cycle.attackEuler = new Vector3(0f, 0f, 82f);
        }

        cycle.hazardVisual = hazard.transform;
        if (cycle.damageCollider == null) cycle.damageCollider = hazard.GetComponent<Collider>();
        if (cycle.damageCollider != null)
        {
            cycle.damageCollider.isTrigger = true;
            if (cycle.damageCollider.GetComponent<AncientTrapDamage>() == null) cycle.damageCollider.gameObject.AddComponent<AncientTrapDamage>();
            cycle.damageCollider.enabled = false;
        }
        warning.SetActive(false);
        SetRendererPerformance(root);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static AncientHallPickup CreatePickupPrefab(string name, AncientHallPickup.PickupKind kind, Material material, PrimitiveType primitive)
    {
        string path = Prefabs + "/Pickup_" + name + ".prefab";
        GameObject root = new GameObject("Pickup_" + name);
        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.75f;
        AncientHallPickup pickup = root.AddComponent<AncientHallPickup>();
        pickup.kind = kind;

        if (kind == AncientHallPickup.PickupKind.Coin)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Scenes/Chips/Jungle Runner/Prefabs/Gameplay/Coin.prefab");
            if (existing != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(existing);
                visual.name = "Coin Visual";
                visual.transform.SetParent(root.transform, false);
                foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(childCollider);
                foreach (MonoBehaviour behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            }
            else CreatePrimitive(primitive, "Visual", root.transform, Vector3.zero, new Vector3(0.8f, 0.12f, 0.8f), material, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            GameObject visual = CreatePrimitive(primitive, "Visual", root.transform, Vector3.zero, Vector3.one * 0.75f, material, false);
            if (kind == AncientHallPickup.PickupKind.Magnet)
            {
                visual.transform.localScale = new Vector3(0.25f, 0.75f, 0.25f);
                CreatePrimitive(PrimitiveType.Capsule, "Magnet Right", root.transform, new Vector3(0.55f, 0f, 0f), new Vector3(0.25f, 0.75f, 0.25f), material, false);
            }
        }
        SetRendererPerformance(root);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<AncientHallPickup>();
    }

    private static void CreateHud(AncientTrapHallLevel level, PlayerController player, Material gold, Material danger, Material cyan, Material violet)
    {
        GameObject canvasObject = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        AncientTrapHallHud hud = canvasObject.AddComponent<AncientTrapHallHud>();
        hud.level = level;

        hud.healthText = CreateText(canvas.transform, "Health Text", "♥♥♥", new Vector2(180f, -55f), new Vector2(300f, 80f), 42, TextAlignmentOptions.Left, new Color(1f, 0.2f, 0.16f));
        hud.timerText = CreateText(canvas.transform, "Timer Text", "0 СЕК", new Vector2(0f, -55f), new Vector2(320f, 80f), 40, TextAlignmentOptions.Center, Color.white);
        hud.coinsText = CreateText(canvas.transform, "Coins Text", "МОНЕТЫ 0", new Vector2(-180f, -55f), new Vector2(360f, 80f), 38, TextAlignmentOptions.Right, new Color(1f, 0.73f, 0.18f));
        Anchor(hud.healthText.rectTransform, new Vector2(0f, 1f));
        Anchor(hud.timerText.rectTransform, new Vector2(0.5f, 1f));
        Anchor(hud.coinsText.rectTransform, new Vector2(1f, 1f));

        Button pause = CreateButton(canvas.transform, "Pause Button", "Ⅱ", new Vector2(-65f, -135f), new Vector2(90f, 90f), new Color(0.12f, 0.15f, 0.13f, 0.9f));
        Anchor(pause.GetComponent<RectTransform>(), new Vector2(1f, 1f));
        UnityEventTools.AddPersistentListener(pause.onClick, hud.Pause);

        Button magnet = CreateButton(canvas.transform, "Magnet Button", "МАГНИТ", new Vector2(-330f, 82f), new Vector2(180f, 105f), new Color(0.04f, 0.45f, 0.56f, 0.9f));
        Button twice = CreateButton(canvas.transform, "Double Coins Button", "×2", new Vector2(-130f, 82f), new Vector2(180f, 105f), new Color(0.58f, 0.37f, 0.05f, 0.9f));
        Button shield = CreateButton(canvas.transform, "Shield Button", "ЩИТ", new Vector2(70f, 82f), new Vector2(180f, 105f), new Color(0.34f, 0.10f, 0.55f, 0.9f));
        Anchor(magnet.GetComponent<RectTransform>(), new Vector2(1f, 0f)); Anchor(twice.GetComponent<RectTransform>(), new Vector2(1f, 0f)); Anchor(shield.GetComponent<RectTransform>(), new Vector2(1f, 0f));
        hud.magnetText = magnet.GetComponentInChildren<TMP_Text>(); hud.doubleText = twice.GetComponentInChildren<TMP_Text>(); hud.shieldText = shield.GetComponentInChildren<TMP_Text>();
        UnityEventTools.AddPersistentListener(magnet.onClick, hud.Magnet); UnityEventTools.AddPersistentListener(twice.onClick, hud.DoubleCoins); UnityEventTools.AddPersistentListener(shield.onClick, hud.Shield);

        CreateInputButton(canvas.transform, "Move Left Button", "◀", new Vector2(110f, 105f), AncientHallInputButton.InputKind.Left, player);
        CreateInputButton(canvas.transform, "Move Right Button", "▶", new Vector2(310f, 105f), AncientHallInputButton.InputKind.Right, player);
        CreateInputButton(canvas.transform, "Jump Button", "ПРЫЖОК", new Vector2(-150f, 215f), AncientHallInputButton.InputKind.Jump, player, new Vector2(230f, 150f));

        hud.countdownPanel = CreatePanel(canvas.transform, "Countdown Panel", new Color(0f, 0f, 0f, 0.35f));
        hud.countdownText = CreateText(hud.countdownPanel.transform, "Countdown Text", "3", Vector2.zero, new Vector2(400f, 400f), 150, TextAlignmentOptions.Center, Color.white);
        hud.pausePanel = CreatePanel(canvas.transform, "Pause Panel", new Color(0f, 0f, 0f, 0.7f));
        CreateText(hud.pausePanel.transform, "Pause Text", "ПАУЗА\nНажмите кнопку паузы для продолжения", Vector2.zero, new Vector2(900f, 250f), 45, TextAlignmentOptions.Center, Color.white);
        hud.revivePanel = CreatePanel(canvas.transform, "Revive Panel", new Color(0f, 0f, 0f, 0.82f));
        hud.reviveText = CreateText(hud.revivePanel.transform, "Revive Text", "ВОСКРЕШЕНИЕ", new Vector2(0f, 130f), new Vector2(900f, 220f), 48, TextAlignmentOptions.Center, Color.white);
        Button ad = CreateButton(hud.revivePanel.transform, "Ad Revive Button", "РЕКЛАМА", new Vector2(-180f, -80f), new Vector2(300f, 100f), new Color(0.10f, 0.45f, 0.30f, 1f));
        Button paid = CreateButton(hud.revivePanel.transform, "Paid Revive Button", "ЗА МОНЕТЫ", new Vector2(180f, -80f), new Vector2(300f, 100f), new Color(0.55f, 0.34f, 0.06f, 1f));
        Button finish = CreateButton(hud.revivePanel.transform, "Finish Button", "ЗАВЕРШИТЬ", new Vector2(0f, -210f), new Vector2(300f, 90f), new Color(0.45f, 0.10f, 0.08f, 1f));
        UnityEventTools.AddPersistentListener(ad.onClick, hud.AdRevive); UnityEventTools.AddPersistentListener(paid.onClick, hud.PaidRevive); UnityEventTools.AddPersistentListener(finish.onClick, hud.Finish);

        hud.resultsPanel = CreatePanel(canvas.transform, "Results Panel", new Color(0f, 0f, 0f, 0.85f));
        hud.resultsText = CreateText(hud.resultsPanel.transform, "Results Text", "РЕЗУЛЬТАТ", new Vector2(0f, 120f), new Vector2(900f, 220f), 52, TextAlignmentOptions.Center, Color.white);
        Button restart = CreateButton(hud.resultsPanel.transform, "Restart Button", "ЕЩЁ РАЗ", new Vector2(-180f, -100f), new Vector2(300f, 100f), new Color(0.10f, 0.42f, 0.26f, 1f));
        Button exit = CreateButton(hud.resultsPanel.transform, "Exit Button", "В ЛАГЕРЬ", new Vector2(180f, -100f), new Vector2(300f, 100f), new Color(0.40f, 0.24f, 0.08f, 1f));
        UnityEventTools.AddPersistentListener(restart.onClick, hud.Restart); UnityEventTools.AddPersistentListener(exit.onClick, hud.Exit);
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = fontSize; text.alignment = alignment; text.color = color; text.raycastTarget = false;
        RectTransform rect = text.rectTransform; rect.sizeDelta = size; rect.anchoredPosition = position;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(AncientHallUiButton));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = size; rect.anchoredPosition = position;
        obj.GetComponent<Image>().color = color;
        Button button = obj.GetComponent<Button>(); button.navigation = new Navigation { mode = Navigation.Mode.None };
        TMP_Text text = CreateText(obj.transform, "Label", label, Vector2.zero, size, 28, TextAlignmentOptions.Center, Color.white);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static void CreateInputButton(Transform parent, string name, string label, Vector2 position, AncientHallInputButton.InputKind kind, PlayerController player, Vector2? customSize = null)
    {
        Vector2 size = customSize ?? new Vector2(170f, 150f);
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(AncientHallInputButton));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(kind == AncientHallInputButton.InputKind.Jump ? 1f : 0f, 0f); rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        obj.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.13f, 0.78f);
        AncientHallInputButton input = obj.GetComponent<AncientHallInputButton>(); input.kind = kind; input.player = player;
        CreateText(obj.transform, "Label", label, Vector2.zero, size, 31, TextAlignmentOptions.Center, Color.white).fontStyle = FontStyles.Bold;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor) { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; }
    private static void CreateEventSystem() { new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); }

    private static GameObject PlaceModel(GameObject asset, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material overrideMaterial)
    {
        if (asset == null) return null;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        if (instance == null) instance = Object.Instantiate(asset);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = scale;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (overrideMaterial != null) renderer.sharedMaterial = overrideMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        return instance;
    }

    private static void CreateBrazier(Transform parent, Vector3 position, Material fire, Material innerFire, Material metal, bool addLight)
    {
        Transform root = new GameObject("Stone Brazier").transform;
        root.SetParent(parent, false);
        root.localPosition = position;
        CreatePrimitive(PrimitiveType.Cylinder, "Base", root, new Vector3(0f, 0.45f, 0f), new Vector3(0.65f, 0.45f, 0.65f), metal, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Bowl", root, new Vector3(0f, 1.0f, 0f), new Vector3(1.05f, 0.18f, 1.05f), metal, false);
        CreateMeshVisual("Outer Flame", root, new Vector3(0f, 1.22f, 0f), new Vector3(0.32f, 0.42f, 0.32f), Quaternion.identity, GetConeMesh(), fire);
        CreateMeshVisual("Inner Flame", root, new Vector3(0.05f, 1.25f, -0.03f), new Vector3(0.18f, 0.27f, 0.18f), Quaternion.identity, GetConeMesh(), innerFire);
        CreateMeshVisual("Side Flame", root, new Vector3(0.28f, 1.18f, 0.06f), new Vector3(0.15f, 0.24f, 0.15f), Quaternion.Euler(0f, 0f, -13f), GetConeMesh(), fire);
        if (!addLight) return;
        GameObject lightObject = new GameObject("Brazier Light");
        lightObject.transform.SetParent(root, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.35f, 0.08f);
        light.intensity = 1.6f;
        light.range = 9f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
    }

    private static GameObject CreateMeshVisual(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Mesh mesh, Material material)
    {
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.transform.localRotation = rotation;
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }

    private static Mesh GetConeMesh()
    {
        string path = Meshes + "/ATH_LowPolySpike.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        const int sides = 8;
        Vector3[] vertices = new Vector3[sides + 2];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
        vertices[sides + 1] = new Vector3(0f, 4f, 0f);
        int[] triangles = new int[sides * 6];
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int offset = i * 6;
            triangles[offset] = 0; triangles[offset + 1] = next + 1; triangles[offset + 2] = i + 1;
            triangles[offset + 3] = i + 1; triangles[offset + 4] = next + 1; triangles[offset + 5] = sides + 1;
        }
        Mesh mesh = new Mesh { name = "ATH Low Poly Spike" };
        mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = obj.GetComponent<Collider>(); if (!keepCollider && collider != null) Object.DestroyImmediate(collider);
        return obj;
    }

    private static GameObject CreateArenaFloor(Transform parent, Material material)
    {
        GameObject floor = new GameObject("Temple Floor", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
        floor.transform.SetParent(parent, false);
        floor.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        floor.GetComponent<MeshFilter>().sharedMesh = GetArenaFloorMesh();
        floor.GetComponent<MeshRenderer>().sharedMaterial = material;
        BoxCollider collider = floor.GetComponent<BoxCollider>();
        collider.center = new Vector3(0f, -0.33f, 0f);
        collider.size = new Vector3(36f, 0.7f, 36f);
        return floor;
    }

    private static GameObject CreateCurvedBar(string name, Transform parent, Vector3 position, Vector3 size, bool rotateToZ, Material material, bool keepCollider)
    {
        GameObject bar = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = position;
        bar.transform.localRotation = rotateToZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        bar.transform.localScale = size;
        bar.GetComponent<MeshFilter>().sharedMesh = GetSegmentedBarMesh();
        bar.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (keepCollider) bar.AddComponent<BoxCollider>();
        return bar;
    }

    private static Mesh GetSegmentedBarMesh()
    {
        string path = Meshes + "/ATH_SegmentedBar.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        const int segments = 24;
        List<Vector3> vertices = new List<Vector3>(segments * 24);
        List<Vector2> uv = new List<Vector2>(segments * 24);
        List<int> triangles = new List<int>(segments * 36);
        for (int i = 0; i < segments; i++)
        {
            float x0 = -0.5f + i / (float)segments;
            float x1 = -0.5f + (i + 1) / (float)segments;
            float u0 = i / (float)segments;
            float u1 = (i + 1) / (float)segments;
            AddBarStripQuad(vertices, uv, triangles, new Vector3(x0,-0.5f,-0.5f), new Vector3(x1,-0.5f,-0.5f), new Vector3(x1,0.5f,-0.5f), new Vector3(x0,0.5f,-0.5f), u0, u1);
            AddBarStripQuad(vertices, uv, triangles, new Vector3(x1,-0.5f,0.5f), new Vector3(x0,-0.5f,0.5f), new Vector3(x0,0.5f,0.5f), new Vector3(x1,0.5f,0.5f), u0, u1);
            AddBarStripQuad(vertices, uv, triangles, new Vector3(x0,0.5f,-0.5f), new Vector3(x1,0.5f,-0.5f), new Vector3(x1,0.5f,0.5f), new Vector3(x0,0.5f,0.5f), u0, u1);
            AddBarStripQuad(vertices, uv, triangles, new Vector3(x0,-0.5f,0.5f), new Vector3(x1,-0.5f,0.5f), new Vector3(x1,-0.5f,-0.5f), new Vector3(x0,-0.5f,-0.5f), u0, u1);
        }
        AddBarQuad(vertices, uv, triangles, new Vector3(-0.5f,-0.5f,0.5f), new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,0.5f,-0.5f), new Vector3(-0.5f,0.5f,0.5f));
        AddBarQuad(vertices, uv, triangles, new Vector3(0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,0.5f), new Vector3(0.5f,0.5f,0.5f), new Vector3(0.5f,0.5f,-0.5f));
        Mesh mesh = existing != null ? existing : new Mesh { name = "ATH Segmented Bar" };
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static void AddBarStripQuad(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u0, float u1)
    {
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        uv.Add(new Vector2(u0, 0f)); uv.Add(new Vector2(u1, 0f)); uv.Add(new Vector2(u1, 1f)); uv.Add(new Vector2(u0, 1f));
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
    }

    private static void AddBarQuad(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
    }

    private static Mesh GetArenaFloorMesh()
    {
        string path = Meshes + "/ATH_ArenaFloor_Grid.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;

        const int segments = 24;
        const float size = 36f;
        Vector3[] vertices = new Vector3[(segments + 1) * (segments + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * segments * 6];
        for (int z = 0; z <= segments; z++)
        for (int x = 0; x <= segments; x++)
        {
            int index = z * (segments + 1) + x;
            float tx = x / (float)segments;
            float tz = z / (float)segments;
            vertices[index] = new Vector3((tx - 0.5f) * size, 0f, (tz - 0.5f) * size);
            uv[index] = new Vector2(tx, tz);
        }
        int triangle = 0;
        for (int z = 0; z < segments; z++)
        for (int x = 0; x < segments; x++)
        {
            int current = z * (segments + 1) + x;
            int nextRow = current + segments + 1;
            triangles[triangle++] = current;
            triangles[triangle++] = nextRow;
            triangles[triangle++] = current + 1;
            triangles[triangle++] = current + 1;
            triangles[triangle++] = nextRow;
            triangles[triangle++] = nextRow + 1;
        }
        Mesh mesh = new Mesh { name = "ATH Arena Floor Grid" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.bounds = new Bounds(Vector3.up * 1.5f, new Vector3(size, 7f, size));
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Material MaterialAsset(string name, Color color, bool emission = false, Texture2D texture = null, Vector2? tiling = null)
    {
        string path = Materials + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Ancient Trap Hall/Mobile Bowl Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader); AssetDatabase.CreateAsset(material, path);
        }
        Shader bowlShader = Shader.Find("Ancient Trap Hall/Mobile Bowl Lit");
        if (bowlShader != null && material.shader != bowlShader) material.shader = bowlShader;
        material.color = color; material.enableInstancing = true;
        if (texture != null)
        {
            Vector2 scale = tiling ?? Vector2.one;
            if (material.HasProperty("_BaseMap")) { material.SetTexture("_BaseMap", texture); material.SetTextureScale("_BaseMap", scale); }
            if (material.HasProperty("_MainTex")) { material.SetTexture("_MainTex", texture); material.SetTextureScale("_MainTex", scale); }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
        }
        if (emission && material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.3f); }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.Compressed;
        TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true; android.maxTextureSize = 512; android.format = TextureImporterFormat.ASTC_6x6; android.compressionQuality = 50;
        importer.SetPlatformTextureSettings(android);
        TextureImporterPlatformSettings webgl = importer.GetPlatformTextureSettings("WebGL");
        webgl.overridden = true; webgl.maxTextureSize = 512; webgl.format = TextureImporterFormat.ETC2_RGB4; webgl.compressionQuality = 50;
        importer.SetPlatformTextureSettings(webgl);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void SetRendererPerformance(GameObject root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            foreach (Material material in renderer.sharedMaterials) if (material != null) material.enableInstancing = true;
        }
    }

    private static void SetBatchingStatic(GameObject root)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(item.gameObject, StaticEditorFlags.BatchingStatic);
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private static void AddSceneToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(item => item.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
