using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class FishingSTTManager : MonoBehaviour
{
    public static FishingSTTManager Instance { get; private set; }

    [Header("Main Panels")]
    public GameObject sttGroup;
    public RectTransform sttPanel;
    public RectTransform glowTransform;
    public Image fishCenterImage;

    [Header("STT Panel UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI sayWordText;
    public Button speakButton;
    public Image speakButtonImage;
    public Sprite speakNormalSprite;
    public Sprite speakActiveSprite;

    [Header("Result Overlay")]
    public Image correctWrongImage;
    public Sprite correctResultSprite;
    public Sprite wrongResultSprite;

    [Header("Tries UI")]
    public List<Image> triesImages;
    public Sprite tryUnusedSprite;
    public Sprite tryUsedSprite;

    [Header("Animations")]
    public float panelAnimationDuration = 0.45f;
    public float glowRotationSpeed = 45f;
    public float resultWaitTime = 2f;
    public float entranceDuration = 0.35f;

    [Header("Title Colors")]
    public Color colorInitial = Color.white;
    public Color colorListening = Color.yellow;
    public Color colorProcessing = Color.cyan;
    public Color colorRight = Color.green;
    public Color colorWrong = Color.red;

    [Header("Sound Effects")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSFX;

    private int currentTries = 3;
    private bool isRecording = false;
    private string targetWord = "";
    private bool isSTTActive = false;

    // We store these to restore if needed
    private Vector2 panelOffscreenPos;
    private Vector2 panelOnscreenPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Spawn STT singletons if they are not already in the scene
        EnsureDependencies();

        // Cache the panel's "on-screen" position BEFORE hiding it
        if (sttPanel != null)
        {
            panelOnscreenPos = sttPanel.anchoredPosition;
            panelOffscreenPos = panelOnscreenPos + new Vector2(0, -1200f); // Slide down out of view
        }

        // Hide the group after caching positions
        if (sttGroup != null) sttGroup.SetActive(false);
        if (speakButton != null) speakButton.onClick.AddListener(OnSpeakButtonClicked);
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

        // Apply the language from FishingGameConfig to PhraseEvaluator
        if (PhraseEvaluator.Instance != null)
            PhraseEvaluator.Instance.SetRegion(FishingGameConfig.GetRegionMode());
    }

    void Update()
    {
        if (isSTTActive && glowTransform != null)
        {
            glowTransform.Rotate(0, 0, glowRotationSpeed * Time.deltaTime);
        }
    }

    public void StartSTT(FishController caughtFish)
    {
        if (sttGroup == null) return;
        
        isSTTActive = true;
        currentTries = 3;
        isRecording = false;

        // Reset Tries UI
        foreach (var tryImg in triesImages)
            tryImg.sprite = tryUnusedSprite;

        // Hide overlay from previous round
        if (correctWrongImage != null)
            correctWrongImage.gameObject.SetActive(false);

        // Set Fish Sprite (start invisible — we'll fade it in)
        if (fishCenterImage != null && caughtFish != null)
        {
            fishCenterImage.sprite = caughtFish.iconSprite;
            fishCenterImage.color = new Color(1,1,1,0);
            fishCenterImage.transform.localScale = Vector3.one * 0.5f;
        }
        // Start Glow invisible too
        if (glowTransform != null)
        {
            Image glowImg = glowTransform.GetComponent<Image>();
            if (glowImg != null) glowImg.color = new Color(1,1,1,0);
            glowTransform.localScale = Vector3.one * 0.5f;
        }

        // Figure out the target word based on FishingGameConfig language
        targetWord = "";
        string langToUse = FishingGameConfig.TargetLanguage;
        
        if (DatasetManager.Instance != null && caughtFish != null)
        {
            PhraseEntry entry = DatasetManager.Instance.GetPhraseById(caughtFish.assignedId);
            if (entry != null)
            {
                targetWord = entry.GetPhrase(langToUse);
            }
        }

        // Setup texts
        UpdateTitle("Nice Catch!", colorInitial);
        if (sayWordText != null)
        {
            sayWordText.text = $"Say \"{targetWord}\"";
        }

        // Reset button
        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;

        // Show UI and Slide In, then fade in fish + glow
        sttGroup.SetActive(true);
        StartCoroutine(SlidePanel(panelOffscreenPos, panelOnscreenPos, true));
        
        if (fadeInCoroutine != null) StopCoroutine(fadeInCoroutine);
        fadeInCoroutine = StartCoroutine(FadeInFishAndGlow());
    }

    private void OnSpeakButtonClicked()
    {
        if (!isSTTActive) return;
        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);

        if (!isRecording)
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
        isRecording = true;
        if (speakButtonImage != null) speakButtonImage.sprite = speakActiveSprite;
        UpdateTitle("Listening... Tap Mic to Stop.", colorListening);
        
        SpeechRecorder.Instance.StartRecording();
    }

    private void StopRecording()
    {
        isRecording = false;
        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;
        UpdateTitle("Processing Voice...", colorProcessing);
        
        string filePath = SpeechRecorder.Instance.StopRecording();
        if (!string.IsNullOrEmpty(filePath))
        {
            string langCode = FishingGameConfig.TargetLanguage.ToLower() == "ilokano" ? "tl" : "ceb";
            GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
        }
        else
        {
            UpdateTitle("Failed to record. Try again.", colorWrong);
        }
    }

    private void OnTranscriptionSuccess(string result)
    {
        if (!isSTTActive) return;

        PhraseEvaluator.Instance.EvaluateSpeech(targetWord, result, (transcript, scorePercent, evalResult) =>
        {
            // --- STT DEBUG LOGS ---
            Debug.Log("<color=cyan>====== STT DEBUG INFO ======</color>");
            Debug.Log($"<color=white>Target Word:</color> {targetWord}");
            Debug.Log($"<color=yellow>Heard Word(s):</color> {transcript}");
            Debug.Log($"<color={(scorePercent >= 80f ? "green" : "red")}>Match Score:</color> {scorePercent:F1}%");
            Debug.Log("<color=cyan>============================</color>");
            // ----------------------

            bool success = scorePercent >= 80f;

            if (success)
            {
                ShowResultOverlay(true);
                UpdateTitle(GetRandomCorrectFeedback(), colorRight);
                StartCoroutine(EndSTTFlow(true));
            }
            else
            {
                ShowResultOverlay(false);
                ConsumeTry(scorePercent);
            }
        });
    }

    private void OnTranscriptionError(string error)
    {
        if (!isSTTActive) return;
        UpdateTitle("Oops! Couldn't hear that. Try again.", colorWrong);
        ShowResultOverlay(false);
        ConsumeTry(0);
    }

    private void ConsumeTry(float score)
    {
        currentTries--;
        
        // Update sprite
        if (currentTries >= 0 && currentTries < triesImages.Count)
        {
            triesImages[2 - currentTries].sprite = tryUsedSprite; // Left to right
        }

        if (currentTries > 0)
        {
            UpdateTitle($"Try Again! ({score:F0}% Match)", colorWrong);
        }
        else
        {
            UpdateTitle($"Out of tries! ({score:F0}% Match)", colorWrong);
            StartCoroutine(EndSTTFlow(false)); // Close, but they didn't pass
        }
    }

    private void UpdateTitle(string text, Color color)
    {
        if (titleText != null)
        {
            titleText.text = text;
            titleText.color = color;
        }
    }

    private Coroutine fadeInCoroutine;
    private Coroutine popInCoroutine;

    private string GetRandomCorrectFeedback()
    {
        string[] msgs = {
            "Excellent! You nailed it!",
            "Perfect pronunciation!",
            "Amazing! Keep it up!",
            "Great job! Correct!",
            "You're a natural speaker!"
        };
        return msgs[UnityEngine.Random.Range(0, msgs.Length)];
    }

    private IEnumerator PopInThenOut(Transform t)
    {
        // Pop in
        float elapsed = 0f;
        while (elapsed < 0.18f)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1.15f, elapsed / 0.18f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        // Settle
        elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1.15f, 1f, elapsed / 0.08f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
        // Stay visible (EndSTTFlow will hide with the panel)
    }

    private void ShowResultOverlay(bool isCorrect)
    {
        if (correctWrongImage == null) return;
        correctWrongImage.sprite = isCorrect ? correctResultSprite : wrongResultSprite;
        correctWrongImage.gameObject.SetActive(true);
        correctWrongImage.transform.localScale = Vector3.zero;
        
        if (popInCoroutine != null) StopCoroutine(popInCoroutine);
        popInCoroutine = StartCoroutine(PopInThenOut(correctWrongImage.transform));
    }

    private IEnumerator FadeInFishAndGlow()
    {
        // Wait for panel to start arriving (slight delay feels more polished)
        yield return new WaitForSeconds(panelAnimationDuration * 0.6f);

        Image glowImg = glowTransform != null ? glowTransform.GetComponent<Image>() : null;
        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / entranceDuration);
            float scale = Mathf.Lerp(0.5f, 1f, t);

            if (fishCenterImage != null)
            {
                fishCenterImage.color = new Color(1, 1, 1, t);
                fishCenterImage.transform.localScale = Vector3.one * scale;
            }
            if (glowImg != null)
            {
                glowImg.color = new Color(1, 1, 1, t);
                glowTransform.localScale = Vector3.one * scale;
            }
            yield return null;
        }
        if (fishCenterImage != null) fishCenterImage.color = Color.white;
        if (glowImg != null) glowImg.color = Color.white;
    }

    private IEnumerator EndSTTFlow(bool success)
    {
        isSTTActive = false;
        
        // Wait a bit so the player can see the success/fail message
        yield return new WaitForSeconds(resultWaitTime);

        // Slide out (Flattened Coroutine to avoid IL2CPP crash)
        yield return SlidePanel(panelOnscreenPos, panelOffscreenPos, false);

        if (success)
        {
            FishingQuizManager.Instance.CompleteSTTAndAdvanceRound();
        }
        else
        {
            // If they failed, just go back to the pond (same question/round)
            FishingQuizManager.Instance.CompleteSTTAndFailRound();
        }
    }

    private IEnumerator SlidePanel(Vector2 startPos, Vector2 endPos, bool showGroup)
    {
        if (sttPanel == null) yield break;

        float elapsed = 0f;
        while (elapsed < panelAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / panelAnimationDuration;
            // Smoothstep curve for nice easing
            t = t * t * (3f - 2f * t);
            
            sttPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        sttPanel.anchoredPosition = endPos;

        if (!showGroup && sttGroup != null)
        {
            // CRITICAL: Stop background animations BEFORE disabling the UI group
            // Modifying a disabled UI object causes native IL2CPP crashes on Android.
            if (fadeInCoroutine != null) StopCoroutine(fadeInCoroutine);
            if (popInCoroutine != null) StopCoroutine(popInCoroutine);
            
            sttGroup.SetActive(false);
        }
    }
}
