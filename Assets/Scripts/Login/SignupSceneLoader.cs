using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToSignupScene : MonoBehaviour
{
    public void LoadSignupScene()
    {
        SceneManager.LoadScene("SignupScene");
    }
}