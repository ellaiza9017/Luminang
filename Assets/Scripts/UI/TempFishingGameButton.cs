using UnityEngine;
using UnityEngine.SceneManagement;

public class TempFishingGameButton : MonoBehaviour
{
    public void LoadFishingGame()
    {
        Debug.Log("[TempFishingGameButton] Loading FishingGameScene...");
        
        // Save the current scene so the minigame knows where to return to
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        
        // Load the minigame
        SceneManager.LoadScene("FishingGameScene");
    }
}
