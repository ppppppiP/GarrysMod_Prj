using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader22: MonoBehaviour
{
    public int lvlID;

    public void Load()
    {
        SceneManager.LoadScene(lvlID);
    }
}