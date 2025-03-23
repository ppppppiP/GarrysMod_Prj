using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpSound : MonoBehaviour
{
    [SerializeField] GameObject soundObj;
    [SerializeField] Transform pos;
    public void SpSound1()
    {
        Instantiate(soundObj, pos.position, Quaternion.identity);
    }
}
