using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI Elements")]
    public UnityEngine.UI.Image loadingFill;
    public TextMeshProUGUI loadingText;
    public CanvasGroup loadingCanvasGroup; // Add this for a foolproof fade fallback
    public Animator loadingAnimator;
    public StartCrystalBounce crystalBounce;

    [Tooltip("Assign the RectTransform of the circle/reveal image so it can shrink on outro.")]
    public RectTransform outroCircleRect;

    [Header("Animations")]
    public string introAnimation = "LoadingReveal";
    public string outroAnimation = "LoadingOutro";
    public float transitionTime = 1f;
    public float minimumLoadTime = 3f;

    private string sceneToLoad;
    private string callerScene;

    void Awake()
    {
        // Auto-add CanvasGroup if missing to support the fade fallback
        if (loadingCanvasGroup == null) loadingCanvasGroup = GetComponent<CanvasGroup>();
        if (loadingCanvasGroup == null) loadingCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Ensure it starts opaque
        loadingCanvasGroup.alpha = 1f;

        // Ensure target is set early
        if (string.IsNullOrEmpty(sceneToLoad)) sceneToLoad = SceneLoader.targetSceneForLoading;

        // NEW: Background Persistence Logic
        Debug.Log("[LoadingScene] Awake - Persistence Flag: " + SceneLoader.keepBackgroundPersistent);

        if (SceneLoader.keepBackgroundPersistent)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                // Disable all Cameras in the LoadingScene
                foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
                {
                    cam.enabled = false;
                }

                // Disable all AudioListeners in the LoadingScene
                foreach (AudioListener al in root.GetComponentsInChildren<AudioListener>(true))
                {
                    al.enabled = false;
                }

                // Disable all EventSystems in the LoadingScene
                foreach (UnityEngine.EventSystems.EventSystem es in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                {
                    es.enabled = false;
                }
            }
        }
    }

    void Start()
    {
        Debug.Log("[LoadingScene] Starting Start()...");
        
        // FORCING CANVAS ON TOP: Ensure loading screen is always above joysticks
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Force overlay mode
            canvas.sortingOrder = 32000;
            Debug.Log("[LoadingScene] Canvas set to Overlay with sorting order 32000");
        }

        // Save the caller scene as early as possible
        callerScene = SceneManager.GetActiveScene().name;
        Debug.Log("[LoadingScene] Caller scene detected: " + callerScene);

        // Get the target scene from SceneLoader
        sceneToLoad = SceneLoader.targetSceneForLoading;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            // If LoadingScene is being pre-loaded by MainLoading, it won't have a target yet
            Debug.Log("[LoadingScene] No target scene yet. This might be a pre-load.");
            return;
        }

        StartCoroutine(LoadProcess());
    }

    public void PrepareAndShow(string targetScene)
    {
        // This is called when we were already in memory (pre-loaded)
        sceneToLoad = targetScene;
        callerScene = SceneManager.GetActiveScene().name;
        Debug.Log("[LoadingScene] PrepareAndShow - Target: " + sceneToLoad + ", Caller: " + callerScene);
        
        // CRITICAL: Reset the alpha because it was set to 0 during the previous outro!
        if (loadingCanvasGroup != null) loadingCanvasGroup.alpha = 1f;

        // CRITICAL: Reset the circle to zero so the intro can expand it again
        if (outroCircleRect != null) outroCircleRect.sizeDelta = Vector2.zero;

        // CRITICAL: Re-enable animator so the intro animation can play
        if (loadingAnimator != null)
        {
            loadingAnimator.enabled = true;
            loadingAnimator.Rebind();
            loadingAnimator.Update(0f);
        }

        StopAllCoroutines();
        StartCoroutine(LoadProcess());
    }

    // REMOVED GUARDIAN TO PREVENT DAMAGE - ONLY LOGGING NOW

    IEnumerator LoadProcess()
    {
        Debug.Log("[LoadingScene] Starting LoadProcess...");

        // Reduce background loading impact to keep UI smooth during animation
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // 1. Start Crystal Bounce IMMEDIATELY
        if (crystalBounce != null)
        {
            Debug.Log("[LoadingScene] Starting Crystal Bounce early");
            crystalBounce.StartBounce();
        }

        // 1.5 INVESTIGATION: Check if we should be keeping the background
        Debug.Log("[LoadingScene] keepBackgroundPersistent flag is: " + SceneLoader.keepBackgroundPersistent);
        // 2. Expand Animation (Intro)
        if (loadingAnimator != null)
        {
            Debug.Log("[LoadingScene] Playing Intro: " + introAnimation);
            loadingAnimator.Play(introAnimation, 0, 0f);
            
            // CRITICAL: We wait for the FULL animation to finish before starting the heavy load
            // This ensures the "eat up" is 100% smooth without lag spikes.
            yield return new WaitForSeconds(transitionTime);
        }

        // Wait a frame to ensure target is set
        if (string.IsNullOrEmpty(sceneToLoad)) sceneToLoad = SceneLoader.targetSceneForLoading;

        // EXTREME LAG REDUCTION: Use Low priority for the entire loading process
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // UI OPTIMIZATION: Disable Raycast on non-interactive images
        RaycastOptimization uiOpt = GetComponent<RaycastOptimization>();
        if (uiOpt == null) uiOpt = gameObject.AddComponent<RaycastOptimization>();
        uiOpt.OptimizeHierarchy(gameObject);

        // PREVENT CAMERA CONFLICTS: Only disable cameras if we DON'T want the background visible
        if (!SceneLoader.keepBackgroundPersistent)
        {
            DisableCamerasInScene(callerScene);
            // PREVENT INPUT CONFLICTS: Disable old EventSystems
            DisableEventSystemsInScene(callerScene);
        }
        else
        {
            Debug.Log("[LoadingScene] KEEPING BACKGROUND: Skipping camera untagging.");
        }

        // 3. Load Target Scene in Background — but ONLY if it's not already in memory!
        //    (When exiting a minigame, the main world was kept alive, so it's already there.)
        Debug.Log("[LoadingScene] Checking if already loaded: " + sceneToLoad);
        Scene alreadyLoadedScene = SceneManager.GetSceneByName(sceneToLoad);
        bool sceneAlreadyInMemory = alreadyLoadedScene.IsValid() && alreadyLoadedScene.isLoaded;

        AsyncOperation operation = null;
        float startTime = Time.time;

        if (sceneAlreadyInMemory)
        {
            Debug.Log("[LoadingScene] Scene already in memory - skipping async load: " + sceneToLoad);
            // Still show the progress bar filling to 100% over minimumLoadTime for polish
            float displayedProgress = 0f;
            while ((Time.time - startTime) < minimumLoadTime)
            {
                float timeProgress = (Time.time - startTime) / minimumLoadTime;
                displayedProgress = Mathf.MoveTowards(displayedProgress, timeProgress, 1.5f * Time.deltaTime);
                if (loadingText != null)
                {
                    int percent = Mathf.RoundToInt(displayedProgress * 100f);
                    loadingText.text = "Loading Assets... " + percent + "%";
                    if (loadingFill != null) loadingFill.fillAmount = displayedProgress;
                }
                yield return null;
            }
            if (loadingText != null) loadingText.text = "Loading Assets... 100%";
            if (loadingFill != null) loadingFill.fillAmount = 1f;
        }
        else
        {
            Debug.Log("[LoadingScene] Starting Async Load for: " + sceneToLoad);
            operation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
            operation.allowSceneActivation = false;

            float displayedProgress = 0f;
            while (operation.progress < 0.9f || (Time.time - startTime) < minimumLoadTime)
            {
                float realProgress = operation.progress / 0.9f;
                float timeProgress = (Time.time - startTime) / minimumLoadTime;
                float targetProgress = Mathf.Min(realProgress, timeProgress);

                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, 1.5f * Time.deltaTime);

                if (loadingText != null)
                {
                    int percent = Mathf.RoundToInt(displayedProgress * 100f);
                    loadingText.text = "Loading Assets... " + percent + "%";
                    if (loadingFill != null) loadingFill.fillAmount = displayedProgress;
                }
                yield return null;
            }

            if (loadingText != null) loadingText.text = "Loading Assets... 100%";

            // Wait for visual bar to catch up smoothly
            while (displayedProgress < 0.99f)
            {
                displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, 2f * Time.deltaTime);
                if (loadingFill != null) loadingFill.fillAmount = displayedProgress;
                yield return null;
            }
        }

        Debug.Log("[LoadingScene] Loading complete, activating scene...");

        // 4. Unload the CALLER scene early ONLY if we are EXITING a minigame back to the main world.
        //    When ENTERING a minigame, we must NOT unload the main world (player needs to return there).
        //    Detection: if sceneToLoad matches PreviousScene, we are going BACK = exiting minigame.
        string previousScenePref = PlayerPrefs.GetString("PreviousScene", "");
        bool isExitingMinigame = (!string.IsNullOrEmpty(previousScenePref) && sceneToLoad == previousScenePref);

        if (isExitingMinigame &&
            !SceneLoader.keepBackgroundPersistent &&
            !string.IsNullOrEmpty(callerScene) &&
            callerScene != sceneToLoad &&
            callerScene != gameObject.scene.name)
        {
            Debug.Log("[LoadingScene] Exiting minigame - unloading caller BEFORE activation: " + callerScene);
            Scene s = SceneManager.GetSceneByName(callerScene);
            if (s.IsValid() && s.isLoaded)
            {
                var unloadOp = SceneManager.UnloadSceneAsync(s);
                while (!unloadOp.isDone) yield return null;
            }
        }

        // 5. Activate the new scene
        if (operation != null)
        {
            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;
        }

        // Multiple frames to let Awake/Start/Physics settle
        for (int i = 0; i < 10; i++) yield return null;

        Debug.Log("[LoadingScene] Scene activated, setting active...");

        Scene loadedScene = SceneManager.GetSceneByName(sceneToLoad);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
            Debug.Log("[LoadingScene] " + sceneToLoad + " is now active.");

            foreach (GameObject obj in loadedScene.GetRootGameObjects())
                obj.SetActive(true);

            // CRITICAL: If the scene was already in memory (we kept it alive during minigame),
            // restore all components that were disabled when we entered the minigame.
            if (sceneAlreadyInMemory)
            {
                Debug.Log("[LoadingScene] Restoring scene components after minigame exit: " + sceneToLoad);
                RestoreSceneComponents(loadedScene);

                // CRITICAL: Start() won't re-run since scene was never destroyed.
                // Manually notify DialogueManager to reset IsInDialogue and resume post-minigame dialogue.
                if (DialogueManager.Instance != null)
                {
                    Debug.Log("[LoadingScene] Notifying DialogueManager of return from minigame.");
                    DialogueManager.Instance.OnReturnFromMinigame();
                }
            }

            RefreshPlayerCameras(loadedScene);
        }

        // Let the new scene's first frame render (prevents the 1-frame flash)
        for (int i = 0; i < 5; i++) yield return null;

        // 6. OUTRO: shrink the circle back to zero
        //    CRITICAL: Disable the Animator first — it will fight any script-driven sizeDelta changes
        Debug.Log("[LoadingScene] Starting Outro (circle close)...");
        if (loadingAnimator != null)
        {
            loadingAnimator.enabled = false;
        }

        // Auto-detect the circle rect if not assigned
        if (outroCircleRect == null)
        {
            // Walk all RectTransforms in the loading scene root objects looking for the big one
            foreach (GameObject rootObj in gameObject.scene.GetRootGameObjects())
            {
                foreach (var rt in rootObj.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.sizeDelta.x > 800f)
                    {
                        outroCircleRect = rt;
                        break;
                    }
                }
                if (outroCircleRect != null) break;
            }
        }

        if (outroCircleRect != null)
        {
            // Ensure the circle is at full size before we start shrinking
            // (in case the animator left it at a weird value)
            outroCircleRect.sizeDelta = new Vector2(2500f, 2500f);

            Vector2 startSize = outroCircleRect.sizeDelta;
            Vector2 targetSize = Vector2.zero;
            float elapsed = 0f;
            while (elapsed < transitionTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionTime);
                outroCircleRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
                yield return null;
            }
            outroCircleRect.sizeDelta = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("[LoadingScene] outroCircleRect not found — using alpha fade fallback!");
            float fastTransition = transitionTime;
            float timer2 = 0f;
            while (timer2 < fastTransition)
            {
                timer2 += Time.unscaledDeltaTime;
                if (loadingCanvasGroup != null) loadingCanvasGroup.alpha = 1f - (timer2 / fastTransition);
                yield return null;
            }
        }

        // 7. Now hide our own camera/EventSystem (AFTER outro, so circle was visible against the new scene)
        DisableOwnRedundantObjects();

        // Give 1 extra frame before hiding everything
        yield return null;

        Debug.Log("[LoadingScene] Transition complete. Hiding loading scene.");
        if (loadingCanvasGroup != null) loadingCanvasGroup.alpha = 0f;

        foreach (GameObject obj in gameObject.scene.GetRootGameObjects())
            obj.SetActive(false);

        // Reset priority and flags
        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
        SceneLoader.ResetLoadingFlag();
        CleanupGlobalConflicts();
    }

    private void CleanupGlobalConflicts()
    {
        Debug.Log("[LoadingScene] Performing Global Conflict Cleanup...");
        
        Scene activeScene = SceneManager.GetActiveScene();

        // 1. Audio Listeners: Keep only the one in the active scene
        AudioListener[] allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var listener in allListeners)
        {
            if (listener.gameObject.scene != activeScene)
            {
                listener.enabled = false;
                Debug.Log($"[LoadingScene] Disabled redundant AudioListener in scene: {listener.gameObject.scene.name}");
            }
        }

        // 2. Event Systems: Keep only the one in the active scene
        UnityEngine.EventSystems.EventSystem[] allES = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var es in allES)
        {
            if (es.gameObject.scene != activeScene)
            {
                es.enabled = false;
                Debug.Log($"[LoadingScene] Disabled redundant EventSystem in scene: {es.gameObject.scene.name}");
            }
        }
    }

    // Instead of UNTAGGING cameras (permanent, destructive), just DISABLE them.
    // This is fully reversible — RestoreSceneComponents re-enables them.
    private void DisableCamerasInScene(string sceneName)
    {
        Scene s = SceneManager.GetSceneByName(sceneName);
        if (!s.IsValid() || !s.isLoaded) return;

        foreach (GameObject obj in s.GetRootGameObjects())
        {
            Camera[] cameras = obj.GetComponentsInChildren<Camera>(true);
            foreach (Camera cam in cameras)
            {
                Debug.Log("[LoadingScene] Disabling Camera (not untagging) in: " + sceneName);
                cam.enabled = false;
            }
        }
    }

    // Restores cameras, EventSystems, and AudioListeners that were disabled when entering a minigame.
    private void RestoreSceneComponents(Scene scene)
    {
        if (!scene.IsValid()) return;

        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            // Re-enable cameras
            Camera[] cameras = obj.GetComponentsInChildren<Camera>(true);
            foreach (Camera cam in cameras)
            {
                cam.enabled = true;
                Debug.Log("[LoadingScene] Restored Camera: " + cam.name);
            }

            // Re-enable EventSystems
            UnityEngine.EventSystems.EventSystem[] systems = obj.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
            foreach (var es in systems)
            {
                es.enabled = true;
                Debug.Log("[LoadingScene] Restored EventSystem: " + es.name);
            }

            // Re-enable AudioListeners
            AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>(true);
            foreach (var al in listeners)
            {
                al.enabled = true;
                Debug.Log("[LoadingScene] Restored AudioListener: " + al.name);
            }
        }
    }

    private void DisableEventSystemsInScene(string sceneName)
    {
        Scene s = SceneManager.GetSceneByName(sceneName);
        if (!s.IsValid() || !s.isLoaded) return;

        foreach (GameObject obj in s.GetRootGameObjects())
        {
            UnityEngine.EventSystems.EventSystem[] systems = obj.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
            foreach (var system in systems)
            {
                Debug.Log("[LoadingScene] Disabling EventSystem in: " + sceneName);
                system.enabled = false;
            }
        }
    }

    private void RefreshPlayerCameras(Scene targetScene)
    {
        foreach (GameObject obj in targetScene.GetRootGameObjects())
        {
            // Refresh ThirdPerson
            var tpControllers = obj.GetComponentsInChildren<StarterAssets.ThirdPersonController>(true);
            foreach (var controller in tpControllers)
            {
                Debug.Log("[LoadingScene] Refreshing camera for player in: " + targetScene.name);
                controller.RefreshCamera();
            }

            // Refresh FirstPerson
            var fpControllers = obj.GetComponentsInChildren<StarterAssets.FirstPersonController>(true);
            foreach (var controller in fpControllers)
            {
                Debug.Log("[LoadingScene] Refreshing camera for player in: " + targetScene.name);
                controller.RefreshCamera();
            }
        }
    }

    private void DisableOwnRedundantObjects()
    {
        // Disable own camera so we see the target scene's environment immediately
        Camera ownCam = GetComponentInChildren<Camera>();
        if (ownCam == null) ownCam = Camera.main;
        
        if (ownCam != null && ownCam.gameObject.scene == gameObject.scene)
        {
            if (SceneLoader.keepBackgroundPersistent)
            {
                // If we are keeping the background, make our camera "see through"
                ownCam.clearFlags = CameraClearFlags.Depth;
                Debug.Log("[LoadingScene] Configured loading camera to be transparent (Depth Only).");
            }
            else
            {
                Debug.Log("[LoadingScene] Disabling own loading camera.");
                ownCam.enabled = false;
            }
        }

        // Disable own EventSystem to resolve the "2 EventSystems" warning
        UnityEngine.EventSystems.EventSystem ownEv = GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
        if (ownEv != null)
        {
            Debug.Log("[LoadingScene] Disabling own loading EventSystem.");
            ownEv.enabled = false;
        }
    }
}
