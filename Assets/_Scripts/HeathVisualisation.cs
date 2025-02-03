using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HeathVisualisation : MonoBehaviour
{
    [SerializeField] Image m_HealthBar;
    [SerializeField] HeathController m_HealthController;

    private void OnEnable()
    {
        m_HealthController.OnChangePlayerHealth += UpdateHealthBar;
    }

    private void OnDisable()
    {
        m_HealthController.OnChangePlayerHealth -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float playerHP)
    {
        // Предполагаем, что playerHP находится в диапазоне от 0 до 100
        float normalizedHealth = playerHP / 100f;

        // Плавно анимируем изменение fillAmount
       // m_HealthBar.fillAmount = normalizedHealth; // Устанавливаем начальное значение
        m_HealthBar.DOFillAmount(normalizedHealth, 0.5f); // Анимируем заполнение в течение 0.5 секунд
    }
}
