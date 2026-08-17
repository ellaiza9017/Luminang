using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MinigameReloader : MonoBehaviour
{
    public static void ReloadActiveMinigame()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        GameObject reloaderObj = new GameObject("MinigameReloader");
        DontDestroyOnLoad(reloaderObj);
        var reloader = reloaderObj.AddComponent<MinigameReloader>();
        
        reloader.StartCoroutine(reloader.ReloadRoutine(sceneName));
    }

    private IEnumerator ReloadRoutine(string sceneName)
    {
        // 1. Cache the CURRENT minigame scene so we know which one to destroy later
        Scene oldScene = SceneManager.GetActiveScene();

        // 2. Load a BRAND NEW instance of the minigame additively, in the background
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return loadOp;

        // 3. Find the newly loaded scene (it will have the same name, but a different handle than oldScene)
        Scene newScene = default;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.name == sceneName && s != oldScene)
            {
                newScene = s;
                break;
            }
        }

        // 4. Set the new scene as active so its EventSystem and lighting take priority
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
        }

        // 5. NOW that the new scene is fully rendered and active, safely destroy the old one!
        // This creates a 100% seamless transition with zero black screens and zero background flashes.
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldScene);
        yield return unloadOp;

        // Clean up the reloader
        Destroy(gameObject);
    }
}
