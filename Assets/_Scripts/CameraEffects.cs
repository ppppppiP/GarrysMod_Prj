using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [SerializeField]private Camera _camera;
    private Tween _tween;
    private float _defaultFov;


    public static CameraEffects instance;
    private void Awake()
    {
        _tween.Kill();
       instance = this;
        _defaultFov = _camera.fieldOfView;
    }

    /// <summary>
    /// Эффект тряски камеры
    /// </summary>
    public void Shake()
    {
        transform.DOShakePosition(0.2f, 0.5f, 20, 90, false, true);
    }

    /// <summary>
    /// Ускорение - увеличение FOV
    /// </summary>
    public void SetSpeedFov(float speedFov = 90f, float duration = 0.5f)
    {
        _tween?.Kill(); // Останавливаем предыдущую анимацию
        _tween = _camera.DOFieldOfView(speedFov, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Возвращение FOV к стандартному значению
    /// </summary>
    public void DoDefaultFov(float duration = 0.5f)
    {
        _tween?.Kill();
        _tween = _camera.DOFieldOfView(_defaultFov, duration).SetEase(Ease.InOutQuad);
    }

    /// <summary>
    /// Имитация прыжка - FOV расширяется и сжимается
    /// </summary>
    public void DoJumpFov(float jumpFov = 5f, float duration = 1f)
    {
        _tween?.Kill();
        float fov = Camera.main.fieldOfView + jumpFov;

        _camera.DOFieldOfView(fov, duration / 2).SetEase(Ease.OutQuad)
            .OnComplete(() => _camera.DOFieldOfView(fov- jumpFov, 0.3f).SetEase(Ease.InQuad));
        
    }
}
