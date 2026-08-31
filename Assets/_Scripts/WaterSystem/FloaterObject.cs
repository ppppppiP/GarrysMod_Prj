using UnityEngine;

public class FloaterObject : MonoBehaviour
{
    private Rigidbody rb;
    public float floatStrength = 1.5f; // �������� ��������
    private int waterTriggersCount = 0; // ���������� ����������� � �����
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
            rb.linearDamping = 1f;
            rb.angularDamping = 1f;
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
                rb.linearDamping = 0f;
                rb.angularDamping = 0f;
            }
            UpdateWaterLevel();
        }
    }

    private void UpdateWaterLevel()
    {
        // ������� ������������ ������ ����� ���� ��������� ����, � ������� ��������� ������
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
