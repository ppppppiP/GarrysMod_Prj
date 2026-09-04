using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AncientHallBowl : MonoBehaviour
{
    private static readonly int CenterId = Shader.PropertyToID("_ATH_BowlCenter");
    private static readonly int ParametersId = Shader.PropertyToID("_ATH_BowlParameters");

    [Header("Визуальная чаша арены")]
    [InspectorName("Включить искривление")] public bool bowlEnabled = true;
    [InspectorName("Центр чаши")] public Transform bowlCenter;
    [InspectorName("Сила подъёма краёв"), Range(0f, 0.02f)] public float curvature = 0.0075f;
    [InspectorName("Плоская зона в центре"), Range(0f, 8f)] public float flatRadius = 1.5f;
    [InspectorName("Максимальный подъём"), Range(0f, 6f)] public float maximumLift = 3.2f;
    [InspectorName("Искривлять модель игрока")] public bool bendPlayer = true;
    [InspectorName("Игрок")] public PlayerController player;

    private readonly Dictionary<Material, Material> playerMaterialCopies = new Dictionary<Material, Material>();
    private Shader bowlShader;

    private void Awake()
    {
        bowlShader = Shader.Find("Ancient Trap Hall/Mobile Bowl Lit");
        if (bendPlayer) ApplyToPlayer();
        ApplyGlobals();
    }

    private void ApplyGlobals()
    {
        Vector3 center = bowlCenter != null ? bowlCenter.position : transform.position;
        Shader.SetGlobalVector(CenterId, new Vector4(center.x, center.y, center.z, 1f));
        Shader.SetGlobalVector(ParametersId, new Vector4(curvature, flatRadius, maximumLift, bowlEnabled && isActiveAndEnabled ? 1f : 0f));
    }

    private void OnValidate()
    {
        if (Application.isPlaying) ApplyGlobals();
    }

    private void ApplyToPlayer()
    {
        if (player == null || bowlShader == null) return;
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer target in renderers)
        {
            Material[] sourceMaterials = target.sharedMaterials;
            Material[] curvedMaterials = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++) curvedMaterials[i] = GetPlayerMaterial(sourceMaterials[i]);
            target.sharedMaterials = curvedMaterials;
        }
    }

    private Material GetPlayerMaterial(Material source)
    {
        if (source == null) return null;
        if (source.shader == bowlShader) return source;
        if (playerMaterialCopies.TryGetValue(source, out Material copy) && copy != null) return copy;

        Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
        Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        copy = new Material(bowlShader) { name = source.name + " (Чаша)", enableInstancing = true };
        copy.SetColor("_BaseColor", color);
        if (texture != null) copy.SetTexture("_BaseMap", texture);
        playerMaterialCopies[source] = copy;
        return copy;
    }

    private void OnDisable() => Shader.SetGlobalVector(ParametersId, Vector4.zero);

    private void OnDestroy()
    {
        Shader.SetGlobalVector(ParametersId, Vector4.zero);
        foreach (Material material in playerMaterialCopies.Values) if (material != null) Destroy(material);
        playerMaterialCopies.Clear();
    }
}
