using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingObject : MonoBehaviour
{
    public static LoadingObject instance;
    private void Awake()
    {
        instance = this;
    }
}
