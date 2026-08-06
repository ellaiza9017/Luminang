using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A placeholder manager for Minigames. 
/// Use this to block gameplay and show a UI overlay when a minigame is triggered.
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Dynamic Spawning")]
    [Tooltip("The container where minigame prefabs will be spawned (usually a Canvas).")]
    public Transform minigameContainer;
    
    [Header("Events")]
    public UnityEvent onMinigameComplete;
    
    private GameObject _currentInstance;
    public bool IsMinigameActive => _currentInstance != null && _currentInstance.activeInHierarchy;
    public string CurrentCategory { get; private set; }
    public int CurrentLanguageId { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Starts a minigame and assigns it a specific category and language.
    /// </summary>
    public void StartMinigameWithCategory(GameObject prefab, string category, int languageId)
    {
        CurrentCategory = category;
        CurrentLanguageId = languageId;
        StartMinigame(prefab);
    }

    /// <summary>
    /// Spawns and starts a specific minigame prefab.
    /// Also hooks up any close/continue/exit buttons on the spawned panel to call HideMinigame(),
    /// so that even panels wired to LessonManager.HideLesson() in the Inspector still resume dialogue.
    /// </summary>
    public void StartMinigame(GameObject prefab)
    {
        if (prefab == null) return;
        
        // If no category/language was set via the helper, fallback to defaults
        if (string.IsNullOrEmpty(CurrentCategory)) CurrentCategory = "";
        if (CurrentLanguageId <= 0) 
        {
            if (LessonManager.Instance != null) CurrentLanguageId = LessonManager.Instance.languageId;
            else CurrentLanguageId = 1; // Default to Ilokano
        }
        
        Debug.Log($"[MinigameManager] Starting Minigame: {prefab.name}");
        
        // Clean up any old instance just in case
        if (_currentInstance != null) Destroy(_currentInstance);

        _currentInstance = Instantiate(prefab, minigameContainer);
        _currentInstance.SetActive(true); // Ensure it's visible even if the prefab was disabled

        // Professional Fail-safe: Reset UI position and scale so it doesn't spawn 'into the void'
        RectTransform rt = _currentInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        // Clean up embedded EventSystems in the minigame prefab to avoid 'Multiple EventSystem' warnings
        var extraEventSystems = _currentInstance.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
        foreach (var es in extraEventSystems)
        {
            if (UnityEngine.EventSystems.EventSystem.current != es) 
                Destroy(es); // Only destroy the component! Destroying gameObject might kill the entire minigame prefab!
        }

        // Auto-wire any close/exit/continue buttons on the spawned panel to call HideMinigame().
        // This ensures dialogue always resumes when the player closes the panel, even if the
        // prefab's button is wired to LessonManager.HideLesson() instead of MinigameManager.
        HookCloseButtons(_currentInstance);
    }

    /// <summary>
    /// Finds buttons named CloseButton, ContinueButton, or ExitButton inside the spawned panel
    /// and adds HideMinigame as a listener, so closing the panel always resumes dialogue.
    /// </summary>
    private void HookCloseButtons(GameObject panel)
    {
        if (panel == null) return;
        var buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            string n = btn.gameObject.name;
            if (n.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Continue", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                btn.onClick.AddListener(HideMinigame);
            }
        }
    }

    /// <summary>
    /// Destroys the current minigame and restores the HUD.
    /// Called by the auto-hooked close/continue/exit buttons on the spawned panel,
    /// or directly by minigame scripts when they finish.
    /// </summary>
    public void HideMinigame()
    {
        // Guard: prevent double-calls (e.g. if both the hooked listener AND LessonManager call this)
        if (_currentInstance == null && DialogueManager.Instance != null && DialogueManager.Instance.PendingMinigameChoice == null)
        {
            return;
        }

        if (_currentInstance != null) 
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }
        
        Debug.Log("[MinigameManager] Minigame Finished.");
        onMinigameComplete?.Invoke();

        // Always notify DialogueManager to resume any paused dialogue.
        // DialogueManager.CompleteMinigame() is idempotent — it clears PendingMinigameChoice
        // on first call and safely warns (does nothing harmful) on subsequent calls.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CompleteMinigame();
        }
    }
}
