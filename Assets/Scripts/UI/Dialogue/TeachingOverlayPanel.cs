using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Full-screen teaching overlay shown during NPC word-teaching dialogues.
/// Displays a background image, prompt text, and a mic tap button (active/inactive).
/// Directly handles SpeechRecorder, GroqWhisperManager, and PhraseEvaluator (same as STTGameController).
/// </summary>
public class TeachingOverlayPanel : MonoBehaviour
{
    public static TeachingOverlayPanel Instance { get; private set; }

    [Header("Panel Root")]
    public CanvasGroup canvasGroup;

    [Header("Background")]
    public Image backgroundImage;
    public Sprite[] backgroundOptions;

    [Header("Prompt Text")]
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI tapToStopText;

    [Header("Text Outline")]
    [Tooltip("Width of the white outline drawn around both prompt texts (0 = none, 0.25 recommended).")]
    [Range(0f, 0.5f)]
    public float textOutlineWidth = 0.25f;
    [Tooltip("Color of the text outline.")]
    public Color textOutlineColor = Color.white;

    [Header("Mic Button")]
    public Button micButton;
    public Image micButtonImage;
    public Sprite micInactiveSprite;
    public Sprite micActiveSprite;

    [Header("Movement Controls")]
    public GameObject movementControls;

    [Header("Animation")]
    public float fadeDuration = 0.3f;

    // ── Private State ──────────────────────────────────────────────
    private bool _isRecording = false;
    private string _targetWord = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        ApplyTextOutlines();

