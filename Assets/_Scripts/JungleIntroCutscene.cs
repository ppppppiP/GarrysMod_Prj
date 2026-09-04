using UnityEngine;

[DisallowMultipleComponent]
public sealed class JungleIntroCutscene : MonoBehaviour
{
    [Header("Траектория камня")]
    [InspectorName("Старт камня")]
    public Transform boulderStart;
    [InspectorName("Изгиб траектории")]
    public Transform boulderMiddle;
    [InspectorName("Финиш камня")]
    public Transform boulderEnd;
    [InspectorName("Позиция камня в игре"), Tooltip("Сюда камень перемещается после окончания заставки.")]
    public Transform boulderGameplay;

    [Header("Декорации заставки")]
    [InspectorName("Склон"), Tooltip("Заменяемый склон: виден во время заставки и отключается при начале игры.")]
    public GameObject introSlope;

    [Header("Траектория камеры")]
    [InspectorName("Старт камеры")]
    public Transform cameraStart;
    [InspectorName("Игровая камера")]
    public Transform cameraGameplay;
    [InspectorName("Следить за камнем"), Tooltip("До начала перехода камера поворачивается в сторону камня.")]
    public bool lookAtBoulderDuringRoll = true;
    [InspectorName("Высота точки взгляда")]
    public float cameraLookHeight = 0.5f;

    [Header("Тайминги")]
    [InspectorName("Длительность движения камня")]
    [Min(0.1f)] public float boulderRollDuration = 2.2f;
    [InspectorName("Задержка перехода камеры"), Tooltip("Через сколько секунд от начала заставки камера начнёт переход к игроку.")]
    [Min(0f)] public float cameraTransitionDelay = 0.45f;
    [InspectorName("Длительность перехода камеры")]
    [Min(0.1f)] public float cameraTransitionDuration = 1.4f;
    [InspectorName("Длительность пустой дороги")]
    [Min(0f)] public float emptyRoadDuration = 4f;

    [Header("Кривые движения")]
    [InspectorName("Кривая движения камня")]
    public AnimationCurve boulderMotion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [InspectorName("Кривая движения камеры")]
    public AnimationCurve cameraMotion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [InspectorName("Скорость вращения камня")]
    [Min(0f)] public float boulderRotationSpeed = 520f;

    private Camera targetCamera;
    private Transform targetBoulder;
    private float startedAt;
    private bool running;

    public float EmptyRoadDuration { get { return emptyRoadDuration; } }

    public void Begin(Camera cameraToAnimate, Transform boulderToAnimate)
    {
        targetCamera = cameraToAnimate;
        targetBoulder = boulderToAnimate;
        startedAt = Time.unscaledTime;
        running = targetCamera != null && targetBoulder != null && HasRequiredPoints();
        if (!running) return;
        if (introSlope != null) introSlope.SetActive(true);
        targetBoulder.SetPositionAndRotation(boulderStart.position, boulderStart.rotation);
        targetCamera.transform.SetPositionAndRotation(cameraStart.position, cameraStart.rotation);
    }

    public bool Tick()
    {
        if (!running) return true;
        float elapsed = Time.unscaledTime - startedAt;
        if (elapsed < boulderRollDuration)
        {
            float t = boulderMotion.Evaluate(Mathf.Clamp01(elapsed / boulderRollDuration));
            Vector3 first = Vector3.Lerp(boulderStart.position, boulderMiddle.position, t);
            Vector3 second = Vector3.Lerp(boulderMiddle.position, boulderEnd.position, t);
            targetBoulder.position = Vector3.Lerp(first, second, t);
            targetBoulder.Rotate(Vector3.right, boulderRotationSpeed * Time.unscaledDeltaTime, Space.Self);
            if (lookAtBoulderDuringRoll && elapsed < cameraTransitionDelay)
            {
                Vector3 target = targetBoulder.position + Vector3.up * cameraLookHeight;
                targetCamera.transform.rotation = Quaternion.LookRotation(target - targetCamera.transform.position);
            }
        }
        else
        {
            targetBoulder.SetPositionAndRotation(boulderEnd.position, boulderEnd.rotation);
        }

        if (elapsed < cameraTransitionDelay) return false;

        float normalized = Mathf.Clamp01((elapsed - cameraTransitionDelay) / cameraTransitionDuration);
        float tCamera = cameraMotion.Evaluate(normalized);
        targetCamera.transform.position = Vector3.Lerp(cameraStart.position, cameraGameplay.position, tCamera);
        targetCamera.transform.rotation = Quaternion.Slerp(cameraStart.rotation, cameraGameplay.rotation, tCamera);
        if (normalized < 1f) return false;

        Transform gameplayPoint = boulderGameplay != null ? boulderGameplay : boulderEnd;
        targetBoulder.SetPositionAndRotation(gameplayPoint.position, gameplayPoint.rotation);
        targetCamera.transform.SetPositionAndRotation(cameraGameplay.position, cameraGameplay.rotation);
        if (introSlope != null) introSlope.SetActive(false);
        running = false;
        return true;
    }

    private bool HasRequiredPoints()
    {
        return boulderStart != null && boulderMiddle != null && boulderEnd != null && cameraStart != null && cameraGameplay != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasRequiredPoints()) return;
        Gizmos.color = new Color(1f, 0.35f, 0.05f, 1f);
        Vector3 previous = boulderStart.position;
        for (int i = 1; i <= 24; i++)
        {
            float t = i / 24f;
            Vector3 point = Vector3.Lerp(Vector3.Lerp(boulderStart.position, boulderMiddle.position, t), Vector3.Lerp(boulderMiddle.position, boulderEnd.position, t), t);
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(cameraStart.position, cameraGameplay.position);
        Gizmos.DrawWireSphere(cameraStart.position, 0.3f);
        Gizmos.DrawWireSphere(cameraGameplay.position, 0.3f);
    }
}
