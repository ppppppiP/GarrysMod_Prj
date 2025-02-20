using System.Collections;
using UnityEngine;

public class DamageMagma : MonoBehaviour
{
    public float damage = 10f;
    public float damageInterval = 1f; 
    bool isEnter;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamagable>(out IDamagable damagable))
        {
            isEnter = true;
            StartCoroutine(ApplyDamageOverTime(damagable));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IDamagable>(out IDamagable damagable))
        {
            isEnter = false;
        }
    }
    private IEnumerator ApplyDamageOverTime(IDamagable damagable)
    {
        float elapsedTime = 0f;

        while (isEnter)
        {
            damagable.GetDamage(damage);

            yield return new WaitForSeconds(damageInterval);
        }
    }
}
