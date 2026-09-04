using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class AncientHallPickup : MonoBehaviour
{
    public enum PickupKind { Coin, Magnet, DoubleCoins, Shield }
    [InspectorName("Тип предмета")] public PickupKind kind;
    [InspectorName("Скорость вращения")] public float rotationSpeed = 100f;
    [InspectorName("Время жизни"), Min(2f)] public float lifeTime = 18f;
    private AncientTrapHallLevel level;
    public PickupKind Kind => kind;

    public void Bind(AncientTrapHallLevel owner) { level = owner; }
    private void Awake() { GetComponent<Collider>().isTrigger = true; }
    private void Start() { if (level == null) level = FindFirstObjectByType<AncientTrapHallLevel>(); }
    private void Update() { transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World); lifeTime -= Time.deltaTime; if (lifeTime <= 0f) Destroy(gameObject); }
}
