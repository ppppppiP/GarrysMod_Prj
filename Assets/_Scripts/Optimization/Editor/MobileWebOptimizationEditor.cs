using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MobileWebOptimizationEditor
{
    private static readonly HashSet<string> LightweightPrefabNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Coin1.prefab",
        "Meteorit.prefab",
        "Spear_move.prefab",
        "Molot.prefab",
        "move_dubina.prefab",
        "Plank.prefab",
        "MOVE.prefab",
        "Fire_Gun.prefab",
        "Trampoline.prefab",
        "Water_Gun.prefab",
        "Move Up Plat.prefab",
        "DOOR.prefab"
    };

    [MenuItem("Tools/Optimization/Apply Mobile and WebGL Optimizations")]
    public static void ApplyAll()
    {
        try
        {
            ConfigurePlayerSettings();
            OptimizePrefabs();
            OptimizeTextures();
            Debug.Log("[MobileWebOptimization] Оптимизация завершена успешно.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
        finally
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "Artem";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.artem.garrysmod");
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        QualitySettings.SetQualityLevel(0, true);
    }

    private static void OptimizePrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int culledAnimators = 0;
        int migratedAnimators = 0;
        int optimizedCanvases = 0;
        int optimizedMeshColliders = 0;
        int boxConversions = 0;

        for (int prefabIndex = 0; prefabIndex < prefabGuids.Length; prefabIndex++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[prefabIndex]);
            if (IsThirdPartyOrExamplePrefab(path))
                continue;

            GameObject root = null;
            bool changed = false;

            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                if (HasMissingScriptsInHierarchy(root))
                {
                    Debug.LogWarning($"[MobileWebOptimization] Префаб с отсутствующим скриптом пропущен: {path}");
                    continue;
                }

                bool migrateToLightweight = LightweightPrefabNames.Contains(Path.GetFileName(path));

                Animator[] animators = root.GetComponentsInChildren<Animator>(true);
                foreach (Animator animator in animators)
                {
                    if (animator == null)
                        continue;

                    AnimatorCullingMode cullingMode = GetSafeCullingMode(animator);
                    if (animator.cullingMode != cullingMode)
                    {
                        animator.cullingMode = cullingMode;
                        culledAnimators++;
                        changed = true;
                    }

                    if (migrateToLightweight && TryMigrateAnimator(animator))
                    {
                        migratedAnimators++;
                        changed = true;
                    }
                }

                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.pixelPerfect || canvas.additionalShaderChannels != AdditionalCanvasShaderChannels.None)
                    {
                        canvas.pixelPerfect = false;
                        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
                        optimizedCanvases++;
                        changed = true;
                    }
                }

                foreach (GraphicRaycaster raycaster in root.GetComponentsInChildren<GraphicRaycaster>(true))
                {
                    if (raycaster.enabled && !HasInteractiveHandlers(raycaster.transform))
                    {
                        raycaster.enabled = false;
                        changed = true;
                    }
                }

                MeshCollider[] meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
                foreach (MeshCollider meshCollider in meshColliders)
                {
                    if (meshCollider == null)
                        continue;

                    if (TryReplaceBoxLikeMeshCollider(meshCollider))
                    {
                        boxConversions++;
                        changed = true;
                        continue;
                    }

                    MeshColliderCookingOptions optimizedOptions =
                        MeshColliderCookingOptions.CookForFasterSimulation |
                        MeshColliderCookingOptions.EnableMeshCleaning |
                        MeshColliderCookingOptions.WeldColocatedVertices |
                        MeshColliderCookingOptions.UseFastMidphase;

                    if (meshCollider.cookingOptions != optimizedOptions)
                    {
                        meshCollider.cookingOptions = optimizedOptions;
                        optimizedMeshColliders++;
                        changed = true;
                    }
                }

                foreach (AutoAddressableProcessData processData in root.GetComponentsInChildren<AutoAddressableProcessData>(true))
                {
                    if (processData.processAnimator && processData.GetComponent<Animator>() == null)
                    {
                        processData.processAnimator = false;
                        changed = true;
                    }
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MobileWebOptimization] Префаб пропущен: {path}\n{exception.Message}");
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Debug.Log($"[MobileWebOptimization] Prefabs: culling Animator={culledAnimators}, lightweight={migratedAnimators}, Canvas={optimizedCanvases}, MeshCollider={optimizedMeshColliders}, MeshCollider->BoxCollider={boxConversions}.");
    }

    private static bool IsThirdPartyOrExamplePrefab(string path)
    {
        return path.StartsWith("Assets/Plugins/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("Assets/YandexGame/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("Assets/TextMesh Pro/", StringComparison.OrdinalIgnoreCase) ||
               path.IndexOf("/Examples/", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("/Samples/", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("/IntegrationTests/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryMigrateAnimator(Animator animator)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null)
            return false;

        AnimationClip[] clips = controller.animationClips
            .Where(clip => clip != null)
            .Distinct()
            .ToArray();

        if (clips.Length != 1)
            return false;

        AnimationClip clip = clips[0];
        if (clip.events != null && clip.events.Length > 0)
            return false;

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        if (bindings.Length == 0 || AnimationUtility.GetObjectReferenceCurveBindings(clip).Length != 0)
            return false;

        Dictionary<string, LightweightTransformAnimation.Track> tracks = new Dictionary<string, LightweightTransformAnimation.Track>();
        foreach (EditorCurveBinding binding in bindings)
        {
            if (!typeof(Transform).IsAssignableFrom(binding.type))
                return false;

            Transform target = string.IsNullOrEmpty(binding.path) ? animator.transform : animator.transform.Find(binding.path);
            if (target == null || !IsSupportedProperty(binding.propertyName))
                return false;

            if (!tracks.TryGetValue(binding.path, out LightweightTransformAnimation.Track track))
            {
                track = new LightweightTransformAnimation.Track
                {
                    target = target,
                    basePosition = target.localPosition,
                    baseEulerAngles = target.localEulerAngles,
                    baseScale = target.localScale
                };
                tracks.Add(binding.path, track);
            }

            AssignCurve(track, binding.propertyName, AnimationUtility.GetEditorCurve(clip, binding));
        }

        if (tracks.Count == 0)
            return false;

        LightweightTransformAnimation lightweight = animator.GetComponent<LightweightTransformAnimation>();
        if (lightweight == null)
            lightweight = animator.gameObject.AddComponent<LightweightTransformAnimation>();

        lightweight.Duration = clip.length;
        lightweight.Loop = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
        lightweight.PlayOnEnable = true;
        lightweight.Tracks.Clear();
        lightweight.Tracks.AddRange(tracks.Values);
        EditorUtility.SetDirty(lightweight);

        UnityEngine.Object.DestroyImmediate(animator, true);
        return true;
    }

    private static bool IsSupportedProperty(string property)
    {
        return property.StartsWith("m_LocalPosition.", StringComparison.Ordinal) ||
               property.StartsWith("m_LocalScale.", StringComparison.Ordinal) ||
               property.StartsWith("m_LocalRotation.", StringComparison.Ordinal) ||
               property.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal) ||
               property.StartsWith("m_LocalEulerAngles.", StringComparison.Ordinal) ||
               property.StartsWith("m_LocalEulerAnglesHint.", StringComparison.Ordinal);
    }

    private static void AssignCurve(LightweightTransformAnimation.Track track, string property, AnimationCurve curve)
    {
        char axis = property[property.Length - 1];
        if (property.StartsWith("m_LocalPosition.", StringComparison.Ordinal))
        {
            if (axis == 'x') track.positionX = curve;
            else if (axis == 'y') track.positionY = curve;
            else if (axis == 'z') track.positionZ = curve;
            return;
        }

        if (property.StartsWith("m_LocalScale.", StringComparison.Ordinal))
        {
            if (axis == 'x') track.scaleX = curve;
            else if (axis == 'y') track.scaleY = curve;
            else if (axis == 'z') track.scaleZ = curve;
            return;
        }

        if (property.StartsWith("m_LocalRotation.", StringComparison.Ordinal))
        {
            if (axis == 'x') track.rotationX = curve;
            else if (axis == 'y') track.rotationY = curve;
            else if (axis == 'z') track.rotationZ = curve;
            else if (axis == 'w') track.rotationW = curve;
            return;
        }

        bool rawEuler = property.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal);
        if (axis == 'x' && (rawEuler || track.rotationX == null)) track.rotationX = curve;
        else if (axis == 'y' && (rawEuler || track.rotationY == null)) track.rotationY = curve;
        else if (axis == 'z' && (rawEuler || track.rotationZ == null)) track.rotationZ = curve;
    }

    private static bool TryReplaceBoxLikeMeshCollider(MeshCollider meshCollider)
    {
        Mesh mesh = meshCollider.sharedMesh;
        if (mesh == null || mesh.vertexCount > 64)
            return false;

        Vector3[] vertices;
        try
        {
            vertices = mesh.vertices;
        }
        catch
        {
            return false;
        }

        if (vertices.Length < 8)
            return false;

        Bounds bounds = mesh.bounds;
        Vector3 minimum = bounds.min;
        Vector3 maximum = bounds.max;
        float tolerance = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.001f + 0.0001f;

        foreach (Vector3 vertex in vertices)
        {
            bool xEdge = Mathf.Abs(vertex.x - minimum.x) <= tolerance || Mathf.Abs(vertex.x - maximum.x) <= tolerance;
            bool yEdge = Mathf.Abs(vertex.y - minimum.y) <= tolerance || Mathf.Abs(vertex.y - maximum.y) <= tolerance;
            bool zEdge = Mathf.Abs(vertex.z - minimum.z) <= tolerance || Mathf.Abs(vertex.z - maximum.z) <= tolerance;
            if (!xEdge || !yEdge || !zEdge)
                return false;
        }

        GameObject target = meshCollider.gameObject;
        bool enabled = meshCollider.enabled;
        bool isTrigger = meshCollider.isTrigger;
        PhysicsMaterial material = meshCollider.sharedMaterial;

        UnityEngine.Object.DestroyImmediate(meshCollider, true);
        BoxCollider boxCollider = target.AddComponent<BoxCollider>();
        boxCollider.center = bounds.center;
        boxCollider.size = bounds.size;
        boxCollider.enabled = enabled;
        boxCollider.isTrigger = isTrigger;
        boxCollider.sharedMaterial = material;
        return true;
    }

    private static bool HasInteractiveHandlers(Transform root)
    {
        if (root.GetComponentInChildren<Selectable>(true) != null || root.GetComponentInChildren<EventTrigger>(true) != null)
            return true;

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();
            if (typeof(IPointerClickHandler).IsAssignableFrom(type) ||
                typeof(IPointerDownHandler).IsAssignableFrom(type) ||
                typeof(IPointerUpHandler).IsAssignableFrom(type) ||
                typeof(IBeginDragHandler).IsAssignableFrom(type) ||
                typeof(IDragHandler).IsAssignableFrom(type) ||
                typeof(IEndDragHandler).IsAssignableFrom(type))
                return true;
        }

        return false;
    }

    private static bool HasMissingScriptsInHierarchy(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                return true;
        }

        return false;
    }

    private static AnimatorCullingMode GetSafeCullingMode(Animator animator)
    {
        if (animator.GetComponentInParent<PlayerController>() != null ||
            animator.GetComponentInChildren<Renderer>(true) == null ||
            animator.GetComponentInChildren<ParticleSystem>(true) != null)
            return AnimatorCullingMode.AlwaysAnimate;

        return AnimatorCullingMode.CullUpdateTransforms;
    }

    private static void OptimizeTextures()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int changed = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                int cap = GetTextureSizeCap(importer);
                bool useMipmaps = importer.textureType != TextureImporterType.Sprite;

                importer.maxTextureSize = Mathf.Min(importer.maxTextureSize, cap);
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.crunchedCompression = false;
                importer.mipmapEnabled = useMipmaps;
                importer.streamingMipmaps = useMipmaps && importer.textureShape != TextureImporterShape.TextureCube;

                SetPlatform(importer, "Android", cap, TextureImporterFormat.ASTC_6x6);
                SetPlatform(importer, "iPhone", cap, TextureImporterFormat.ASTC_6x6);
                SetPlatform(importer, "WebGL", cap, null);

                if (AssetDatabase.WriteImportSettingsIfDirty(path))
                    changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[MobileWebOptimization] Texture import settings updated: {changed}.");
    }

    private static int GetTextureSizeCap(TextureImporter importer)
    {
        if (importer.textureShape == TextureImporterShape.TextureCube || importer.textureType == TextureImporterType.Sprite)
            return 2048;

        if (importer.textureType == TextureImporterType.NormalMap)
            return 1024;

        return 1024;
    }

    private static void SetPlatform(TextureImporter importer, string platform, int cap, TextureImporterFormat? format)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
        settings.name = platform;
        settings.overridden = true;
        settings.maxTextureSize = cap;
        if (format.HasValue)
            settings.format = format.Value;
        settings.compressionQuality = 50;
        settings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(settings);
    }
}
