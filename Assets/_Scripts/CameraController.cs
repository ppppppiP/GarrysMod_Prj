using UnityEngine;


public class CameraController : MonoBehaviour
{
    public Transform target; // ���� (�����)
    public float distance = 5f; // ���������� �� ����
    public float xSpeed = 200f; // �������� �������� �� �����������
    public float ySpeed = 200f; // �������� �������� �� ���������
    public float yMinLimit = -20f; // ����������� ���� �������
    public float yMaxLimit = 80f; // ������������ ���� �������

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        xRotation = angles.y;
        yRotation = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // �������� ������
        xRotation += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
        yRotation -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
        yRotation = ClampAngle(yRotation, yMinLimit, yMaxLimit);

        // ���������� ��������
        Quaternion rotation = Quaternion.Euler(yRotation, xRotation, 0);
        Vector3 negDistance = new Vector3(0, 0, -distance);
        Vector3 position = rotation * negDistance + target.position;

        // ��������� ������� � �������� ������
        transform.rotation = rotation;
        transform.position = position;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
