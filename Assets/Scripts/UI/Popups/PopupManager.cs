using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

[Serializable]
public struct PopupEntry
{
    [Tooltip("The name of the popup, e.g., 'welcome_level1'")]
    public string popupName;
    [Tooltip("The sprite to display for this popup.")]
    public Sprite popupSprite;
}

/// <summary>
/// Handles one-time achievement popups that can interrupt dialogue or gameplay.
/// Displays popups sequentially if multiple are triggered at once.
/// </summary>
public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The main panel holding the popup UI (should have a CanvasGroup for fading).")]
    public GameObject popupPanel;
    [Tooltip("The Image component where the popup sprite will be displayed.")]
    public Image popupImage;
    [Tooltip("The text prompting the user to click anywhere.")]
    public TextMeshProUGUI clickPromptText;
    [Tooltip("A full-screen invisible button to capture clicks and close the popup.")]
    public Button backgroundClickButton;
    [Tooltip("Optional CanvasGroup to fade the popup in/out smoothly.")]
    public CanvasGroup canvasGroup;

    [Header("Popup Data")]
    [Tooltip("List of all available popups. Map the string name to the Sprite here.")]
    public List<PopupEntry> popupDatabase = new List<PopupEntry>();

    [Header("Testing")]
    [Tooltip("If true, popups will ALWAYS show, ignoring the one-time limit. Turn this off for final build!")]
    public bool alwaysShowForTesting = false;

    [Tooltip("How long it takes for the popup to fade in and out.")]
    public float fadeDuration = 0.3f;

    private Queue<string> _popupQueue = new Queue<string>();
    private Action _onAllPopupsComplete;
    private bool _isDisplaying = false;
    private Coroutine _fadeCoroutine;

    private const string PREFS_PREFIX = "PopupShown_";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        if (backgroundClickButton != null)
        {
            backgroundClickButton.onClick.AddListener(OnBackgroundClicked);
        }
    }

    /// <summary>
    /// Queues one or more popups to be shown.
    /// E.g., ShowPopups("complete_level1,welcome_level1", callback)
    /// </summary>
    public void ShowPopups(string commaSeparatedNames, Action onComplete = null)
    {
        if (string.IsNullOrEmpty(commaSeparatedNames))
        {
            onComplete?.Invoke();
            return;
        }

        string[] names = commaSeparatedNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        
        bool addedAny = false;
        foreach (string name in names)
        {
            string trimmedName = name.Trim();
            // Only add if it hasn't been shown before (or if testing toggle is on)
            if (alwaysShowForTesting || !HasPopupBeenShown(trimmedName))
            {
                _popupQueue.Enqueue(trimmedName);
                addedAny = true;
            }
            else
            {
                Debug.Log($"[PopupManager] Popup '{trimmedName}' was already shown previously. Skipping.");
            }
        }

        if (addedAny)
        {
            // Store the callback. If we're already displaying, it will run when the whole queue finishes.
            if (_onAllPopupsComplete == null)
            {
                _onAllPopupsComplete = onComplete;
            }
            else
            {
                // Chain callbacks if needed
                _onAllPopupsComplete += onComplete;
            }

            if (!_isDisplaying)
            {
                ShowNextPopup();
            }
        }
        else
        {
            // Nothing to show, return immediately
            onComplete?.Invoke();
        }
    }

    private void ShowNextPopup()
    {
        if (_popupQueue.Count == 0)
        {
            FinishAllPopups();
            return;
        }

        string nextPopupName = _popupQueue.Dequeue();
        Sprite spriteToShow = GetSpriteForPopup(nextPopupName);

        if (spriteToShow == null)
        {
            Debug.LogWarning($"[PopupManager] Could not find sprite for popup '{nextPopupName}'. Skipping.");
            // Mark as shown anyway so we don't keep trying to show a missing popup
            MarkPopupAsShown(nextPopupName);
            ShowNextPopup();
            return;
        }

        _isDisplaying = true;
        popupImage.sprite = spriteToShow;
        
        if (clickPromptText != null)
        {
            clickPromptText.text = "Click anywhere to continue";
        }

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(1f, fadeDuration, () => {
            // Fade in complete
        }));

        MarkPopupAsShown(nextPopupName);
        Debug.Log($"[PopupManager] Displaying popup: {nextPopupName}");
    }

    private void OnBackgroundClicked()
    {
        if (!_isDisplaying) return;
        
        // Prevent double clicking while it's already fading out
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, () => {
            // Check if there's another one in the queue after this one finishes fading out
            ShowNextPopup();
        }));
    }

    private void FinishAllPopups()
    {
        _isDisplaying = false;
        if (popupPanel != null) popupPanel.SetActive(false);
        
        Action callback = _onAllPopupsComplete;
        _onAllPopupsComplete = null;
        callback?.Invoke();
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            if (popupPanel != null) popupPanel.SetActive(targetAlpha > 0.5f);
            onComplete?.Invoke();
            yield break;
        }

        if (targetAlpha > 0f)
        {
            popupPanel.SetActive(true);
            canvasGroup.interactable = true;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            popupPanel.SetActive(false);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        onComplete?.Invoke();
    }

    private Sprite GetSpriteForPopup(string popupName)
    {
        foreach (var entry in popupDatabase)
        {
            if (entry.popupName.Equals(popupName, StringComparison.OrdinalIgnoreCase))
            {
                return entry.popupSprite;
            }
        }
        return null;
    }

    private bool HasPopupBeenShown(string popupName)
    {
        return PlayerPrefs.GetInt(PREFS_PREFIX + popupName, 0) == 1;
    }

    private void MarkPopupAsShown(string popupName)
    {
        PlayerPrefs.SetInt(PREFS_PREFIX + popupName, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Debug method to reset all popup memory so they can be viewed again.
    /// </summary>
    [ContextMenu("Reset All Popups")]
    public void DebugResetAllPopups()
    {
        foreach (var entry in popupDatabase)
        {
            PlayerPrefs.DeleteKey(PREFS_PREFIX + entry.popupName);
        }
        PlayerPrefs.Save();
        Debug.Log("[PopupManager] All popup memory cleared. Popups will show again.");
    }
}
