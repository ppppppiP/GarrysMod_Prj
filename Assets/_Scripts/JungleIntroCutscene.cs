using UnityEngine;

[DisallowMultipleComponent]
public sealed class JungleIntroCutscene : MonoBehaviour
{
    [Header("Boulder path — move these points in Scene view")]
    public Transform boulderStart;
    public Transform boulderMiddle;
    public Transform boulderEnd;
    [Tooltip("Where the boulder is placed after the intro finishes.")]
    public Transform boulderGameplay;

    [Header("Replaceable intro scenery")]
    [Tooltip("Prefab instance of the hill. It is visible during the intro and disabled when gameplay starts.")]
    public GameObject introSlope;

    [Header("Camera path — move and rotate these points")]
    public Transform cameraStart;
    public Transform cameraGameplay;
    public bool lookAtBoulderDuringRoll = true;
    public float cameraLookHeight = 0.5f;

    [Header("Timing")]
    [Min(0.1f)] public float boulderRollDuration = 2.2f;
    [Min(0.1f)] public float cameraTransitionDuration = 1.4f;
    [Min(0f)] public float emptyRoadDuration = 4f;

    [Header("Motion curves")]
    public AnimationCurve boulderMotion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve cameraMotion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
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
            if (lookAtBoulderDuringRoll)
            {
                Vector3 target = targetBoulder.position + Vector3.up * cameraLookHeight;
                targetCamera.transform.rotation = Quaternion.LookRotation(target - targetCamera.transform.position);
            }
            return false;
        }

        float normalized = Mathf.Clamp01((elapsed - boulderRollDuration) / cameraTransitionDuration);
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
