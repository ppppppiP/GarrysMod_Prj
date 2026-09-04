using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class AncientHallPlayerAdapter : MonoBehaviour
{
    [InspectorName("Уровень")] public AncientTrapHallLevel level;
    [InspectorName("Половина размера арены"), Min(2f)] public float arenaHalfSize = 12.5f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (level == null) level = FindFirstObjectByType<AncientTrapHallLevel>();
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, -arenaHalfSize, arenaHalfSize);
        position.z = Mathf.Clamp(position.z, -arenaHalfSize, arenaHalfSize);
        if ((position - transform.position).sqrMagnitude < 0.0001f) return;
        if (controller != null && controller.enabled) controller.Move(position - transform.position);
        else transform.position = position;
    }

    private void OnTriggerEnter(Collider other)
    {
        AncientTrapDamage damage = other.GetComponent<AncientTrapDamage>();
        if (damage != null) { if (level != null) level.TakeDamage(); return; }
        AncientHallPickup pickup = other.GetComponent<AncientHallPickup>();
        if (pickup != null && level != null) level.Collect(pickup);
    }
}
