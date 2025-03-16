using Lean.Pool;
using System.Collections;
using UnityEngine;

public class Shelter: MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<PlayerController>(out PlayerController pla))
        {
            pla.isHided = true;
        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out PlayerController pla))
        {
            pla.isHided = false;
        }

    }
}
