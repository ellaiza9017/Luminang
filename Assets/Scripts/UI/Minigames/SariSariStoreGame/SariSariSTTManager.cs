using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SariSariSTTManager : MonoBehaviour
{
    public static SariSariSTTManager Instance { get; private set; }

    [Header("STT Sliding UI")]
    public RectTransform bottomPanel;
    public RectTransform speakButtonRt;
    public Button speakButton;
    public Image speakButtonImage;
    public Sprite speakNormalSprite;
    public Sprite speakActiveSprite;

    [Header("STT Fading UI")]
    public CanvasGroup wordBoxGroup;
    public CanvasGroup submitButtonGrp;
    public CanvasGroup feedbackBox;
    public CanvasGroup triesContainer;
    public TextMeshProUGUI feedbackText;

    [Header("Tries UI")]
    public List<Image> triesImages;
    public Sprite tryUnusedSprite;
    public Sprite tryUsedSprite;

    [Header("Animations")]
    public float slideDuration = 0.45f;
    public float fadeDuration = 0.3f;
    public float bottomPanelSlideY = 250f;
    public float speakButtonHiddenY = -600f;

    [Header("Sound Effects")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSFX;

    private int currentTries = 3;
    private bool isRecording = false;
    private bool isSTTActive = false;
    private string currentTargetSentence = "";
    private bool currentIsTemplate = false;
    
    private Vector2 bottomPanelOriginalPos;
    private Vector2 speakButtonOriginalPos;
    private Vector2 bottomPanelActivePos;
    private Vector2 speakButtonActivePos;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        EnsureDependencies();

        // Setup Slide Positions
        if (bottomPanel != null)
        {
            bottomPanelOriginalPos = bottomPanel.anchoredPosition;
            bottomPanelActivePos = bottomPanelOriginalPos + new Vector2(0f, bottomPanelSlideY); 
        }

        if (speakButtonRt != null)
        {
            // The position in the editor is where it should be when ACTIVE
            speakButtonActivePos = speakButtonRt.anchoredPosition;
            // It will start hidden below the screen
            speakButtonOriginalPos = speakButtonActivePos + new Vector2(0f, speakButtonHiddenY); 
            
            // Force it to start at the hidden position
            speakButtonRt.anchoredPosition = speakButtonOriginalPos;
        }

        if (speakButton != null)
        {
            speakButton.onClick.AddListener(OnSpeakButtonClicked);
        }

        // Initialize Faded Out elements
        SetCanvasGroupAlpha(feedbackBox, 0f);
        SetCanvasGroupAlpha(triesContainer, 0f);
    }

    private void EnsureDependencies()
    {
        if (SpeechRecorder.Instance == null && FindFirstObjectByType<SpeechRecorder>() == null)
            new GameObject("SpeechRecorder").AddComponent<SpeechRecorder>();

        if (GroqWhisperManager.Instance == null && FindFirstObjectByType<GroqWhisperManager>() == null)
            new GameObject("GroqWhisperManager").AddComponent<GroqWhisperManager>();

        if (PhraseEvaluator.Instance == null && FindFirstObjectByType<PhraseEvaluator>() == null)
            new GameObject("PhraseEvaluator").AddComponent<PhraseEvaluator>();

        if (DatasetManager.Instance == null && FindFirstObjectByType<DatasetManager>() == null)
            new GameObject("DatasetManager").AddComponent<DatasetManager>();

        if (PhraseEvaluator.Instance != null)
            PhraseEvaluator.Instance.SetRegion(SariSariGameConfig.GetRegionMode());
    }

    public void StartSTT(ChismisRoundData data, string targetSentence, bool isTemplate)
    {
        isSTTActive = true;
        currentTries = 3;
        isRecording = false;
        currentTargetSentence = targetSentence;
        currentIsTemplate = isTemplate;

        foreach (var tryImg in triesImages)
            tryImg.sprite = tryUnusedSprite;

        if (feedbackText != null)
        {
            feedbackText.text = "Try Saying it!";
            feedbackText.color = SariSariGameManager.Instance.sttNormalColor;
        }

        if (speakButtonImage != null && speakNormalSprite != null) 
            speakButtonImage.sprite = speakNormalSprite;

        // Start Transition
        StartCoroutine(TransitionToSTTUI());
    }

    private IEnumerator TransitionToSTTUI()
    {
        // 1. Fade out WordBoxGroup & SubmitButton
        if (wordBoxGroup != null) StartCoroutine(FadeCanvasGroup(wordBoxGroup, 1f, 0f));
        if (submitButtonGrp != null) StartCoroutine(FadeCanvasGroup(submitButtonGrp, 1f, 0f));

        yield return new WaitForSeconds(fadeDuration);

        // 2. Slide up BottomPanel and SpeakButton
        if (speakButtonRt != null) speakButtonRt.gameObject.SetActive(true);
        if (bottomPanel != null) StartCoroutine(SlideElement(bottomPanel, bottomPanelOriginalPos, bottomPanelActivePos));
        if (speakButtonRt != null) StartCoroutine(SlideElement(speakButtonRt, speakButtonOriginalPos, speakButtonActivePos));

        yield return new WaitForSeconds(slideDuration);

        // 3. Fade in FeedbackBox and TriesContainer
        if (feedbackBox != null) StartCoroutine(FadeCanvasGroup(feedbackBox, 0f, 1f));
        if (triesContainer != null) StartCoroutine(FadeCanvasGroup(triesContainer, 0f, 1f));
    }

    private IEnumerator EndSTTFlow(bool success)
    {
        isSTTActive = false;
        
        yield return new WaitForSeconds(1.5f); // Wait a bit so player can read success/fail message

        // 1. Fade out FeedbackBox and TriesContainer
        if (feedbackBox != null) StartCoroutine(FadeCanvasGroup(feedbackBox, 1f, 0f));
        if (triesContainer != null) StartCoroutine(FadeCanvasGroup(triesContainer, 1f, 0f));

        yield return new WaitForSeconds(fadeDuration);

        // 2. Slide down BottomPanel and SpeakButton
        if (bottomPanel != null) StartCoroutine(SlideElement(bottomPanel, bottomPanelActivePos, bottomPanelOriginalPos));
        if (speakButtonRt != null) StartCoroutine(SlideElement(speakButtonRt, speakButtonActivePos, speakButtonOriginalPos));

        yield return new WaitForSeconds(slideDuration);
        
        if (speakButtonRt != null) speakButtonRt.gameObject.SetActive(false);

        // 3. Fade in WordBoxGroup & SubmitButton
        if (wordBoxGroup != null) StartCoroutine(FadeCanvasGroup(wordBoxGroup, 0f, 1f));
        if (submitButtonGrp != null) StartCoroutine(FadeCanvasGroup(submitButtonGrp, 0f, 1f));

        // 4. Notify GameManager
        if (success)
            SariSariGameManager.Instance.OnSTTSuccess();
        else
            SariSariGameManager.Instance.OnSTTFailure(currentTargetSentence);
    }

    private void OnSpeakButtonClicked()
    {
        if (!isSTTActive) return;
        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);

        if (!isRecording)
            StartRecording();
        else
            StopRecording();
    }

    private void StartRecording()
    {
        isRecording = true;
        if (speakButtonImage != null && speakActiveSprite != null) 
            speakButtonImage.sprite = speakActiveSprite;
        
        UpdateFeedback("Listening... Tap Mic to Stop.", SariSariGameManager.Instance.sttProcessingColor);
        SpeechRecorder.Instance.StartRecording();
    }

    private void StopRecording()
    {
        isRecording = false;
        if (speakButtonImage != null && speakNormalSprite != null) 
            speakButtonImage.sprite = speakNormalSprite;
        
        UpdateFeedback("Processing Voice...", SariSariGameManager.Instance.sttProcessingColor);
        
        string filePath = SpeechRecorder.Instance.StopRecording();
        if (!string.IsNullOrEmpty(filePath))
        {
            string langCode = SariSariGameConfig.TargetLanguage.ToLower() == "ilokano" ? "tl" : "ceb";
            GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
        }
        else
        {
            UpdateFeedback("Failed to record. Try again.", SariSariGameManager.Instance.sttWrongColor);
        }
    }

    private void OnTranscriptionSuccess(string result)
    {
        if (!isSTTActive) return;

        if (currentIsTemplate)
        {
            float localScore = ComputeStringSimilarity(currentTargetSentence, result);
            
            Debug.Log($"<color=cyan>====== STT DEBUG ======</color>");
            Debug.Log($"<color=white>Target:</color> {currentTargetSentence} (TYPED TEMPLATE)");
            Debug.Log($"<color=yellow>Heard:</color> {result}");
            Debug.Log($"<color=green>Forgiving Score:</color> {localScore:F1}%");

            // For templates, we use a VERY forgiving threshold (60%) because names/places are often misspelled by the STT
            bool success = localScore >= 60f;

            if (success)
            {
                UpdateFeedback("Excellent! Correct!", SariSariGameManager.Instance.sttCorrectColor);
                StartCoroutine(EndSTTFlow(true));
            }
            else
            {
                ConsumeTry(localScore);
            }
        }
        else
        {
            PhraseEvaluator.Instance.EvaluateSpeech(currentTargetSentence, result, (transcript, backendScore, evalResult) =>
            {
                Debug.Log($"<color=cyan>====== STT DEBUG ======</color>");
                Debug.Log($"<color=white>Target:</color> {currentTargetSentence}");
                Debug.Log($"<color=yellow>Heard:</color> {transcript}");
                Debug.Log($"<color=green>Final Score:</color> {backendScore:F1}%");

                bool success = backendScore >= 80f;

                if (success)
                {
                    UpdateFeedback("Excellent! Correct!", SariSariGameManager.Instance.sttCorrectColor);
                    StartCoroutine(EndSTTFlow(true));
                }
                else
                {
                    ConsumeTry(backendScore);
                }
            });
        }
    }

    private void OnTranscriptionError(string error)
    {
        if (!isSTTActive) return;
        UpdateFeedback("Oops! Couldn't hear that. Try again.", SariSariGameManager.Instance.sttWrongColor);
        ConsumeTry(0);
    }

    private void ConsumeTry(float score)
    {
        currentTries--;
        
        if (currentTries >= 0 && currentTries < triesImages.Count)
        {
            triesImages[2 - currentTries].sprite = tryUsedSprite;
        }

        if (currentTries > 0)
        {
            UpdateFeedback($"Try Again! ({score:F0}% Match)", SariSariGameManager.Instance.sttWarningTextColor);
        }
        else
        {
            UpdateFeedback($"Out of tries! ({score:F0}% Match)", SariSariGameManager.Instance.sttWrongColor);
            StartCoroutine(EndSTTFlow(false));
        }
    }

    private void UpdateFeedback(string text, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.color = color;
        }
    }

    private IEnumerator SlideElement(RectTransform rt, Vector2 start, Vector2 end)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            // Ease out cubic
            t = 1f - Mathf.Pow(1f - t, 3f);
            rt.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        rt.anchoredPosition = end;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            cg.alpha = Mathf.Lerp(start, end, t);
            
            if (end > 0.5f) { cg.interactable = true; cg.blocksRaycasts = true; }
            else { cg.interactable = false; cg.blocksRaycasts = false; }
            
            yield return null;
        }
        cg.alpha = end;
        
        if (end > 0.5f) { cg.interactable = true; cg.blocksRaycasts = true; }
        else { cg.interactable = false; cg.blocksRaycasts = false; }
    }

    private void SetCanvasGroupAlpha(CanvasGroup cg, float alpha)
    {
        if (cg != null)
        {
            cg.alpha = alpha;
            if (alpha > 0.5f) { cg.interactable = true; cg.blocksRaycasts = true; }
            else { cg.interactable = false; cg.blocksRaycasts = false; }
        }
    }

    private float ComputeStringSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0f;
        
        // Highly forgiving string cleanup: remove all spaces and common punctuation
        source = System.Text.RegularExpressions.Regex.Replace(source.ToLower(), @"[\s\-\.\,\!\?\'\""]", "");
        target = System.Text.RegularExpressions.Regex.Replace(target.ToLower(), @"[\s\-\.\,\!\?\'\""]", "");
        
        int n = source.Length;
        int m = target.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) { d[i, 0] = i; }
        for (int j = 0; j <= m; j++) { d[0, j] = j; }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                d[i, j] = UnityEngine.Mathf.Min(
                    UnityEngine.Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        
        int maxLen = UnityEngine.Mathf.Max(n, m);
        if (maxLen == 0) return 100f;
        return (1f - ((float)d[n, m] / maxLen)) * 100f;
    }
}
