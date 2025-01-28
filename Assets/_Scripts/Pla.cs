using UnityEngine;

public class Pla: MonoBehaviour
{
    public static Pla instance;

    private void Awake()
    {
        instance = this;
    }
}