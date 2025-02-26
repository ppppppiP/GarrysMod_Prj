using System.Collections;
using UnityEngine;

public class DamageRain : MonoBehaviour
{
    public float damage = 10f;
    public float damageInterval = 1f;
    private Coroutine damageCoroutine;

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<IDamagable>(out IDamagable damagable))
        {
            if (!PlayerController.instance.isHided)
            {
                if (damageCoroutine == null)
                {
                    damageCoroutine = StartCoroutine(ApplyDamageOverTime(damagable));
                }
            }
            else
            {
                if (damageCoroutine != null)
                {
                    StopCoroutine(damageCoroutine);
                    damageCoroutine = null;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IDamagable>(out IDamagable damagable))
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator ApplyDamageOverTime(IDamagable damagable)
    {
        while (!PlayerController.instance.isHided)
        {
            damagable.GetDamage(damage);
            yield return new WaitForSeconds(damageInterval);
        }
        damageCoroutine = null; // Установите в null, когда корутина завершится
    }
}
