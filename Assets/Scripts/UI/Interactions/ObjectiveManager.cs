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
        
        if (PlayerPrefs.GetInt("RestorePlayerPos", 0) == 1)
        {
            float x = PlayerPrefs.GetFloat("PlayerPosX");
            float y = PlayerPrefs.GetFloat("PlayerPosY");
            float z = PlayerPrefs.GetFloat("PlayerPosZ");
            float rotY = PlayerPrefs.GetFloat("PlayerRotY");

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = new Vector3(x, y, z);
                player.transform.rotation = Quaternion.Euler(0, rotY, 0);

                if (cc != null) cc.enabled = true;
            }
            PlayerPrefs.SetInt("RestorePlayerPos", 0);
            PlayerPrefs.Save();
        }

        // Broadcast the initial objective so all Indicators sync up
        OnObjectiveChanged?.Invoke(CurrentObjective);
        
        UpdateVisibility();
    }

    public void SetObjective(string newObjective)
    {
        _isCounterActive = false; // Disable any active counter when a new static objective is set
        UpdateObjectiveInternal(newObjective);
    }

    private void UpdateObjectiveInternal(string newObjective)
    {
        string oldObjective = CurrentObjective;
        string cleanObjective = newObjective != null ? newObjective.Trim() : "";

        // Strip "Objective: " if it was passed in so we don't double up
        if (cleanObjective.StartsWith("Objective:", System.StringComparison.OrdinalIgnoreCase))
        {
            cleanObjective = cleanObjective.Substring("Objective:".Length).Trim();
        }

        if (cleanObjective == oldObjective) return;

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
