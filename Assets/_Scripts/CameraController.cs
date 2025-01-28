using UnityEngine;


public class CameraController : MonoBehaviour
{
    public Transform target; // Цель (игрок)
    public float distance = 5f; // Расстояние до цели
    public float xSpeed = 200f; // Скорость вращения по горизонтали
    public float ySpeed = 200f; // Скорость вращения по вертикали
    public float yMinLimit = -20f; // Минимальный угол наклона
    public float yMaxLimit = 80f; // Максимальный угол наклона

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

        // Вращение камеры
        xRotation += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
        yRotation -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
        yRotation = ClampAngle(yRotation, yMinLimit, yMaxLimit);

        // Применение вращения
        Quaternion rotation = Quaternion.Euler(yRotation, xRotation, 0);
        Vector3 negDistance = new Vector3(0, 0, -distance);
        Vector3 position = rotation * negDistance + target.position;

        // Установка позиции и вращения камеры
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
