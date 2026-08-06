using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static string previousScene;
    
    // The target scene that LoadingScene will eventually load
    public static string targetSceneForLoading;
    public static bool keepBackgroundPersistent = false; // NEW: Set this to true to keep the previous scene visible
    private static bool isSceneLoading = false; // Prevents double-triggering the loading screen

    [Header("Transition Settings")]
    public float transitionDelay = 0.4f;
    
    [Header("Loading Screen Setup")]
    public bool useLoadingScreenForGameScene = true;
    public string loadingSceneName = "LoadingScene";
    private static int _lastLoadFrame = -1;

    public void LoadScene(string sceneName)
    {
        if (Time.frameCount == _lastLoadFrame)
        {
            Debug.Log("[SceneLoader] LoadScene ignored - duplicate call in same frame.");
            return;
        }
        _lastLoadFrame = Time.frameCount;

        if (isSceneLoading)
        {
            Debug.Log("[SceneLoader] LoadScene ignored - already loading.");
            return;
        }

        Debug.Log("[SceneLoader] LoadScene called for: " + sceneName);
        isSceneLoading = true;
        
        string currentScene = SceneManager.GetActiveScene().name;

        // NEW RULE: Only use the loading screen if we are leaving the Main Menu 
        // AND going to a "heavy" scene (Game, Map, Prologue, or Character Creation).
        bool isHeavyScene = (sceneName == "Calle_Crisologo" || sceneName == "Magellan's_Cross" || 
                             sceneName == "MapSelectionScene" || sceneName == "PrologueScene" || 
                             sceneName == "CreateCharacterScene");
        
        bool shouldShowLoadingScreen = (currentScene == "MainMenuScene" && isHeavyScene);

        if (shouldShowLoadingScreen && useLoadingScreenForGameScene)
        {
            Debug.Log("[SceneLoader] Using loading screen for transition from Main Menu to: " + sceneName);
            targetSceneForLoading = sceneName;
            keepBackgroundPersistent = false; // Reset persistence for normal loads
            
            if (IsSceneLoaded(loadingSceneName))
            {
                ActivateScene(loadingSceneName);
            }
            else
            {
                SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Additive);
            }
        }
        else
        {
            Debug.Log("[SceneLoader] Normal (Direct) loading for: " + sceneName);
            StartCoroutine(LoadSceneWithFade(sceneName));
        }
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // CHECK: Are these "Sibling" scenes with the same background?
        bool isCurrentSibling = (currentScene == "MainMenuScene" || currentScene == "OptionScene" || currentScene == "AboutScene");
        bool isTargetSibling = (sceneName == "MainMenuScene" || sceneName == "OptionScene" || sceneName == "AboutScene");
        bool isSiblingTransition = isCurrentSibling && isTargetSibling;

        // 1. Only use the Visual Fade if it's NOT a sibling transition
        if (!isSiblingTransition && SceneFader.Instance != null)
        {
            yield return SceneFader.Instance.FadeOutCoroutine();
        }
        else
        {
            // For siblings, we do a tiny "pause" to let the UI settle, but NO visual flash
            yield return new WaitForSeconds(0.05f);
        }

        // 2. Log previous scene
        if (SceneManager.GetActiveScene().name != loadingSceneName)
        {
            previousScene = SceneManager.GetActiveScene().name;
        }

        // 3. SMART LOADING: Check if scene is already pre-loaded
        isSceneLoading = false; 
        Scene preloadedScene = SceneManager.GetSceneByName(sceneName);

        if (preloadedScene.IsValid() && preloadedScene.isLoaded)
        {
            Debug.Log("[SceneLoader] Scene " + sceneName + " is already pre-loaded! Activating instantly...");
            
            // Turn on the root objects (MainLoading deactivates them)
            foreach (GameObject obj in preloadedScene.GetRootGameObjects())
            {
                obj.SetActive(true);
            }
            
            SceneManager.SetActiveScene(preloadedScene);
            
            // Unload the previous scene manually
            string oldScene = previousScene;
            if (!string.IsNullOrEmpty(oldScene) && oldScene != sceneName)
            {
                SceneManager.UnloadSceneAsync(oldScene);
            }
        }
        else
        {
            Debug.Log("[SceneLoader] Scene " + sceneName + " not pre-loaded. Loading now...");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
        }
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.name == sceneName) return true;
        }
        return false;
    }

    private void ActivateScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
        {
            bool triggered = false;
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                obj.SetActive(true);
                
                if (triggered) continue;

                // Try to find the controller and trigger it ONLY ONCE
                var controller = obj.GetComponentInChildren<LoadingSceneController>();
                if (controller != null)
                {
                    controller.PrepareAndShow(targetSceneForLoading);
                    triggered = true;
                    continue;
                }
                
                var mainLoading = obj.GetComponentInChildren<MainLoading>();
                if (mainLoading != null)
                {
                    mainLoading.PrepareAndShow(targetSceneForLoading);
                    triggered = true;
                    continue;
                }
            }
        }
    }

    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(transitionDelay);
        if (SceneManager.GetActiveScene().name != loadingSceneName)
        {
            previousScene = SceneManager.GetActiveScene().name;
        }
        isSceneLoading = false; // RESET THE FLAG
        SceneManager.LoadScene(sceneName);
    }

    public void GoBack()
    {
        if (!string.IsNullOrEmpty(previousScene))
        {
            StartCoroutine(GoBackWithDelay());
        }
    }

    private IEnumerator GoBackWithDelay()
    {
        yield return new WaitForSeconds(transitionDelay);
        isSceneLoading = false; // RESET THE FLAG
        SceneManager.LoadScene(previousScene);
    }

    public static void ResetLoadingFlag()
    {
        isSceneLoading = false;
        keepBackgroundPersistent = false; // Reset for the next load
        Debug.Log("[SceneLoader] Loading flag and persistence reset.");
    }
}