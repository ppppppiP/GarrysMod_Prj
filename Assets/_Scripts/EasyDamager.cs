using UnityEngine;

public class EasyDamager: MonoBehaviour
{
    [SerializeField] float Damage;

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<IDamagable>(out IDamagable damagable))
        {
            damagable.GetDamage(Damage);
        }
    }
}
