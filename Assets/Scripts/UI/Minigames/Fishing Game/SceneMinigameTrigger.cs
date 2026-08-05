using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMinigameTrigger : MonoBehaviour
{
    [Tooltip("The exact name of the minigame scene to load (e.g., 'FishingGameScene')")]
    public string minigameSceneName = "FishingGameScene";

    [Header("Fishing Game Configs (Optional)")]
    [Tooltip("The language for the fishing game (e.g., 'cebuano' or 'ilokano')")]
    public string targetLanguage = "cebuano";

    [Tooltip("The category filter from LuminangPhrases.json (e.g., 'Greetings')")]
    public string categoryFilter = "Greetings";

    [Tooltip("If true, uses the beautiful loading screen transition!")]
    public bool useLoadingScreen = true;

    public void StartMinigameScene()
    {
        StartCoroutine(MinigameTransitionRoutine());
    }

    private System.Collections.IEnumerator MinigameTransitionRoutine()
    {
        // 1. Set the config for the fishing game (other minigames might ignore this)
        FishingGameConfig.TargetLanguage = targetLanguage;
        FishingGameConfig.CategoryFilter = categoryFilter;

        // 2. Tell the minigame where to return to when it finishes
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        // Optional: Smoothly fade out the current scene before the chunky load happens
        if (SceneFader.Instance != null)
        {
            yield return SceneFader.Instance.FadeOutCoroutine();
        }

        // 3. Load the minigame scene
        if (useLoadingScreen)
        {
            SceneLoader.ResetLoadingFlag();
            SceneLoader.targetSceneForLoading = minigameSceneName;
            SceneLoader.keepBackgroundPersistent = false; // Hide the current scene while loading
            SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
        }
        else
        {
            SceneManager.LoadScene(minigameSceneName);
        }
    }
}
