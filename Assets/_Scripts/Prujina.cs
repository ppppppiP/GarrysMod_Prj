using UnityEngine;

public class Prujina : MonoBehaviour
{
    public float Strength;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out PlayerController baff))
        {
            baff.Jump(Strength);
        }
    }

   
}