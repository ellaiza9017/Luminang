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
        // 1. Set the config for the fishing game
        FishingGameConfig.TargetLanguage = targetLanguage;
        FishingGameConfig.CategoryFilter = categoryFilter;

        // Set config for SariSari Chismis game
        SariSariGameConfig.TargetLanguage = targetLanguage;
        SariSariGameConfig.TargetCategory = categoryFilter;

        // Set config for Memory Game (uses PlayerPrefs)
        PlayerPrefs.SetString("MemoryGameLanguage", targetLanguage);
        PlayerPrefs.SetString("MemoryGameCategory", categoryFilter);
        
        // Set config for Tumbang Preso
        TumbangPresoGameConfig.TargetLanguage = targetLanguage;
        TumbangPresoGameConfig.CategoryFilter = categoryFilter;
        
        // Set config for Reaction Cards (uses PlayerPrefs for category)
        PlayerPrefs.SetString("ReactionCardCategory", categoryFilter);
        
        // Set config for Tusok-Tusok (uses PlayerPrefs for category)
        PlayerPrefs.SetString("TusokTusokCategory", categoryFilter);
        
        // Normalize language to title case so comparisons like language == "Cebuano" work
        // correctly throughout the codebase. (targetLanguage may be lowercase "cebuano")
        string normalizedLanguage = string.IsNullOrEmpty(targetLanguage) ? "Cebuano"
            : char.ToUpper(targetLanguage[0]) + targetLanguage.Substring(1).ToLower();
        // Replace "Ilokano" alternate spellings
        if (normalizedLanguage.Equals("Ilocano", System.StringComparison.OrdinalIgnoreCase)) normalizedLanguage = "Ilokano";

        // Ensure global language is synced (for AssessmentManager and others)
        PlayerPrefs.SetString("SelectedLanguage", normalizedLanguage);
        
        PlayerPrefs.Save();

        // Disable main game joysticks and touchpads so they don't block minigame UI input
        DisableMainGameControls();

        // 2. We no longer save Player Position to PlayerPrefs because Magellan stays perfectly alive!

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
            SceneLoader.keepBackgroundPersistent = true; // KEEP Magellan alive!
            SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
        }
        else
        {
            SceneManager.LoadScene(minigameSceneName);
        }
    }

    /// <summary>
    /// Disables main game mobile touchpads, joysticks, and movement controls in background scenes 
    /// so they don't block clicks/taps during minigames!
    /// </summary>
    public static void DisableMainGameControls()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;

            string sceneName = s.name.ToLower();
            if (sceneName.Contains("minigame") || sceneName.Contains("tcg") || sceneName.Contains("reaction") || sceneName.Contains("tumbang")) continue;

            foreach (GameObject root in s.GetRootGameObjects())
            {
                // INSTANTLY disable Canvases so Pause Menu and HUD don't overlap the loading screen!
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas c in canvases)
                {
                    if (!c.gameObject.name.Contains("LoadingScreen") && !c.gameObject.name.Contains("MainLoading"))
                    {
                        c.enabled = false;
                    }
                }

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Contains("StarterAssetsInputs") || 
                        t.name.Contains("Touchpad") || 
                        t.name.Contains("Joystick") || 
                        t.name.Contains("Movement_Controls") || 
                        t.name.Contains("UI_Virtual_Touchpad"))
                    {
                        t.gameObject.SetActive(false);
                        UnityEngine.UI.Image img = t.GetComponent<UnityEngine.UI.Image>();
                        if (img != null) img.raycastTarget = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Re-enables main game controls when returning to the main world.
    /// </summary>
    public static void EnableMainGameControls()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;

            foreach (GameObject root in s.GetRootGameObjects())
            {
                // INSTANTLY re-enable Canvases that were hidden during the minigame
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas c in canvases)
                {
                    c.enabled = true;
                }

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Contains("StarterAssetsInputs") || 
                        t.name.Contains("Touchpad") || 
                        t.name.Contains("Joystick") || 
                        t.name.Contains("Movement_Controls") || 
                        t.name.Contains("UI_Virtual_Touchpad"))
                    {
                        t.gameObject.SetActive(true);
                        UnityEngine.UI.Image img = t.GetComponent<UnityEngine.UI.Image>();
                        if (img != null) img.raycastTarget = true;
                    }
                }
            }
        }

        // 3. Ensure BGM returns to the active scene's track!
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.RefreshBGMForActiveScene();
        }
    }
}
