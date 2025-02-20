using UnityEngine;

public class Shelter: MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(TryGetComponent<PlayerController>(out PlayerController pla))
        {
            pla.isHided = true;
        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (TryGetComponent<PlayerController>(out PlayerController pla))
        {
            pla.isHided = false;
        }

    }
}