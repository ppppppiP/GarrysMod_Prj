using UnityEngine;

public class FloaterObject : MonoBehaviour
{
    private Rigidbody rb;
    public float floatStrength = 1.5f; // Усиление всплытия
    private int waterTriggersCount = 0; // Количество пересечений с водой
    private float waterLevel = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            waterTriggersCount++;
            UpdateWaterLevel();
            rb.drag = 1f;
            rb.angularDrag = 1f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            waterTriggersCount--;
            if (waterTriggersCount <= 0)
            {
                waterTriggersCount = 0;
                rb.drag = 0f;
                rb.angularDrag = 0f;
            }
            UpdateWaterLevel();
        }
    }

    private void UpdateWaterLevel()
    {
        // Находим максимальную высоту среди всех триггеров воды, в которых находится объект
        Collider[] waterTriggers = Physics.OverlapSphere(transform.position, 5f, LayerMask.GetMask("Water"));
        if (waterTriggers.Length > 0)
        {
            float highestWaterLevel = float.MinValue;
            foreach (Collider water in waterTriggers)
            {
                highestWaterLevel = Mathf.Max(highestWaterLevel, water.bounds.max.y);
            }
            waterLevel = highestWaterLevel;
        }
    }

    private void FixedUpdate()
    {
        if (waterTriggersCount > 0)
        {
            float difference = waterLevel - transform.position.y;
            if (difference > 0)
            {
                float forceAmount = Mathf.Clamp(difference * floatStrength, 0, Mathf.Abs(Physics.gravity.y) * 2f);
                rb.AddForce(Vector3.up * forceAmount, ForceMode.Acceleration);
            }
        }
    }
}
