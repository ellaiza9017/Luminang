using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToLoginScene : MonoBehaviour
{
    public void LoadLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }
}