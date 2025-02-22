using System;
using UnityEngine;
using UnityEngine.Events;

public class HeathController: MonoBehaviour, IDamagable
{
    [SerializeField] float m_playerHelath = 100;
    float maxHealth;
    public UnityEvent OnPlayerDie;
    [HideInInspector] public Action<float> OnChangePlayerHealth;

    private void Start()
    {
        maxHealth = m_playerHelath;
    }
    public void GetDamage(float damage)
    {
        if(damage > m_playerHelath)
        {
            m_playerHelath = 0;
        }
     
        if(m_playerHelath > maxHealth)
        {
            m_playerHelath = maxHealth;
        }
        m_playerHelath -= damage;
        OnChangePlayerHealth?.Invoke(m_playerHelath);
        if(m_playerHelath <= 0)
        {

            OnPlayerDie?.Invoke();
            m_playerHelath = maxHealth;
            OnChangePlayerHealth?.Invoke(m_playerHelath);
        }
    }

   
}
