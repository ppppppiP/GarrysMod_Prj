using UnityEngine;

[DisallowMultipleComponent]
public sealed class LightweightRotator : MonoBehaviour
{
    [SerializeField] private Vector3 localAxis = Vector3.forward;
    [SerializeField] private float degreesPerSecond = 30f;

    public Vector3 LocalAxis
    {
        get => localAxis;
        set => localAxis = value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.forward;
    }

    public float DegreesPerSecond
    {
        get => degreesPerSecond;
        set => degreesPerSecond = value;
    }

    private void OnEnable()
    {
        LightweightAnimationScheduler.Register(this);
    }

    private void OnDisable()
    {
        LightweightAnimationScheduler.Unregister(this);
    }

    internal void Tick(float deltaTime)
    {
        transform.Rotate(localAxis, degreesPerSecond * deltaTime, Space.Self);
    }
}
