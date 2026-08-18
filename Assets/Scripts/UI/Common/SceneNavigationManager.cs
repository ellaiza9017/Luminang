using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigationManager
{
    // This is the "Memory" of our navigator
    private static string lastSceneName = "LanguageSelectionScene"; // Default fallback

    /// <summary>
    /// INSTANTLY hides all Canvases and Lights in the current active scene.
    /// Call this right before triggering a persistent additive load (Shop, Customization)
    /// so the Pause Menu and world lighting don't bleed through the loading screen.
    /// </summary>
    public static void HideCurrentSceneImmediate()
    {
        Scene active = SceneManager.GetActiveScene();
        foreach (GameObject root in active.GetRootGameObjects())
        {
            // Instantly kill all Canvases (Pause Menu, HUD, etc.)
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas c in canvases) c.enabled = false;

            // Instantly kill all Lights so they don't bleed into the next scene
            Light[] lights = root.GetComponentsInChildren<Light>(true);
            foreach (Light l in lights) l.enabled = false;
        }
    }

    /// <summary>
    /// Loads the customization scene and remembers the current scene.
    /// Uses the game's existing Additive LoadingScene system.
    /// </summary>
    public static void LoadCustomization()
    {
        // 1. Remember where we are right now for the back button
        lastSceneName = SceneManager.GetActiveScene().name;
        Debug.Log("[SceneNavigator] Remembering scene for return: " + lastSceneName);

        // 2. Instantly hide the current scene's UI and lights BEFORE loading starts
        HideCurrentSceneImmediate();

        // 3. Reset the loading flag in your SceneLoader so it doesn't block us
        SceneLoader.ResetLoadingFlag();

        // 4. Set the target for your existing LoadingScene system
        SceneLoader.targetSceneForLoading = "CharacterCustomizationScene";
        SceneLoader.keepBackgroundPersistent = true;
        
        // 5. Load your LoadingScene ADDITIVELY (it will overlay on your current scene)
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
    }

    public static void LoadSTTTest()
    {
        // 1. Remember where we are (LanguageSelectionScene)
        lastSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("PreviousScene", "LanguageSelectionScene");
        Debug.Log("[SceneNavigator] Saving scene: " + lastSceneName);

        // 2. Load the test scene
        SceneManager.LoadScene("STT_TestScene");
    }

    /// <summary>
    /// Returns to the scene we were in before customization.
    /// Uses the smooth Navy/Purple fade.
    /// </summary>
    public static void ReturnToPreviousScene()
    {
        Debug.Log("[SceneNavigator] Returning to: " + lastSceneName);
        
        // If we have a TransitionOverlay in the scene, use it!
        if (TransitionOverlay.Instance != null)
        {
            TransitionOverlay.Instance.StartTransition(lastSceneName);
        }
        else
        {
            // Fallback if no overlay exists
            SceneManager.LoadScene(lastSceneName);
        }
    }
}
