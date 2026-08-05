using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenOptionsHelper : MonoBehaviour
{
    public void OpenFromMainMenu()
    {
        OptionsManager.PreviousSceneName = "MainMenuScene";
        
        SceneLoader loader = FindFirstObjectByType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadScene("OptionScene");
        }
        else if (TransitionOverlay.Instance != null)
        {
            TransitionOverlay.Instance.StartTransition("OptionScene");
        }
        else
        {
            SceneManager.LoadScene("OptionScene");
        }
    }

    public void OpenFromLanguageSelection()
    {
        OptionsManager.PreviousSceneName = "LanguageSelectionScene";
        
        SceneLoader loader = FindFirstObjectByType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadScene("OptionScene");
        }
        else if (TransitionOverlay.Instance != null)
        {
            TransitionOverlay.Instance.StartTransition("OptionScene");
        }
        else
        {
            SceneManager.LoadScene("OptionScene");
        }
    }
}