        gameObject.SetActive(false);
    }

    private void ApplyTextOutlines()
    {
        if (promptText != null)
        {
            promptText.outlineWidth = textOutlineWidth;
            promptText.outlineColor = textOutlineColor;
        }
        if (tapToStopText != null)
        {
            tapToStopText.outlineWidth = textOutlineWidth;
            tapToStopText.outlineColor = textOutlineColor;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────

    public bool isManuallyShown = false;

    public void ShowFromEvent(string eventName)
    {
        isManuallyShown = true;

        string autoWord = "";
        if (DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null)
        {
            autoWord = DialogueManager.Instance.PendingSTTChoice.expectedSTTWord;
        }

        string[] parts = eventName.Split(':');
        string bgName = "";
        string manualWord = "";

        if (parts.Length == 3)
        {
            manualWord = parts[1].Trim();
            bgName = parts[2].Trim();
        }
        else if (parts.Length == 2)
        {
            bgName = parts[1].Trim();
        }
        else if (parts.Length == 1)
        {
            bgName = parts[0].Trim();
        }

        string finalWord = !string.IsNullOrEmpty(autoWord) ? autoWord : manualWord;
        Show(finalWord, bgName);
    }

    public void ShowCustomText(string text)
    {
        isManuallyShown = true;
        gameObject.SetActive(true);

        if (backgroundImage != null) backgroundImage.gameObject.SetActive(false); // Hide bird
        if (micButton != null) micButton.gameObject.SetActive(false); // Hide mic
        if (tapToStopText != null) tapToStopText.gameObject.SetActive(false);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = $"<color=#55FF55><b>{text}</b></color>";
        }

        if (canvasGroup != null && canvasGroup.alpha < 0.95f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }
    }

    public void ShowForPendingSTT(string backgroundName = "")
    {
        string word = "";
        if (DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null)
            word = DialogueManager.Instance.PendingSTTChoice.expectedSTTWord;
        Show(word, backgroundName);
    }

    public void Show(string word, string backgroundName = "")
    {
        _targetWord = word;
        _isRecording = false;

        EnsureSpeechEngineDependencies();

        // Match STT_TestScene behavior: always set region to Cebuano for Magellan scene lessons
        if (PhraseEvaluator.Instance != null)
            PhraseEvaluator.Instance.SetRegion(RegionMode.Cebuano);

        if (backgroundImage != null)
        {
            if (!string.IsNullOrEmpty(backgroundName))
            {
                Sprite found = FindBackground(backgroundName);
                if (found != null) 
                {
                    backgroundImage.gameObject.SetActive(true);
                    ChangeBackground(found);
                }
            }
            // If backgroundName is empty, DO NOT hide the background.
            // This allows the background to persist across multiple STT nodes!
        }

        ResetPromptText();

        // Apply white outline to prompt and tap-to-stop texts
        ApplyTextOutline(promptText);
        ApplyTextOutline(tapToStopText);

        if (tapToStopText != null)
            tapToStopText.gameObject.SetActive(false);

        SetMicState(false);

        // Check if device even has a microphone!
        bool hasMic = Microphone.devices.Length > 0;
        bool hasSttWord = !string.IsNullOrEmpty(_targetWord);

        if (micButton != null)
        {
            if (hasSttWord && hasMic)
            {
                micButton.gameObject.SetActive(true);
                micButton.interactable = true;
                micButton.onClick.RemoveAllListeners();
                micButton.onClick.AddListener(OnMicButtonTapped);
            }
            else
            {
                micButton.gameObject.SetActive(false);
            }
        }

        if (hasSttWord && !hasMic)
        {
            // Auto skip if no mic to prevent softlock
            if (promptText != null)
                promptText.text = "<color=#FF5555>No microphone detected!\nAuto-skipping check...</color>";
            StartCoroutine(AutoSkipNoMic());
        }

        HideMovementControls(true);

        gameObject.SetActive(true);

        // If panel is ALREADY fully open, keep alpha at 1.0f directly to prevent flickering!
        if (canvasGroup != null && canvasGroup.alpha >= 0.95f)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }
    }

    public void Hide()
    {
        isManuallyShown = false;
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    private void ResetPromptText()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = string.IsNullOrEmpty(_targetWord)
                ? "Tap the mic and speak!"
                : $"Tap and speak the word <b>\"{_targetWord}\"</b> into the mic";
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Direct STT Flow (Matching STTGameController)
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator AutoSkipNoMic()
    {
        yield return new WaitForSeconds(3f);
        // Simulate a perfect transcription of the target word
        OnTranscriptionSuccess(_targetWord);
    }

    private void OnMicButtonTapped()
    {
        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        _isRecording = true;
        SetMicState(true);

        if (tapToStopText != null)
        {
            tapToStopText.text = "Tap to stop";
            tapToStopText.gameObject.SetActive(true);
        }

        if (promptText != null)
            promptText.text = "Listening... Speak clearly!";

        if (SpeechRecorder.Instance != null)
        {
            SpeechRecorder.Instance.StartRecording();
        }

        Debug.Log($"[TeachingOverlayPanel] Started recording for word: '{_targetWord}'");
    }

    private void StopRecording()
    {
        _isRecording = false;
        SetMicState(false);

        // Disable mic while processing so player cannot spam-tap
        if (micButton != null) micButton.interactable = false;

        if (tapToStopText != null)
            tapToStopText.gameObject.SetActive(false);

        if (promptText != null)
            promptText.text = "Processing your voice...";

        string filePath = "";
        if (SpeechRecorder.Instance != null)
        {
            filePath = SpeechRecorder.Instance.StopRecording();
        }

        Debug.Log($"[TeachingOverlayPanel] Stopped recording. Audio path: '{filePath}'");

        if (!string.IsNullOrEmpty(filePath))
        {
            if (GroqWhisperManager.Instance != null)
            {
                string langCode = "";
                if (PhraseEvaluator.Instance != null && PhraseEvaluator.Instance.CurrentRegion == RegionMode.Cebuano)
                    langCode = "ceb";
                else if (PhraseEvaluator.Instance != null && PhraseEvaluator.Instance.CurrentRegion == RegionMode.Ilokano)
                    langCode = "tl";

                GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
            }
        }
        else
        {
            if (promptText != null)
                promptText.text = "<color=#FF5555>Recording failed. Tap to try again.</color>";
                
            // CRITICAL: Re-enable the mic button so they can try again!
            if (micButton != null) micButton.interactable = true;
        }
    }

    private void OnTranscriptionSuccess(string transcribedText)
    {
        Debug.Log($"[TeachingOverlayPanel] Transcribed speech: \"{transcribedText}\"");

        if (promptText != null)
            promptText.text = "Evaluating speech...";

        string target = !string.IsNullOrEmpty(_targetWord) ? _targetWord : 
            (DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null ? DialogueManager.Instance.PendingSTTChoice.expectedSTTWord : "");

        if (!string.IsNullOrEmpty(target) && PhraseEvaluator.Instance != null)
        {
            PhraseEvaluator.Instance.EvaluateSpeech(target, transcribedText, (transcript, scorePercent, evalResult) =>
            {
                bool success = scorePercent >= 80f;
                Debug.Log($"[TeachingOverlayPanel] Evaluation score: {scorePercent:F0}%. Result: {evalResult}");

                if (success)
                {
                    HandleSuccess(transcribedText);
                }
                else
                {
                    HandleFailure();
                }
            });
        }
        else if (PhraseEvaluator.Instance != null)
        {
            PhraseEvaluator.Instance.FindBestMatch(transcribedText, (bestEntry, bestLang, accuracy, isEnglish, matchResult) =>
            {
                bool success = accuracy >= 80f && !isEnglish;
                if (success)
                {
                    HandleSuccess(transcribedText);
                }
                else
                {
                    HandleFailure();
                }
            });
        }
    }

    public void HandleSuccess(string transcribedText)
    {
        Debug.Log($"<color=green>[TeachingOverlayPanel] HandleSuccess called for speech: '{transcribedText}'. Firing CompleteSTT(true)...</color>");

        // 1. Display success message on overlay prompt
        if (promptText != null)
            promptText.text = "<color=#55FF55><b>Great job! Correct!</b></color>";

        // 2. Hide the mic button so player uses NEXT>> on dialogue panel
        if (micButton != null)
            micButton.gameObject.SetActive(false);

        // 3. IMMEDIATELY complete STT in DialogueManager to load success node (e.g. Tiptip_Word_1_Success)
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CompleteSTT(true);
        }

        // NOTE: We do NOT call Hide() here! Panel & background stay active during the success node!
    }

    private void HandleFailure()
    {
        // Display only 'Not quite!' message without doubling the prompt text
        if (promptText != null)
            promptText.text = "<color=#FF7777><b>Not quite! Try again.</b></color>";

        // Re-enable mic button so player can attempt again right away
        if (micButton != null)
        {
            micButton.gameObject.SetActive(true);
            micButton.interactable = true;
            micButton.onClick.RemoveAllListeners();
            micButton.onClick.AddListener(OnMicButtonTapped);
        }

        SetMicState(false);

        // NOTE: Do NOT call CompleteSTT(false) here. The overlay stays open.
        // The player simply taps the mic again to retry — no dialogue node re-load needed.
        Debug.Log("[TeachingOverlayPanel] Failure handled. Player can retry immediately.");
    }

    private void OnTranscriptionError(string error)
    {
        Debug.LogError($"[TeachingOverlayPanel] Transcription Error: {error}");
        if (promptText != null)
            promptText.text = $"<color=#FF7777>Error: {error}</color>";

        if (micButton != null)
        {
            micButton.gameObject.SetActive(true);
            micButton.interactable = true;
        }
        SetMicState(false);
    }

    private void SetMicState(bool active)
    {
        if (micButtonImage == null) return;
        micButtonImage.sprite = active ? micActiveSprite : micInactiveSprite;
    }

    /// <summary>
    /// Applies a white outline to a TextMeshProUGUI for legibility over busy backgrounds.
    /// </summary>
    private void ApplyTextOutline(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new UnityEngine.Color32(255, 255, 255, 255);
    }

    private void EnsureSpeechEngineDependencies()
    {
        if (SpeechRecorder.Instance == null && FindFirstObjectByType<SpeechRecorder>() == null)
            new GameObject("SpeechRecorder").AddComponent<SpeechRecorder>();

        if (GroqWhisperManager.Instance == null && FindFirstObjectByType<GroqWhisperManager>() == null)
            new GameObject("GroqWhisperManager").AddComponent<GroqWhisperManager>();

        if (PhraseEvaluator.Instance == null && FindFirstObjectByType<PhraseEvaluator>() == null)
            new GameObject("PhraseEvaluator").AddComponent<PhraseEvaluator>();
    }

    private Sprite FindBackground(string name)
    {
        if (backgroundOptions == null) return null;
        foreach (var sprite in backgroundOptions)
        {
            if (sprite != null && sprite.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return sprite;
        }
        return null;
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) { gameObject.SetActive(false); yield break; }
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        HideMovementControls(false);
    }

    private void HideMovementControls(bool hide)
    {
        if (movementControls == null)
            movementControls = GameObject.Find("Movement_Controls");

        if (movementControls != null)
        {
            if (hide) movementControls.SetActive(false);
            else
            {
                bool inDialogue = DialogueManager.Instance != null && DialogueManager.Instance.IsInDialogue;
                if (!inDialogue) movementControls.SetActive(true);
            }
        }
    }

    private Coroutine _bgFadeCoroutine;

    private void ChangeBackground(Sprite newSprite)
    {
        if (backgroundImage == null || newSprite == null) return;
        if (backgroundImage.sprite == newSprite) return; // Same background, no swap needed

        if (_bgFadeCoroutine != null) StopCoroutine(_bgFadeCoroutine);

        // If the overlay panel is already open & visible, smoothly cross-fade the background image!
        if (gameObject.activeInHierarchy && canvasGroup != null && canvasGroup.alpha > 0.5f)
        {
            _bgFadeCoroutine = StartCoroutine(FadeBackgroundRoutine(newSprite));
        }
        else
        {
            backgroundImage.sprite = newSprite;
            backgroundImage.color = Color.white;
        }
    }

    private IEnumerator FadeBackgroundRoutine(Sprite newSprite)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Color initialColor = backgroundImage.color;

        // 1. Fade out current background image
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(initialColor.a, 0f, elapsed / duration);
            backgroundImage.color = new Color(initialColor.r, initialColor.g, initialColor.b, a);
            yield return null;
        }

        // 2. Swap background sprite
        backgroundImage.sprite = newSprite;

        // 3. Fade new background image back in
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, elapsed / duration);
            backgroundImage.color = new Color(initialColor.r, initialColor.g, initialColor.b, a);
            yield return null;
        }

        backgroundImage.color = Color.white;
        _bgFadeCoroutine = null;
    }
}
