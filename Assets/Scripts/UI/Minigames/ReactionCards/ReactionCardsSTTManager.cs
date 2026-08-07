using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ReactionCardsSTTManager : MonoBehaviour
{
    public static ReactionCardsSTTManager Instance { get; private set; }

    [Header("Main Panels")]
    public RectTransform sttPanel;
    
    [Header("STT Panel UI")]
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
    public float resultWaitTime = 2f;

    [Header("Title Colors")]
    public Color colorNormal = Color.black;
    public Color colorListening = Color.yellow;
    public Color colorProcessing = Color.cyan;
    public Color colorRight = Color.green;
    public Color colorWrong = Color.red;
    
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSFX;

    private int currentTries = 3;
    private bool isRecording = false;
    private string targetWord = "";
    private bool isSTTActive = false;

    private Vector2 panelOffscreenPos;
    private Vector2 panelOnscreenPos;
    private Coroutine popInCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        EnsureDependencies();

        if (sttPanel != null)
        {
            panelOnscreenPos = sttPanel.anchoredPosition;
            panelOffscreenPos = panelOnscreenPos + new Vector2(1200f, 0f); // Slide right
            sttPanel.anchoredPosition = panelOffscreenPos;
        }

        if (sttPanel != null) sttPanel.gameObject.SetActive(false);
        if (correctWrongImage != null) correctWrongImage.gameObject.SetActive(false);
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

        if (PhraseEvaluator.Instance != null)
        {
            string lang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano").ToLower();
            RegionMode mode = (lang == "cebuano") ? RegionMode.Cebuano : RegionMode.Ilokano;
            PhraseEvaluator.Instance.SetRegion(mode);
        }
    }

    public void StartSTT(string phraseId)
    {
        if (sttPanel == null) return;
        
        isSTTActive = true;
        currentTries = 3;
        isRecording = false;

        foreach (var tryImg in triesImages)
            if (tryImg != null) tryImg.sprite = tryUnusedSprite;

        if (correctWrongImage != null)
            correctWrongImage.gameObject.SetActive(false);

        string langToUse = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        targetWord = "";

        if (DatasetManager.Instance != null)
        {
            PhraseEntry entry = DatasetManager.Instance.GetPhraseById(phraseId);
            if (entry != null)
            {
                targetWord = entry.GetPhrase(langToUse);
            }
        }
        else
        {
            targetWord = phraseId; // Fallback
        }

        ReactionCardsManager.Instance.UpdateQuestionText($"Say \"{targetWord}\" to go to the next round", colorNormal);
        
        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;

        sttPanel.gameObject.SetActive(true);
        StartCoroutine(SlidePanel(panelOffscreenPos, panelOnscreenPos, true));
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
        if (speakButtonImage != null) speakButtonImage.sprite = speakActiveSprite;
        ReactionCardsManager.Instance.UpdateQuestionText("Listening...", colorListening);
        
        SpeechRecorder.Instance.StartRecording();
    }

    private void StopRecording()
    {
        isRecording = false;
        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;
        ReactionCardsManager.Instance.UpdateQuestionText("Processing voice...", colorProcessing);
        
        string filePath = SpeechRecorder.Instance.StopRecording();
        if (!string.IsNullOrEmpty(filePath))
        {
            string langCode = PlayerPrefs.GetString("SelectedLanguage", "Ilokano").ToLower() == "ilokano" ? "tl" : "ceb";
            GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
        }
        else
        {
            ReactionCardsManager.Instance.UpdateQuestionText("Failed to record. Try again.", colorWrong);
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
                ReactionCardsManager.Instance.UpdateQuestionText(GetRandomCorrectFeedback(), colorRight);
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
        ReactionCardsManager.Instance.UpdateQuestionText("Oops! Couldn't hear that. Try again.", colorWrong);
        ShowResultOverlay(false);
        ConsumeTry(0);
    }

    private void ConsumeTry(float score)
    {
        currentTries--;
        
        if (currentTries >= 0 && currentTries < triesImages.Count)
        {
            if (triesImages[2 - currentTries] != null) 
                triesImages[2 - currentTries].sprite = tryUsedSprite;
        }

        if (currentTries > 0)
        {
            ReactionCardsManager.Instance.UpdateQuestionText($"Try Again! ({score:F0}% Match)", colorWrong);
            StartCoroutine(ResetTextToTargetWord());
        }
        else
        {
            ReactionCardsManager.Instance.UpdateQuestionText($"Out of tries! ({score:F0}% Match)", colorWrong);
            StartCoroutine(EndSTTFlow(false));
        }
    }

    private IEnumerator ResetTextToTargetWord()
    {
        yield return new WaitForSeconds(2f);
        
        // Only reset if they haven't already started recording again
        if (isSTTActive && !isRecording && currentTries > 0)
        {
            ReactionCardsManager.Instance.UpdateQuestionText($"Say \"{targetWord}\" to go to the next round", colorNormal);
        }
    }

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

    private void ShowResultOverlay(bool isCorrect)
    {
        if (correctWrongImage == null) return;
        correctWrongImage.sprite = isCorrect ? correctResultSprite : wrongResultSprite;
        correctWrongImage.gameObject.SetActive(true);
        correctWrongImage.transform.localScale = Vector3.zero;
        
        if (popInCoroutine != null) StopCoroutine(popInCoroutine);
        popInCoroutine = StartCoroutine(PopInThenOut(correctWrongImage.transform));
    }

    private IEnumerator PopInThenOut(Transform t)
    {
        float elapsed = 0f;
        while (elapsed < 0.18f)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1.15f, elapsed / 0.18f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1.15f, 1f, elapsed / 0.08f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
        
        // Let it hang for a moment, then fade out so it doesn't block UI permanently
        yield return new WaitForSeconds(1.5f);
        
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0f, elapsed / 0.2f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.gameObject.SetActive(false);
    }

    private IEnumerator EndSTTFlow(bool success)
    {
        isSTTActive = false;
        
        yield return new WaitForSeconds(resultWaitTime);

        yield return SlidePanel(panelOnscreenPos, panelOffscreenPos, false);

        if (success)
        {
            ReactionCardsManager.Instance.CompleteSTTAndAdvanceRound();
        }
        else
        {
            ReactionCardsManager.Instance.CompleteSTTAndFailRound();
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
            t = t * t * (3f - 2f * t);
            
            sttPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        sttPanel.anchoredPosition = endPos;

        if (!showGroup && sttPanel != null)
        {
            sttPanel.gameObject.SetActive(false);
        }
    }
}
