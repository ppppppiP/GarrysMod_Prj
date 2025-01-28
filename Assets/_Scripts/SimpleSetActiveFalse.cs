using System.Collections;
using UnityEngine;

public class SimpleSetActiveFalse: MonoBehaviour
{
    public float time;
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);  
    }
}