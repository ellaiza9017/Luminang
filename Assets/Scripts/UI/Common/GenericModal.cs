using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class GenericModal : MonoBehaviour
{
    public static GenericModal Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject modalPanel;
    public Image backgroundImage;
    public TextMeshProUGUI messageText;
    public Image regionIconImage;
    public TMP_InputField inputField;

    [Header("Buttons")]
    public Button confirmButton;
    public Image confirmButtonImage;

    public Button cancelButton;
    public Image cancelButtonImage;

    [Header("Animation Settings")]
    public float animationDuration = 0.25f;
    private CanvasGroup canvasGroup;
    private Coroutine currentAnim;

    [Header("Sprites")]
    public Sprite panelSprite;
    public Sprite okayButtonSprite;
    public Sprite yesButtonSprite;
    public Sprite noButtonSprite;

    private Action onConfirmAction;
    private Action onCancelAction;

    private void Awake()
    {
        // Always take over as the active instance. Never destroy itself.
        Instance = this;

        // Get or add CanvasGroup for fading
        canvasGroup = modalPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = modalPanel.AddComponent<CanvasGroup>();

        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
            modalPanel.transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0;
        }

        if (inputField != null) inputField.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows a modal with a single button (Alert style)
    /// </summary>
    public void ShowAlert(string message, string buttonText = "Okay", Action onConfirm = null)
    {
        if (inputField != null) inputField.gameObject.SetActive(false);
        SetupModal(message, buttonText, onConfirm, null, null);
    }

    /// <summary>
    /// Shows a modal with two buttons (Confirmation style)
    /// </summary>
    public void ShowConfirm(string message, string confirmText, Action onConfirm, string cancelText, Action onCancel = null, Sprite thumbnail = null)
    {
        if (inputField != null) inputField.gameObject.SetActive(false);
        SetupModal(message, confirmText, onConfirm, cancelText, onCancel, thumbnail);
    }

    /// <summary>
    /// Shows a modal with an input field
    /// </summary>
    public void ShowInput(string message, string confirmText, Action<string> onConfirm, string cancelText, Action onCancel = null)
    {
        if (inputField == null)
        {
            Debug.LogError("[GenericModal] No InputField assigned to GenericModal!");
            return;
        }

        inputField.gameObject.SetActive(true);
        inputField.text = ""; // Clear previous text
        
        // Wrap the confirm action to pass the input text
        Action wrappedConfirm = () => {
            onConfirm?.Invoke(inputField.text);
        };

        SetupModal(message, confirmText, wrappedConfirm, cancelText, onCancel, null);
    }

    private void SetupModal(string message, string confirmText, Action onConfirm, string cancelText, Action onCancel, Sprite thumbnail = null)
    {
        if (modalPanel == null) return;

        messageText.text = message;
        onConfirmAction = onConfirm;
        onCancelAction = onCancel;

        // Handle Thumbnail
        if (regionIconImage != null)
        {
            if (thumbnail != null)
            {
                regionIconImage.gameObject.SetActive(true);
                regionIconImage.sprite = thumbnail;
            }
            else
            {
                regionIconImage.gameObject.SetActive(false);
            }
        }

        // Reset listeners
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClick);

        // Configure Buttons based on input
        if (string.IsNullOrEmpty(cancelText))
        {
            // Simple Alert mode
            cancelButton.gameObject.SetActive(false);
            confirmButtonImage.sprite = okayButtonSprite;
        }
        else
        {
            // Dual button mode
            cancelButton.gameObject.SetActive(true);
            
            confirmButtonImage.sprite = yesButtonSprite;
            cancelButtonImage.sprite = noButtonSprite;

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClick);
        }

        // Force a layout rebuild to center buttons automatically
        LayoutRebuilder.ForceRebuildLayoutImmediate(confirmButton.transform.parent as RectTransform);

        // ACTIVATE the object so the Coroutine can run!
        gameObject.SetActive(true);
        modalPanel.SetActive(true);

        // Start Animation
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateIn());
    }

    private IEnumerator AnimateIn()
    {
        modalPanel.SetActive(true);
        float elapsed = 0;
        Vector3 targetScale = Vector3.one;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // "Overshoot" / Bounce formula
            float bounce = Mathf.Sin(t * Mathf.PI * 1.2f) * (1.1f - t) + t;
            
            modalPanel.transform.localScale = Vector3.one * bounce;
            canvasGroup.alpha = t;
            yield return null;
        }

        modalPanel.transform.localScale = targetScale;
        canvasGroup.alpha = 1;
        currentAnim = null;
    }

    private IEnumerator AnimateOut(Action onComplete)
    {
        float elapsed = 0;
        while (elapsed < animationDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = 1 - (elapsed / (animationDuration * 0.5f));
            modalPanel.transform.localScale = Vector3.one * t;
            canvasGroup.alpha = t;
            yield return null;
        }

        modalPanel.SetActive(false);
        gameObject.SetActive(false); // Disable self when totally done
        currentAnim = null;
        onComplete?.Invoke();
    }

    private void OnConfirmClick()
    {
        StartCoroutine(AnimateOut(() => {
            onConfirmAction?.Invoke();
        }));
    }

    private void OnCancelClick()
    {
        StartCoroutine(AnimateOut(() => {
            onCancelAction?.Invoke();
        }));
    }

    public void Hide()
    {
        if (modalPanel != null && modalPanel.activeSelf)
        {
            StartCoroutine(AnimateOut(null));
        }
    }
}
