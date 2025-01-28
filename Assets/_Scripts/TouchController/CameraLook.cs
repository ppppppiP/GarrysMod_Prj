//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UniversalMobileController;
//using YG;

//public class CameraLook : MonoBehaviour
//{
//    int cameraFingerId = -1;
//    public static CameraLook Instance { get; private set; }

//    private float XMove;
//    private float YMove;
//    private float XRotation;
//    [SerializeField] private Transform PlayerBody;
//    public Vector2 LockAxis;
//    public float Sensitivity = 40f;
//    public SpecialTouchPad touchPad;

//    public float Smoothing = 0.1f;
//    private Vector2 currentLookAxis;

//    void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Destroy(gameObject);
//            return;
//        }
//        DontDestroyOnLoad(gameObject);
//        Sensitivity = YandexGame.savesData.sensitivity;
//    }

//    void Start()
//    {
//        currentLookAxis = Vector2.zero; 

//    }

//    //void Update()
//    //{

//    //    if (YandexGame.EnvironmentData.isDesktop)
//    //    {
//    //        currentLookAxis = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
//    //    }
//    //    else
//    //    {

//    //        currentLookAxis = touchPad.GetHorizontalAndVerticalValue(); //Vector2.Lerp(currentLookAxis, LockAxis, Smoothing * Time.deltaTime);
//    //    }
//    //    XMove = currentLookAxis.x * Sensitivity * Time.deltaTime;
//    //    YMove = currentLookAxis.y * Sensitivity * Time.deltaTime;

//    //    XRotation -= YMove;
//    //    XRotation = Mathf.Clamp(XRotation, -90f, 90f);

//    //    transform.localRotation = Quaternion.Euler(XRotation, 0, 0);
//    //    PlayerBody.Rotate(Vector3.up * XMove);
//    //}

//    public void UpdateSensitivity(float newSensitivity)
//    {
//        Sensitivity = newSensitivity;
//        YandexGame.savesData.sensitivity = newSensitivity;
//        YandexGame.SaveProgress();
//    }

//    [SerializeField] float m_SensitivityPanelRotate = 1;
//    [SerializeField] float m_MaxYAngle = 80.0f;

//    private float _rotationX = 0.0f;

//    private void LateUpdate()
//    {

//        //Cursor.lockState = CursorLockMode.Confined;
//        //Cursor.visible = true;

//        float mouseX = 0;
//        float mouseY = 0;
//        if (YandexGame.EnvironmentData.isDesktop)
//        {
//            currentLookAxis = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
//            XMove = currentLookAxis.x * Sensitivity * Time.deltaTime;
//            YMove = currentLookAxis.y * Sensitivity * Time.deltaTime;

//            XRotation -= YMove;
//            XRotation = Mathf.Clamp(XRotation, -90f, 90f);

//            transform.localRotation = Quaternion.Euler(XRotation, 0, 0);
//            PlayerBody.Rotate(Vector3.up * XMove);
//        }
//        else
//        {

//            if (Input.touchCount > 0)
//            {
//                foreach (Touch touch in Input.touches)
//                {
//                    if (cameraFingerId == -1 && touch.position.x > Screen.width / 2 && touch.phase == TouchPhase.Began)
//                    {
//                        if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
//                        {
//                            cameraFingerId = touch.fingerId;
//                        }
//                    }


//                    if (touch.fingerId == cameraFingerId)
//                    {
//                        if (touch.phase == TouchPhase.Moved)
//                        {
//                            mouseY = -touch.deltaPosition.y * m_SensitivityPanelRotate;
//                            mouseX = -touch.deltaPosition.x * m_SensitivityPanelRotate;
//                        }
//                        else if (touch.phase == TouchPhase.Stationary)
//                        {
//                            mouseY = 0;
//                            mouseX = 0;
//                        }
//                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
//                        {

//                            cameraFingerId = -1;
//                            mouseY = 0;
//                            mouseX = 0;
//                        }
//                    }
//                }
//            }

//            PlayerBody.transform.Rotate(Vector3.up * mouseX * Sensitivity * Time.deltaTime);

//            _rotationX -= mouseY * Sensitivity * Time.deltaTime;
//            _rotationX = Mathf.Clamp(_rotationX, -m_MaxYAngle, m_MaxYAngle);
//            transform.localRotation = Quaternion.Euler(_rotationX, 0.0f, 0.0f);
//        }

//    }

//}