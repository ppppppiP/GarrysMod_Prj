using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class JungleWorldBend : MonoBehaviour
{
    private static readonly int BendOriginId = Shader.PropertyToID("_JR_BendOrigin");
    private static readonly int BendParametersId = Shader.PropertyToID("_JR_BendParameters");
    private static readonly int SideBendParametersId = Shader.PropertyToID("_JR_SideBendParameters");
    private static readonly int BendForwardId = Shader.PropertyToID("_JR_BendForward");
    private static readonly int BendRightId = Shader.PropertyToID("_JR_BendRight");

    [Header("Изгиб мира для мобильных и WebGL")]
    [InspectorName("Включить изгиб")]
    public bool bendEnabled = true;
    [InspectorName("Точка начала"), Tooltip("Обычно это машина или центр игрового пространства. Физика при этом не меняется.")]
    public Transform bendOrigin;
    [InspectorName("Вертикальный изгиб"), Tooltip("Положительное значение загибает далёкую дорогу вниз. Рекомендуемый диапазон: 0,001–0,006.")]
    [Range(-0.01f, 0.01f)] public float verticalCurvature = 0.003f;
    [InspectorName("Боковой изгиб"), Tooltip("Изгиб дороги влево или вправо. Ноль оставляет дорогу прямой по горизонтали.")]
    [Range(-0.01f, 0.01f)] public float horizontalCurvature;
    [InspectorName("Начало изгиба"), Tooltip("Расстояние от точки начала, до которого мир остаётся прямым.")]
    [Min(0f)] public float startDistance = 4f;
    [InspectorName("Максимальная дистанция расчёта"), Tooltip("Ограничивает деформацию дальних вершин и предотвращает чрезмерный изгиб.")]
    [Min(10f)] public float maximumDistance = 120f;

    [Header("Скругление боков к центру")]
    [InspectorName("Сила скругления"), Tooltip("Плавно подтягивает левый и правый край к центру. Ноль отключает поперечное скругление.")]
    [Range(0f, 0.05f)] public float sideInwardCurvature = 0.015f;
    [InspectorName("Подъём краёв"), Tooltip("Дополнительно загибает края вверх, создавая форму чаши. Ноль меняет только ширину.")]
    [Range(-0.02f, 0.02f)] public float sideVerticalCurvature = 0.002f;
    [InspectorName("Начало от центра"), Tooltip("Центральная часть указанной ширины остаётся прямой.")]
    [Min(0f)] public float sideStartDistance = 3.5f;
    [InspectorName("Максимальная боковая дистанция"), Tooltip("Ограничивает силу скругления на очень далёком фоне.")]
    [Min(5f)] public float maximumSideDistance = 30f;

    [InspectorName("Мобильный шейдер")]
    [SerializeField] private Shader mobileShader;

    private readonly Dictionary<Material, Material> curvedMaterials = new Dictionary<Material, Material>();
    private Vector3 trackForward = Vector3.forward;
    private Vector3 trackRight = Vector3.right;

    public Shader MobileShader
    {
        get
        {
            if (mobileShader == null)
                mobileShader = Shader.Find("Jungle Runner/Mobile Curved Lit");
            return mobileShader;
        }
    }

    private void OnEnable()
    {
        ApplyGlobals();
    }

    private void LateUpdate()
    {
        ApplyGlobals();
    }

    private void OnValidate()
    {
        maximumDistance = Mathf.Max(startDistance + 1f, maximumDistance);
        maximumSideDistance = Mathf.Max(sideStartDistance + 1f, maximumSideDistance);
        ApplyGlobals();
    }

    public Material GetCurvedMaterial(Material source)
    {
        if (source == null || MobileShader == null)
            return source;

        Material curved;
        if (curvedMaterials.TryGetValue(source, out curved) && curved != null)
            return curved;

        curved = new Material(source)
        {
            name = source.name + " (Mobile Curved)",
            shader = MobileShader,
            enableInstancing = true,
            hideFlags = HideFlags.DontSave
        };
        if ((source.HasProperty("_MQQuantized") && source.GetFloat("_MQQuantized") > 0.5f) || source.IsKeywordEnabled("_MQ_QUANTIZED"))
        {
            curved.SetFloat("_MQQuantized", 1f);
            curved.EnableKeyword("_MQ_QUANTIZED");
        }
        curvedMaterials[source] = curved;
        return curved;
    }

    public void SetTrackOrientation(Vector3 forward, Vector3 right)
    {
        trackForward = forward.sqrMagnitude > 0.1f ? forward.normalized : Vector3.forward;
        trackRight = right.sqrMagnitude > 0.1f ? right.normalized : Vector3.right;
        ApplyGlobals();
    }

    private void ApplyGlobals()
    {
        Vector3 origin = bendOrigin != null ? bendOrigin.position : transform.position;
        Shader.SetGlobalVector(BendOriginId, new Vector4(origin.x, origin.y, origin.z, 1f));
        Shader.SetGlobalVector(BendForwardId, new Vector4(trackForward.x, trackForward.y, trackForward.z, 0f));
        Shader.SetGlobalVector(BendRightId, new Vector4(trackRight.x, trackRight.y, trackRight.z, 0f));
        Shader.SetGlobalVector(BendParametersId, bendEnabled && isActiveAndEnabled
            ? new Vector4(horizontalCurvature, verticalCurvature, startDistance, maximumDistance)
            : Vector4.zero);
        Shader.SetGlobalVector(SideBendParametersId, bendEnabled && isActiveAndEnabled
            ? new Vector4(sideInwardCurvature, sideVerticalCurvature, sideStartDistance, maximumSideDistance)
            : Vector4.zero);
    }

    private void OnDisable()
    {
        Shader.SetGlobalVector(BendParametersId, Vector4.zero);
        Shader.SetGlobalVector(SideBendParametersId, Vector4.zero);
    }

    private void OnDestroy()
    {
        foreach (Material material in curvedMaterials.Values)
        {
            if (material == null) continue;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
        curvedMaterials.Clear();
    }
}
