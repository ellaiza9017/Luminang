using UnityEngine;
using TMPro;
using System.Collections;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }
    public static System.Action<string> OnObjectiveChanged;

    [Header("UI References")]
    [Tooltip("The text component that will be updated and animated.")]
    public TextMeshProUGUI objectiveText;

    [Header("Animation Settings")]
    public float fadeDuration = 0.4f;
    [Tooltip("How much of its own size it slides (1.0 = full width/height)")]
    public float slideFactor = 0.5f;
    [Tooltip("Check this if the panel should slide left/right instead of up/down")]
    public bool slideHorizontal = true;

    public string CurrentObjective { get; private set; } = "";

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Vector2 _originalAnchoredPos;
    private Coroutine _animCoroutine;
    private bool _isShowing = true; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (objectiveText != null)
        {
            _rectTransform = objectiveText.GetComponent<RectTransform>();
            _canvasGroup = objectiveText.GetComponent<CanvasGroup>();
            
            if (_canvasGroup == null) _canvasGroup = objectiveText.gameObject.AddComponent<CanvasGroup>();

            if (_rectTransform != null)
            {
                _originalAnchoredPos = _rectTransform.anchoredPosition;
            }

            // Always start hidden so we can animate in
            _canvasGroup.alpha = 0f;
            _isShowing = false;
            
            // Snap to the hidden position immediately
            float size = slideHorizontal ? _rectTransform.rect.width : _rectTransform.rect.height;
            float pixelOffset = size * slideFactor;

            Vector2 hiddenPos = _originalAnchoredPos;
            if (slideHorizontal) hiddenPos.x -= pixelOffset;
            else hiddenPos.y += pixelOffset;
            _rectTransform.anchoredPosition = hiddenPos;

            // Grab initial text and strip "Objective: " if it exists to keep the ID clean
            string savedObj = PlayerPrefs.GetString("CurrentObjective", "");
            if (!string.IsNullOrEmpty(savedObj))
            {
                CurrentObjective = savedObj;
                objectiveText.text = "Objective: " + CurrentObjective;
            }
            else if (!string.IsNullOrEmpty(objectiveText.text))
            {
                string raw = objectiveText.text.Trim();
                if (raw.StartsWith("Objective:", System.StringComparison.OrdinalIgnoreCase))
                {
                    CurrentObjective = raw.Substring("Objective:".Length).Trim();
                }
                else
                {
                    CurrentObjective = raw;
                }
            }
            
            objectiveText.gameObject.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        // Wait a small moment for the scene to settle, then slide in
        yield return new WaitForSeconds(0.1f);
        
        // (Removed RestorePlayerPos logic since Magellan persists in the background)

        // Step 1: Push any locally-cached progress that may have failed to reach Supabase
        // This prevents players getting rolled back after a crash or network failure
        if (UserProfileManager.Instance != null)
        {
            var syncTask = UserProfileManager.Instance.SyncLocalObjectivesWithCloud();
            yield return new UnityEngine.WaitUntil(() => syncTask.IsCompleted);
            if (syncTask.IsFaulted)
                Debug.LogWarning("[ObjectiveManager] SyncLocalObjectivesWithCloud failed: " + syncTask.Exception?.InnerException?.Message);
        }

        // Step 2: Automatically fetch the correct objective from Supabase!
        SyncObjectiveWithDatabase();

        // Broadcast the initial objective so all Indicators sync up
        OnObjectiveChanged?.Invoke(CurrentObjective);
        
        UpdateVisibility();
    }

    // Added for UnityEvent Inspector support (which only supports 1 parameter)
    public void SetObjective(string newObjective)
    {
        SetObjective(newObjective, true);
    }

    public void SetObjective(string newObjective, bool autoSaveOld = true)
    {
        // Intercept Counter Objectives formatted as "Prefix ; Target ; Completion"
        if (newObjective != null && newObjective.Contains(";") && newObjective.Split(';').Length >= 2)
        {
            SetCounterObjective(newObjective);
            return;
        }

        _isCounterActive = false; // Disable any active counter when a new static objective is set
        UpdateObjectiveInternal(newObjective, autoSaveOld);
    }

    private void UpdateObjectiveInternal(string newObjective, bool autoSaveOld = true)
    {
        string oldObjective = CurrentObjective;
        string cleanObjective = newObjective != null ? newObjective.Trim() : "";

        // Strip "Objective: " if it was passed in so we don't double up
        if (cleanObjective.StartsWith("Objective:", System.StringComparison.OrdinalIgnoreCase))
        {
            cleanObjective = cleanObjective.Substring("Objective:".Length).Trim();
        }

        Debug.Log($"<color=cyan>[ObjectiveManager] UpdateObjectiveInternal | old='{oldObjective}' | new='{cleanObjective}' | autoSaveOld={autoSaveOld}</color>");

        if (cleanObjective == oldObjective)
        {
            Debug.Log($"<color=yellow>[ObjectiveManager] SKIPPED — new objective is same as old. No save.</color>");
            return;
        }

        // If we are moving to a new objective, save the old one AND any skipped ones to the database.
        // We collect everything that is uncompleted up to (and including) the old objective itself.
        if (autoSaveOld && !string.IsNullOrEmpty(oldObjective))
        {
            string oldId = GetObjectiveIdFromText(oldObjective);
            string language = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");

            Debug.Log($"<color=cyan>[ObjectiveManager] autoSaveOld=true | oldId='{oldId}' | language='{language}'</color>");

            if (!string.IsNullOrEmpty(oldId))
            {
                // Get all uncompleted objectives before the NEW one (catches skipped entries)
                string newId = string.IsNullOrEmpty(cleanObjective) ? null : GetObjectiveIdFromText(cleanObjective);
                Debug.Log($"<color=cyan>[ObjectiveManager] newId='{newId}'</color>");

                System.Collections.Generic.List<string> missedIds =
                    !string.IsNullOrEmpty(newId)
                    ? GetAllUncompletedObjectivesBefore(newId, language)
                    : new System.Collections.Generic.List<string>();

                // Also include the old objective itself — this is what was missing before!
                if (!missedIds.Contains(oldId)) missedIds.Add(oldId);

                Debug.Log($"<color=cyan>[ObjectiveManager] Saving IDs: [{string.Join(", ", missedIds)}]</color>");
                BulkSaveMissedObjectivesAsync(missedIds, language);
            }
            else
            {
                Debug.LogWarning($"<color=orange>[ObjectiveManager] Could not find ID for old objective '{oldObjective}' in JSON — SAVE SKIPPED!</color>");
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[ObjectiveManager] Save skipped — autoSaveOld={autoSaveOld}, oldObjective='{oldObjective}'</color>");
        }

        CurrentObjective = cleanObjective;
        PlayerPrefs.SetString("CurrentObjective", cleanObjective);
        PlayerPrefs.Save();

        if (objectiveText != null) 
        {
            objectiveText.text = string.IsNullOrEmpty(cleanObjective) ? "" : "Objective: " + cleanObjective;
        }
        
        // Force an instant event trigger so Indicators hide/show immediately
        OnObjectiveChanged?.Invoke(cleanObjective);
        
        UpdateVisibility();
    }

    [System.Serializable]
    private class ObjectiveItemData { public string id; public string objective; }
    [System.Serializable]
    private class ObjectiveCategoryData { public string category; public ObjectiveItemData[] items; }
    [System.Serializable]
    private class ObjectivesRootData { public ObjectiveCategoryData[] objectives; }

    /// <summary>
    /// Fetches the JSON file for the active language, checks the user's completed objectives
    /// in the database, and sets CurrentObjective to the first uncompleted one.
    /// </summary>
    public void SyncObjectiveWithDatabase()
    {
        if (UserProfileManager.Instance == null || UserProfileManager.Instance.CurrentProfile == null) return;
        
        string activeLanguage = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        bool isCebuano = activeLanguage.Equals("Cebuano", System.StringComparison.OrdinalIgnoreCase);
        string jsonFileName = isCebuano ? "Cebuano Objectives" : "Ilokano Objectives";
        
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
        if (jsonAsset == null)
        {
            Debug.LogError($"[ObjectiveManager] Could not find {jsonFileName} in Resources!");
            return;
        }

        ObjectivesRootData rootData = JsonUtility.FromJson<ObjectivesRootData>(jsonAsset.text);
        if (rootData == null || rootData.objectives == null) return;

        var profile = UserProfileManager.Instance.CurrentProfile;
        var completedList = isCebuano ? profile.CompletedObjectivesCebuano : profile.CompletedObjectivesIlokano;
        if (completedList == null) completedList = new System.Collections.Generic.List<string>();

        string activeQuest = PlayerPrefs.GetString("ActiveQuest", "");
        
        // --- 1. REPLAY MODE CHECK ---
        // If the user clicked a specific category in the menu (ActiveQuest)
        if (!string.IsNullOrEmpty(activeQuest))
        {
            foreach (var category in rootData.objectives)
            {
                if (category.category.Equals(activeQuest, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check if this category is completely finished
                    bool isCategoryCompleted = true;
                    foreach (var item in category.items)
                    {
                        if (!completedList.Contains(item.id))
                        {
                            isCategoryCompleted = false;
                            break;
                        }
                    }

                    // If they already finished this category, they are replaying it!
                    if (isCategoryCompleted && category.items.Length > 0)
                    {
                        Debug.Log($"[ObjectiveManager] Replay Mode: Starting at first objective of '{activeQuest}'");
                        SetObjective(category.items[0].objective, false);
                        // Clear ActiveQuest so a game restart returns them to normal progression
                        PlayerPrefs.DeleteKey("ActiveQuest");
                        return;
                    }
                }
            }
        }

        // --- 2. NORMAL PROGRESSION ---
        // Find the first objective ID that is NOT in the completed list
        foreach (var category in rootData.objectives)
        {
            foreach (var item in category.items)
            {
                if (!completedList.Contains(item.id))
                {
                    Debug.Log($"[ObjectiveManager] Database Sync: Found current objective -> {item.id}: {item.objective}");
                    SetObjective(item.objective, false); // DO NOT auto-save whatever was randomly in PlayerPrefs
                    return;
                }
            }
        }
        
        Debug.Log("[ObjectiveManager] Database Sync: All objectives completed!");
    }

    /// <summary>
    /// Calculates the overall objective progress percentage (0.0 to 1.0) based on ALL languages combined.
    /// </summary>
    public float GetOverallProgress()
    {
        if (UserProfileManager.Instance == null || UserProfileManager.Instance.CurrentProfile == null) return 0f;
        var profile = UserProfileManager.Instance.CurrentProfile;

        int totalCount = 0;
        int completedCount = 0;

        // 1. Cebuano
        TextAsset cebAsset = Resources.Load<TextAsset>("Cebuano Objectives");
        if (cebAsset != null)
        {
            ObjectivesRootData cebData = JsonUtility.FromJson<ObjectivesRootData>(cebAsset.text);
            var cebList = profile.CompletedObjectivesCebuano ?? new System.Collections.Generic.List<string>();
            if (cebData != null && cebData.objectives != null)
            {
                foreach (var category in cebData.objectives)
                {
                    if (category.items != null)
                    {
                        totalCount += category.items.Length;
                        foreach (var item in category.items)
                        {
                            if (cebList.Contains(item.id)) completedCount++;
                        }
                    }
                }
            }
        }

        // 2. Ilokano
        TextAsset iloAsset = Resources.Load<TextAsset>("Ilokano Objectives");
        if (iloAsset != null)
        {
            ObjectivesRootData iloData = JsonUtility.FromJson<ObjectivesRootData>(iloAsset.text);
            var iloList = profile.CompletedObjectivesIlokano ?? new System.Collections.Generic.List<string>();
            if (iloData != null && iloData.objectives != null)
            {
                foreach (var category in iloData.objectives)
                {
                    if (category.items != null)
                    {
                        totalCount += category.items.Length;
                        foreach (var item in category.items)
                        {
                            if (iloList.Contains(item.id)) completedCount++;
                        }
                    }
                }
            }
        }

        if (totalCount == 0) return 0f;
        return (float)completedCount / totalCount;
    }

    /// <summary>
    /// Checks if a given objective text (e.g. "Go meet Mar") has already been completed in the database.
    /// Used by InteractableNPC to determine if the player is in the Pre-Quest or Post-Quest phase.
    /// </summary>
    public bool IsObjectiveCompleted(string objectiveText)
    {
        if (string.IsNullOrEmpty(objectiveText)) return false;
        if (UserProfileManager.Instance == null || UserProfileManager.Instance.CurrentProfile == null) return false;
        
        string activeLanguage = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        bool isCebuano = activeLanguage.Equals("Cebuano", System.StringComparison.OrdinalIgnoreCase);
        string jsonFileName = isCebuano ? "Cebuano Objectives" : "Ilokano Objectives";
        
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
        if (jsonAsset == null) return false;

        ObjectivesRootData rootData = JsonUtility.FromJson<ObjectivesRootData>(jsonAsset.text);
        if (rootData == null || rootData.objectives == null) return false;

        var profile = UserProfileManager.Instance.CurrentProfile;
        var completedList = isCebuano ? profile.CompletedObjectivesCebuano : profile.CompletedObjectivesIlokano;
        if (completedList == null) return false;

        foreach (var category in rootData.objectives)
        {
            foreach (var item in category.items)
            {
                if (item.objective.StartsWith(objectiveText, System.StringComparison.OrdinalIgnoreCase))
                {
                    return completedList.Contains(item.id);
                }
            }
        }
        return false;
    }

    private System.Collections.Generic.List<string> GetAllUncompletedObjectivesBefore(string newId, string language)
    {
        System.Collections.Generic.List<string> missed = new System.Collections.Generic.List<string>();
        bool isCebuano = language.Equals("Cebuano", System.StringComparison.OrdinalIgnoreCase);
        string jsonFileName = isCebuano ? "Cebuano Objectives" : "Ilokano Objectives";
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
        if (jsonAsset == null) return missed;

        ObjectivesRootData rootData = JsonUtility.FromJson<ObjectivesRootData>(jsonAsset.text);
        if (rootData == null || rootData.objectives == null) return missed;

        var profile = UserProfileManager.Instance?.CurrentProfile;
        var completedList = isCebuano ? profile?.CompletedObjectivesCebuano : profile?.CompletedObjectivesIlokano;
        if (completedList == null) completedList = new System.Collections.Generic.List<string>();

        foreach (var category in rootData.objectives)
        {
            foreach (var item in category.items)
            {
                if (item.id == newId) return missed; // Stop when we reach the new objective
                
                if (!completedList.Contains(item.id))
                {
                    missed.Add(item.id);
                }
            }
        }
        return missed; // Return all uncompleted if newId wasn't found (fallback)
    }

    /// <summary>
    /// Looks up the exact objective text in the JSON file and returns its ID (e.g. "ceb_01").
    /// Returns null if not found.
    /// </summary>
    public string GetObjectiveIdFromText(string objectiveText)
    {
        if (string.IsNullOrEmpty(objectiveText)) return null;
        
        string activeLanguage = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        bool isCebuano = activeLanguage.Equals("Cebuano", System.StringComparison.OrdinalIgnoreCase);
        string jsonFileName = isCebuano ? "Cebuano Objectives" : "Ilokano Objectives";
        
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
        if (jsonAsset == null) return null;

        ObjectivesRootData rootData = JsonUtility.FromJson<ObjectivesRootData>(jsonAsset.text);
        if (rootData == null || rootData.objectives == null) return null;

        foreach (var category in rootData.objectives)
        {
            foreach (var item in category.items)
            {
                if (item.objective.Equals(objectiveText, System.StringComparison.OrdinalIgnoreCase))
                {
                    return item.id;
                }
            }
        }
        return null;
    }


    /// <summary>
    /// Call this when a player finishes an objective (NPC talk, minigame complete, etc.).
    /// Saves the objective ID to Supabase immediately, then advances the HUD to the next one.
    /// Usage: ObjectiveManager.Instance.CompleteObjective("ceb_01");
    /// </summary>
    public void CompleteObjective(string objectiveId)
    {
        string language = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");

        // Fire-and-forget: save in the background without blocking gameplay
        _ = SaveAndAdvanceAsync(objectiveId, language);
    }

    private async System.Threading.Tasks.Task SaveAndAdvanceAsync(string objectiveId, string language)
    {
        // 1. Auto-save to Supabase (updates local cache + pushes to DB)
        if (UserProfileManager.Instance != null)
            await UserProfileManager.Instance.MarkObjectiveCompleted(objectiveId, language);

        // 2. Advance the HUD to the next uncompleted objective
        SyncObjectiveWithDatabase();
    }

    private async System.Threading.Tasks.Task SaveToDatabaseOnlyAsync(string objectiveId, string language)
    {
        if (UserProfileManager.Instance != null)
            await UserProfileManager.Instance.MarkObjectiveCompleted(objectiveId, language);
    }

    private async void BulkSaveMissedObjectivesAsync(System.Collections.Generic.List<string> missedIds, string language)
    {
        // Single batched call — avoids Supabase race conditions from rapid sequential updates
        if (missedIds == null || missedIds.Count == 0) return;
        await UserProfileManager.Instance.BulkMarkObjectivesCompleted(missedIds, language);
        Debug.Log($"[ObjectiveManager] Auto-saved {missedIds.Count} skipped objectives in a single batch.");
    }

    [Header("Counter Logic")]
    public UnityEngine.Events.UnityEvent onCounterComplete;
    private string _counterPrefix;
    private string _completionText;
    private int _currentCount;
    private int _targetCount;
    private bool _isCounterActive;

    /// <summary>
    /// Starts a multi-step objective using a single string for UnityEvent compatibility.
    /// Format: "Prefix ; Target ; CompletionText"
    /// Example: "Find Organizers ; 6 ; Talk to Apo Lakay"
    /// </summary>
    public void SetCounterObjective(string data)
    {
        string[] parts = data.Split(';');
        if (parts.Length < 2) 
        {
            Debug.LogError("[ObjectiveManager] Invalid Counter Data! Format must be 'Prefix;Target;Completion'");
            return;
        }

        string prefix = parts[0].Trim();
        int target = 0;
        int.TryParse(parts[1].Trim(), out target);
        string completion = parts.Length > 2 ? parts[2].Trim() : "";

        _counterPrefix = prefix;
        _targetCount = target;
        _completionText = completion;
        _currentCount = 0;
        _isCounterActive = true;
        RefreshCounterUI();
    }

    /// <summary>
    /// Increases the counter by 1. If target reached, transitions to completion text.
    /// </summary>
    public void AddProgress()
    {
        if (!_isCounterActive) return;
        _currentCount++;
        
        if (_currentCount >= _targetCount)
        {
            _isCounterActive = false;
            if (!string.IsNullOrEmpty(_completionText))
            {
                UpdateObjectiveInternal(_completionText);
            }
            onCounterComplete?.Invoke();
        }
        else
        {
            RefreshCounterUI();
        }
    }

    private void RefreshCounterUI()
    {
        UpdateObjectiveInternal($"{_counterPrefix} ({_currentCount}/{_targetCount})");
    }

    public int GetCurrentCount() => _currentCount;
    public int GetTargetCount() => _targetCount;

    private void UpdateVisibility()
    {
        bool hasObjective = !string.IsNullOrEmpty(CurrentObjective);
        Debug.Log($"[ObjectiveManager] Visibility Check. Current: '{CurrentObjective}' (HasText: {hasObjective})");
        
        if (hasObjective) Show();
        else Hide();
    }

    public void Hide()
    {
        if (!_isShowing) return; 
        _isShowing = false;
        
        if (objectiveText == null) return;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimatePanel(false));
    }

    public void Show()
    {
        if (_isShowing) return; 
        _isShowing = true;

        if (objectiveText == null) return;
        
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimatePanel(true));
    }

    private IEnumerator AnimatePanel(bool show)
    {
        if (_canvasGroup == null || _rectTransform == null)
        {
            Debug.LogWarning("[ObjectiveManager] Missing CanvasGroup or RectTransform on objective text!");
            yield break;
        }

        if (show) 
        {
            objectiveText.gameObject.SetActive(true);
            Debug.Log("[ObjectiveManager] Animating Show...");
        }

        float startAlpha = _canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        float size = slideHorizontal ? _rectTransform.rect.width : _rectTransform.rect.height;
        float pixelOffset = size * slideFactor;

        Vector2 hiddenPos = _originalAnchoredPos;
        if (slideHorizontal) hiddenPos.x -= pixelOffset;
        else hiddenPos.y += pixelOffset;

        Vector2 startPos = _rectTransform.anchoredPosition;
        Vector2 targetPos = show ? _originalAnchoredPos : hiddenPos;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);

            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        _rectTransform.anchoredPosition = targetPos;

        if (!show) 
        {
            objectiveText.gameObject.SetActive(false);
            Debug.Log("[ObjectiveManager] Animating Hide complete.");
        }
    }

    // Update loop removed to prevent fighting with HUDManager watchdog
}
