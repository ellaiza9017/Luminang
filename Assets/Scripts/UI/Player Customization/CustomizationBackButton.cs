using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the Back button in CharacterCustomizationScene.
/// 
/// - If the scene was loaded ADDITIVELY (e.g. from Magellan's Cross via PlayerInfoPanel),
///   it unloads the scene additively (instant return), re-enables background EventSystems,
///   and blocks PlayerInfoPanel clicks for 3 seconds to prevent click-through.
///
/// - If the scene is the only one loaded (e.g. launched directly), it falls back to
///   SceneNavigationManager.ReturnToPreviousScene().
///
/// - Always shows an "unsaved changes" confirmation modal before leaving if changes exist.
/// </summary>
[RequireComponent(typeof(Button))]
public class CustomizationBackButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnBackClicked);
    }

    private void OnBackClicked()
    {
        CustomizationManager manager = FindFirstObjectByType<CustomizationManager>(FindObjectsInactive.Include);

        if (manager != null && manager.HasUnsavedChanges())
        {
            GenericModal modal = GenericModal.Instance;
            if (modal == null) modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);

            if (modal != null)
            {
                modal.ShowConfirm(
                    "You have some unsaved changes. Are you sure you want to leave?",
                    "Yes", DoGoBack,
                    "No"
                );
            }
            else
            {
                // No modal found — just go back anyway
                DoGoBack();
            }
        }
        else
        {
            DoGoBack();
        }
    }

    private void DoGoBack()
    {
        // Step 1: Notify outfit managers in background scenes to refresh
        var allOutfitManagers = FindObjectsByType<OutfitManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var om in allOutfitManagers)
        {
            if (om.gameObject.scene != gameObject.scene)
                om.LoadOutfit(UserProfileManager.Instance.GetEquippedOutfitData());
        }

        // Step 2: Refresh PlayerInfoPanel data (coins, portrait, etc.)
        var infoPanels = FindObjectsByType<PlayerInfoPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var panel in infoPanels)
            panel.UpdatePanelData();

        if (SceneManager.sceneCount > 1)
        {
            // Loaded additively — unload this scene and re-enable background systems safely
            var thisScene = gameObject.scene;

            // Spawn a persistent helper (survives the unload) to re-enable EventSystems
            // and block PlayerInfoPanel clicks for 3 seconds
            GameObject helper = new GameObject("CustomizationExitHelper");
            DontDestroyOnLoad(helper);
            var exitHelper = helper.AddComponent<CustomizationExitHelper>();
            exitHelper.StartCoroutine(exitHelper.ReenableAfterUnload(thisScene));

            SceneManager.UnloadSceneAsync(thisScene);
        }
        else
        {
            // Standalone — use the normal transition flow
            SceneNavigationManager.ReturnToPreviousScene();
        }
    }
}

/// <summary>
/// Helper that survives the scene unload to safely re-enable background EventSystems
/// and block PlayerInfoPanel interaction for 3 seconds.
/// Mirrors ShopExitHelper.
/// </summary>
public class CustomizationExitHelper : MonoBehaviour
{
    public System.Collections.IEnumerator ReenableAfterUnload(UnityEngine.SceneManagement.Scene unloadedScene)
    {
        // Immediately block PlayerInfoPanel so the Back button click can't bleed through
        foreach (var panel in FindObjectsByType<PlayerInfoPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (panel.gameObject.scene != unloadedScene)
                panel.TemporarilyDisableInteraction(3f);
        }

        // Wait a short moment, then re-enable background EventSystems and AudioListeners
        yield return new WaitForSeconds(0.25f);

        foreach (var es in FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (es.gameObject.scene != unloadedScene) es.enabled = true;
        }
        foreach (var al in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (al.gameObject.scene != unloadedScene) al.enabled = true;
        }

        // FIX: Re-enable Canvases and Lights that were hidden by HideCurrentSceneImmediate()
        // Without this, Magellan's UI and lighting never come back after returning.
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.gameObject.scene != unloadedScene) c.enabled = true;
        }
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.gameObject.scene != unloadedScene) l.enabled = true;
        }

        // Restore the BGM for whichever scene is now active (OnSceneLoaded doesn't fire on unload)
        if (BGMManager.Instance != null)
            BGMManager.Instance.RefreshBGMForActiveScene();

        Destroy(gameObject);
    }
}
