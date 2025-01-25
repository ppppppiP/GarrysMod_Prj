using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    [SerializeField] float _speed;

    [SerializeField]
    float _minX,
          _maxX,
          _minY,
          _maxY;

    private float _eulerX = 0, _eulerY = 0;

    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Update()
    {
        RotateCamera(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
    }

    public void RotateCamera(float X, float Y)
    {
        X = X * _speed * Time.deltaTime;
        Y = Y * _speed * Time.deltaTime;

        _eulerX += Y;
        _eulerY += X;



        transform.eulerAngles = new Vector3(_eulerX, _eulerY, 0);
        // Debug.LogAssertion(transform.eulerAngles); че бля
        _eulerX = Mathf.Clamp(_eulerX, _minX, _maxX);
        //_eulerY = Mathf.Clamp(_eulerY, _minY, _maxY);
    }
    public void SetMovementX()
    {

    }
    public void moveX()
    {

    }

    public void MoveXX()
    {

    }
}
