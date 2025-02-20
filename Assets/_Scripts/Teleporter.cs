using UnityEngine;

public class Teleporter: MonoBehaviour
{
    [SerializeField] GameObject m_Object;
    [SerializeField] Transform m_newPosition;

    public void Teleport()
    {
        m_Object.transform.position = m_newPosition.position;
    }
}
