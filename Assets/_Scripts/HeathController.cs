using System;
using UnityEngine;
using UnityEngine.Events;

public class HeathController: MonoBehaviour, IDamagable
{
    [SerializeField] float m_playerHelath = 100;
    public UnityEvent OnPlayerDie;
    [HideInInspector] public Action<float> OnChangePlayerHealth;
    public void GetDamage(float damage)
    {
        if(damage > m_playerHelath)
        {
            m_playerHelath = 0;
        }
        if (damage < 0) return;

        m_playerHelath -= damage;
        OnChangePlayerHealth?.Invoke(m_playerHelath);
        if(m_playerHelath <= 0)
        {
            OnPlayerDie?.Invoke();
        }
    }
}
