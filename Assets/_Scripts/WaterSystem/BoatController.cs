using UnityEngine;

public class BoatController : MonoBehaviour
{
    private Rigidbody rb;
    public float thrust = 10f; // Сила толчка вперёд
    public float turnSpeed = 5f; // Скорость поворота
    public float rotationSmoothness = 2f; // Насколько плавно поворачивать лодку под наклон воды

    public Transform frontPoint; // Точка в носу лодки
    public Transform backPoint;  // Точка в корме лодки
    public Transform leftPoint;  // Левая сторона лодки
    public Transform rightPoint; // Правая сторона лодки

    private float moveInput;
    private float turnInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        if (moveInput != 0)
        {
            rb.AddForce(transform.forward * moveInput * thrust, ForceMode.Force);
        }

        if (turnInput != 0)
        {
            rb.AddTorque(Vector3.up * turnInput * turnSpeed, ForceMode.Force);
        }

        AdjustBoatRotation();

        rb.velocity *= 0.99f;
        rb.angularVelocity *= 0.98f;
    }

    private void AdjustBoatRotation()
    {
        Vector3 waterNormal = GetBestWaterNormal();
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, waterNormal) * transform.rotation;
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothness));
    }

    private Vector3 GetBestWaterNormal()
    {
        Vector3[] normals = new Vector3[4];
        normals[0] = GetWaterNormal(frontPoint.position);
        normals[1] = GetWaterNormal(backPoint.position);
        normals[2] = GetWaterNormal(leftPoint.position);
        normals[3] = GetWaterNormal(rightPoint.position);

        // Выбираем наибольший по наклону угол воды
        Vector3 bestNormal = Vector3.up;
        float maxDeviation = 0f;

        foreach (Vector3 normal in normals)
        {
            float deviation = Vector3.Angle(Vector3.up, normal);
            if (deviation > maxDeviation)
            {
                maxDeviation = deviation;
                bestNormal = normal;
            }
        }

        return bestNormal;
    }

    private Vector3 GetWaterNormal(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out hit, 5f, LayerMask.GetMask("Water")))
        {
            return hit.normal;
        }
        return Vector3.up;
    }
}
