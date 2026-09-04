using UnityEngine;

public sealed class AncientTrapCycle : MonoBehaviour
{
    public enum TrapKind { FloorSpikes, FallingRock, SweepingBeam, WallBlock, RotatingBeam, Shockwave, FallingColumn }
    private enum Phase { Waiting, Warning, Attack, Return }

    [Header("Тип и ссылки")]
    [InspectorName("Тип ловушки")] public TrapKind kind;
    [InspectorName("Предупреждение")] public GameObject warningVisual;
    [InspectorName("Опасная часть")] public Transform hazardVisual;
    [InspectorName("Коллайдер урона")] public Collider damageCollider;
    [InspectorName("Дополнительные коллайдеры урона")] public Collider[] additionalDamageColliders;

    [Header("Цикл ловушки")]
    [InspectorName("Задержка перед первым запуском"), Min(0f)] public float initialDelay = 1f;
    [InspectorName("Ожидание между атаками"), Min(0.1f)] public float waitingDuration = 2.4f;
    [InspectorName("Время предупреждения"), Min(0.1f)] public float warningDuration = 1.2f;
    [InspectorName("Время атаки"), Min(0.1f)] public float attackDuration = 1f;
    [InspectorName("Время возврата"), Min(0.1f)] public float returnDuration = 0.8f;
    [InspectorName("Смещение во время атаки")] public Vector3 attackOffset = Vector3.zero;
    [InspectorName("Поворот во время атаки")] public Vector3 attackEuler = Vector3.zero;
    [InspectorName("Масштаб во время атаки")] public Vector3 attackScale = Vector3.one;

    private AncientTrapHallLevel level;
    private Phase phase;
    private float phaseTime;
    private float speedMultiplier = 1f;
    private bool runtimeActive;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;

    public void Bind(AncientTrapHallLevel owner) { level = owner; }

    private void Awake()
    {
        if (hazardVisual == null) hazardVisual = transform;
        basePosition = hazardVisual.localPosition;
        baseRotation = hazardVisual.localRotation;
        baseScale = hazardVisual.localScale;
        if (damageCollider == null && hazardVisual != null) damageCollider = hazardVisual.GetComponent<Collider>();
        ResetCycle();
    }

    public void ResetCycle()
    {
        phase = Phase.Waiting;
        phaseTime = -initialDelay;
        ApplyPose(0f);
        SetWarning(false);
        SetDamage(false);
        SetTransientHazardVisible(false);
    }

    public void SetRuntimeState(bool active, float speed)
    {
        speedMultiplier = Mathf.Max(0.1f, speed);
        if (runtimeActive == active) return;
        runtimeActive = active;
        if (runtimeActive)
        {
            enabled = true;
            ResetCycle();
            return;
        }
        SetWarning(false);
        SetDamage(false);
        ApplyPose(0f);
        SetTransientHazardVisible(false);
        enabled = false;
    }

    private void Update()
    {
        if (!runtimeActive || level == null || !level.SimulationRunning) return;
        phaseTime += Time.deltaTime * speedMultiplier;
        float duration = phase == Phase.Waiting ? waitingDuration : phase == Phase.Warning ? warningDuration : phase == Phase.Attack ? attackDuration : returnDuration;
        float progress = Mathf.Clamp01(Mathf.Max(0f, phaseTime) / Mathf.Max(0.05f, duration));

        if (phase == Phase.Warning) ApplyPose(0f);
        else if (phase == Phase.Attack) ApplyPose(Mathf.SmoothStep(0f, 1f, progress));
        else if (phase == Phase.Return) ApplyPose(1f - Mathf.SmoothStep(0f, 1f, progress));

        if (phaseTime < duration) return;
        phaseTime = 0f;
        if (phase == Phase.Waiting) { phase = Phase.Warning; SetWarning(true); SetTransientHazardVisible(false); }
        else if (phase == Phase.Warning) { phase = Phase.Attack; SetWarning(false); SetTransientHazardVisible(true); SetDamage(true); }
        else if (phase == Phase.Attack) { phase = Phase.Return; SetDamage(false); }
        else { phase = Phase.Waiting; ApplyPose(0f); SetTransientHazardVisible(false); }
    }

    private void ApplyPose(float value)
    {
        if (hazardVisual == null) return;
        if (kind == TrapKind.RotatingBeam)
        {
            hazardVisual.localPosition = basePosition;
            hazardVisual.localRotation = baseRotation * Quaternion.Euler(0f, attackEuler.y * value, 0f);
            hazardVisual.localScale = baseScale;
            return;
        }
        hazardVisual.localPosition = Vector3.Lerp(basePosition, basePosition + attackOffset, value);
        hazardVisual.localRotation = Quaternion.Slerp(baseRotation, baseRotation * Quaternion.Euler(attackEuler), value);
        hazardVisual.localScale = Vector3.Scale(baseScale, Vector3.Lerp(Vector3.one, attackScale, value));
    }

    private void SetWarning(bool value) { if (warningVisual != null) warningVisual.SetActive(value); }
    private void SetTransientHazardVisible(bool value)
    {
        if (hazardVisual == null) return;
        if (kind == TrapKind.FallingRock || kind == TrapKind.Shockwave)
            hazardVisual.gameObject.SetActive(value);
    }
    private void SetDamage(bool value)
    {
        if (damageCollider != null) damageCollider.enabled = value;
        if (additionalDamageColliders == null) return;
        for (int i = 0; i < additionalDamageColliders.Length; i++)
            if (additionalDamageColliders[i] != null) additionalDamageColliders[i].enabled = value;
    }
}
