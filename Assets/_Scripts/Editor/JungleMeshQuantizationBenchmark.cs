#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JungleMeshQuantizationBenchmark
{
    private const string Root = "Assets/_Scenes/Chips/Jungle Runner/Prefabs/Environment/Quantized";
    private const string SourceMeshPath = "Assets/_Scenes/Chips/Jungle Runner/Prefabs/Environment/Tree_Combined.asset";
    private const string SourcePrefabPath = "Assets/_Scenes/Chips/Jungle Runner/Prefabs/Environment/Tree_Combined.prefab";
    public const string ReadWritePrefabPath = Root + "/Tree_Combined_MQ_RW.prefab";
    public const string NoReadWritePrefabPath = Root + "/Tree_Combined_MQ_NoRW.prefab";

    [MenuItem("Tools/Chips/Mesh Quantization/Prepare Benchmark Variants")]
    public static void PrepareVariants()
    {
        EnsureFolders();
        CreateVariant("Tree_Combined_MQ_RW", false);
        CreateVariant("Tree_Combined_MQ_NoRW", true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Mesh Quantization: benchmark variants prepared without modifying source assets.");
    }

    [MenuItem("Tools/Chips/Mesh Quantization/Use Original")]
    public static void UseOriginal() => ApplyTreePrefab(SourcePrefabPath);

    [MenuItem("Tools/Chips/Mesh Quantization/Use Quantized + Read Write")]
    public static void UseReadWrite() => ApplyTreePrefab(ReadWritePrefabPath);

    [MenuItem("Tools/Chips/Mesh Quantization/Use Quantized + No Read Write")]
    public static void UseNoReadWrite() => ApplyTreePrefab(NoReadWritePrefabPath);

    private static void CreateVariant(string assetName, bool disableReadWrite)
    {
        string meshPath = Root + "/" + assetName + ".asset";
        string materialPath = Root + "/" + assetName + ".mat";
        string prefabPath = Root + "/" + assetName + ".prefab";

        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.DeleteAsset(materialPath);
        AssetDatabase.DeleteAsset(prefabPath);
        if (!AssetDatabase.CopyAsset(SourceMeshPath, meshPath))
            throw new InvalidOperationException("Cannot copy source mesh to " + meshPath);
        AssetDatabase.ImportAsset(meshPath, ImportAssetOptions.ForceSynchronousImport);

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        SetMeshReadable(mesh, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(meshPath, ImportAssetOptions.ForceSynchronousImport);
        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null || !mesh.isReadable)
            throw new InvalidOperationException("Copied mesh could not be made readable: " + meshPath);

        InvokeBarkarQuantizer(mesh, disableReadWrite, meshPath);
        EditorUtility.SetDirty(mesh);

        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        Renderer sourceRenderer = sourcePrefab.GetComponentInChildren<Renderer>(true);
        Material sourceMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
        Shader curvedShader = Shader.Find("Jungle Runner/Mobile Curved Lit");
        if (sourceMaterial == null || curvedShader == null)
            throw new InvalidOperationException("Source material or curved shader was not found.");

        Material material = new Material(sourceMaterial)
        {
            name = assetName,
            shader = curvedShader,
            enableInstancing = true
        };
        material.SetFloat("_MQQuantized", 1f);
        material.EnableKeyword("_MQ_QUANTIZED");
        AssetDatabase.CreateAsset(material, materialPath);

        GameObject contents = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        try
        {
            MeshFilter filter = contents.GetComponentInChildren<MeshFilter>(true);
            Renderer renderer = contents.GetComponentInChildren<Renderer>(true);
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            contents.name = assetName;
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void InvokeBarkarQuantizer(Mesh mesh, bool disableReadWrite, string assetPath)
    {
        Type utilityType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("MeshQuantization.MeshQuantizationUtility", false))
            .FirstOrDefault(type => type != null);
        Type settingsType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("MeshQuantization.MeshQuantizationSettings", false))
            .FirstOrDefault(type => type != null);
        if (utilityType == null || settingsType == null)
            throw new InvalidOperationException("Barkar Mesh Quantization package is not installed or feature/mesh-quantize is unavailable.");

        object settings = Activator.CreateInstance(settingsType);
        settingsType.GetField("overwriteVertexColors").SetValue(settings, true);
        settingsType.GetField("generateMissingNormals").SetValue(settings, false);
        settingsType.GetField("generateMissingTangents").SetValue(settings, false);
        settingsType.GetField("disableReadWrite").SetValue(settings, disableReadWrite);
        MethodInfo method = utilityType.GetMethod("TryQuantize", BindingFlags.Public | BindingFlags.Static);
        bool success = method != null && (bool)method.Invoke(null, new[] { mesh, settings, assetPath });
        if (!success) throw new InvalidOperationException("Barkar quantizer rejected " + assetPath);
    }

    private static void SetMeshReadable(Mesh mesh, bool value)
    {
        SerializedObject serialized = new SerializedObject(mesh);
        SerializedProperty readable = serialized.FindProperty("m_IsReadable");
        if (readable == null) throw new InvalidOperationException("Unity mesh readability property was not found.");
        readable.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mesh);
    }

    private static void ApplyTreePrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) throw new InvalidOperationException("Tree prefab not found: " + prefabPath);
        JungleEnvironmentGenerator[] generators = UnityEngine.Object.FindObjectsByType<JungleEnvironmentGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (JungleEnvironmentGenerator generator in generators)
        {
            generator.treePrefab = prefab;
            EditorUtility.SetDirty(generator);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Mesh Quantization: " + generators.Length + " generators now use " + prefabPath);
    }

    private static void EnsureFolders()
    {
        string parent = "Assets/_Scenes/Chips/Jungle Runner/Prefabs/Environment";
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder(parent, "Quantized");
    }
}
#endif
