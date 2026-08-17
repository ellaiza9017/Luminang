using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TusokSTTManager : MonoBehaviour
{
    public static TusokSTTManager Instance { get; private set; }

    [Header("STT UI Panels")]
    public GameObject sttPanel;
    public GameObject speakButton;

    [Header("STT Panel Elements")]
    public Image speakButtonImage;
    public Sprite speakNormalSprite;
    public Sprite speakActiveSprite;

    [Header("Tries UI")]
    public List<Image> triesImages;
    public Sprite tryUnusedSprite;
    public Sprite tryUsedSprite;

    [Header("Animations")]
    public float slideDuration = 0.45f;
    
    [Header("Sound Effects")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSFX;

    private int currentTries = 3;
    private bool isRecording = false;
    private bool isSTTActive = false;
    
    // Target word queue for Counting rounds
    private Queue<string> targetWordsQueue = new Queue<string>();
    private string currentTargetWord = "";
    
    private CountingRoundData currentRoundData;

    private Vector2 sttPanelOnscreenPos;
    private Vector2 sttPanelOffscreenPos;
    private Vector2 speakBtnOnscreenPos;
    private Vector2 speakBtnOffscreenPos;

    void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        EnsureDependencies();

        if (sttPanel != null)
        {
            RectTransform rt = sttPanel.GetComponent<RectTransform>();
            sttPanelOnscreenPos = rt.anchoredPosition;
            sttPanelOffscreenPos = sttPanelOnscreenPos + new Vector2(1000f, 0f);
            rt.anchoredPosition = sttPanelOffscreenPos;
        }

        if (speakButton != null)
        {
            RectTransform rt = speakButton.GetComponent<RectTransform>();
            speakBtnOnscreenPos = rt.anchoredPosition;
            speakBtnOffscreenPos = speakBtnOnscreenPos + new Vector2(0f, -500f);
            rt.anchoredPosition = speakBtnOffscreenPos;
            
            Button btn = speakButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnSpeakButtonClicked);
        }
    }

    private void Update()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (isSTTActive && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
        {
            Debug.Log("[Cheat] P pressed: Forcing STT success.");
            TusokTusokGameManager.Instance.UpdateChatBubbleColorText("Excellent! Correct!", TusokTusokGameManager.Instance.sttCorrectColor);
            TusokTusokGameManager.Instance.SetManongSprite(true);
            StartCoroutine(TransitionToNextWord());
        }
        #endif
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
            // Try to set region if config exists, otherwise default
            // Assuming generic config or PlayerPrefs
            string region = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
            PhraseEvaluator.Instance.SetRegion(region == "Ilokano" ? RegionMode.Ilokano : RegionMode.Cebuano);
        }
    }

    private bool isFirstWord = true;
    private bool currentWordIsFirstWord = true;

    public void StartSTT(CountingRoundData data)
    {
        currentRoundData = data;
        isSTTActive = true;
        currentTries = 3;
        isRecording = false;
        isFirstWord = true;
        currentWordIsFirstWord = true;

        foreach (var tryImg in triesImages)
            if (tryImg != null) tryImg.sprite = tryUnusedSprite;

        targetWordsQueue.Clear();

        string selectedLang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");

        if (data.isRecall)
        {
            // For recall, the target is the selected phrase
            if (DatasetManager.Instance != null)
            {
                PhraseEntry entry = DatasetManager.Instance.GetPhraseById(data.correctPhraseId);
                if (entry != null) targetWordsQueue.Enqueue(entry.GetPhrase(selectedLang));
            }
        }
        else
        {
            // For counting, queue all target words, avoiding duplicates
            string targetWordsStr = selectedLang == "Ilokano" ? data.ilokanoTargetWords : data.cebuanoTargetWords;
            if (!string.IsNullOrEmpty(targetWordsStr))
            {
                string[] words = targetWordsStr.Split(' ');
                foreach (string w in words)
                {
                    if (!string.IsNullOrWhiteSpace(w))
                    {
                        string trimmed = w.Trim();
                        if (!targetWordsQueue.Contains(trimmed))
                        {
                            targetWordsQueue.Enqueue(trimmed);
                        }
                    }
                }
            }
        }

        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;

        if (sttPanel != null && !sttPanel.activeSelf) sttPanel.SetActive(true);
        if (speakButton != null && !speakButton.activeSelf) speakButton.SetActive(true);

        StartCoroutine(SlideElement(sttPanel.GetComponent<RectTransform>(), sttPanelOffscreenPos, sttPanelOnscreenPos));
        StartCoroutine(SlideElement(speakButton.GetComponent<RectTransform>(), speakBtnOffscreenPos, speakBtnOnscreenPos));

        PromptNextWord();
    }

    private void PromptNextWord()
    {
        if (targetWordsQueue.Count > 0)
        {
            currentTargetWord = targetWordsQueue.Dequeue();
            currentWordIsFirstWord = isFirstWord;
            
            // Reset tries for the new word
            currentTries = 3;
            foreach (var tryImg in triesImages)
                if (tryImg != null) tryImg.sprite = tryUnusedSprite;
            
            // Format prompt: "Can you say X?" or "How about X?"
            TusokTusokGameManager.Instance.ShowSTTPrompt(currentTargetWord, currentWordIsFirstWord);
            isFirstWord = false;
        }
        else
        {
            // Finished all words!
            StartCoroutine(EndSTTFlow(true));
        }
    }

    private void OnSpeakButtonClicked()
    {
        if (!isSTTActive) return;
        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);

        if (!isRecording) StartRecording();
        else StopRecording();
    }

    private void StartRecording()
    {
        isRecording = true;
        if (speakButtonImage != null) speakButtonImage.sprite = speakActiveSprite;
        
        TusokTusokGameManager.Instance.UpdateChatBubbleColorText("Listening... Tap Mic to Stop.", TusokTusokGameManager.Instance.sttProcessingColor);
        SpeechRecorder.Instance.StartRecording();
    }

    private void StopRecording()
    {
        isRecording = false;
        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;
        
        TusokTusokGameManager.Instance.UpdateChatBubbleColorText("Processing Voice...", TusokTusokGameManager.Instance.sttProcessingColor);
        
        string filePath = SpeechRecorder.Instance.StopRecording();
        if (!string.IsNullOrEmpty(filePath))
        {
            string langCode = PlayerPrefs.GetString("SelectedLanguage", "Ilokano").ToLower() == "ilokano" ? "tl" : "ceb";
            GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
        }
        else
        {
            TusokTusokGameManager.Instance.UpdateChatBubbleColorText("Failed to record. Try again.", TusokTusokGameManager.Instance.sttWrongColor);
        }
    }

    private void OnTranscriptionSuccess(string result)
    {
        if (!isSTTActive) return;

        PhraseEvaluator.Instance.EvaluateSpeech(currentTargetWord, result, (transcript, scorePercent, evalResult) =>
        {
            Debug.Log("<color=cyan>====== STT DEBUG ======</color>");
            Debug.Log($"<color=white>Target:</color> {currentTargetWord}");
            Debug.Log($"<color=yellow>Heard:</color> {transcript}");
            Debug.Log($"<color=green>Score:</color> {scorePercent:F1}%");

            bool success = scorePercent >= 80f;

            // Manual fallbacks for known Whisper hallucination issues on extremely short words
            if (!success && !string.IsNullOrEmpty(transcript))
            {
                string lowerTranscript = transcript.ToLower();
                if (currentTargetWord.ToLower() == "dua" && (lowerTranscript.Contains("dua") || lowerTranscript.Contains("do a") || lowerTranscript.Contains("two") || lowerTranscript.Contains("to") || lowerTranscript.Contains("duha") || lowerTranscript.Contains("noah") || lowerTranscript.Contains("do") || lowerTranscript.Contains("juan") || lowerTranscript.Contains("luha")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'dua'!</color>");
                }
                else if (currentTargetWord.ToLower() == "walo" && (lowerTranscript.Contains("walo") || lowerTranscript.Contains("huelo") || lowerTranscript.Contains("halo") || lowerTranscript.Contains("wall") || lowerTranscript.Contains("hello")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'walo'!</color>");
                }
                else if (currentTargetWord.ToLower() == "maysa" && (lowerTranscript.Contains("maysa") || lowerTranscript.Contains("mysa") || lowerTranscript.Contains("my sa") || lowerTranscript.Contains("mice") || lowerTranscript.Contains("mass") || lowerTranscript.Contains("misa") || lowerTranscript.Contains("mesa") || lowerTranscript.Contains("maisa") || lowerTranscript.Contains("my son") || lowerTranscript.Contains("lisa")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'maysa'!</color>");
                }
                else if (currentTargetWord.ToLower() == "tallo" && (lowerTranscript.Contains("tallo") || lowerTranscript.Contains("tallow") || lowerTranscript.Contains("shallow") || lowerTranscript.Contains("hello") || lowerTranscript.Contains("yellow") || lowerTranscript.Contains("hallo") || lowerTranscript.Contains("talo") || lowerTranscript.Contains("follow") || lowerTranscript.Contains("halo")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'tallo'!</color>");
                }
                else if (currentTargetWord.ToLower() == "uppat" && (lowerTranscript.Contains("uppat") || lowerTranscript.Contains("up at") || lowerTranscript.Contains("a pat") || lowerTranscript.Contains("apart") || lowerTranscript.Contains("op art") || lowerTranscript.Contains("who pat") || lowerTranscript.Contains("oopa") || lowerTranscript.Contains("up") || lowerTranscript.Contains("pat")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'uppat'!</color>");
                }
                else if (currentTargetWord.ToLower() == "lima" && (lowerTranscript.Contains("lima") || lowerTranscript.Contains("liam") || lowerTranscript.Contains("emma") || lowerTranscript.Contains("lema") || lowerTranscript.Contains("dima")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'lima'!</color>");
                }
                else if (currentTargetWord.ToLower() == "pito" && (lowerTranscript.Contains("pito") || lowerTranscript.Contains("pete") || lowerTranscript.Contains("veto") || lowerTranscript.Contains("vito") || lowerTranscript.Contains("tito") || lowerTranscript.Contains("peter")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'pito'!</color>");
                }
                else if (currentTargetWord.ToLower() == "siam" && (lowerTranscript.Contains("siam") || lowerTranscript.Contains("see em") || lowerTranscript.Contains("sam") || lowerTranscript.Contains("see am") || lowerTranscript.Contains("siam") || lowerTranscript.Contains("shiam") || lowerTranscript.Contains("she am") || lowerTranscript.Contains("sean")))
                {
                    success = true;
                    Debug.Log("<color=magenta>Fallback activated for 'siam'!</color>");
                }
            }

            if (success)
            {
                TusokTusokGameManager.Instance.UpdateChatBubbleColorText("Excellent! Correct!", TusokTusokGameManager.Instance.sttCorrectColor);
                TusokTusokGameManager.Instance.SetManongSprite(true);
                
                // Wait briefly then prompt next word or finish
                StartCoroutine(TransitionToNextWord());
            }
            else
            {
                ConsumeTry(scorePercent);
            }
        });
    }

    private IEnumerator TransitionToNextWord()
    {
        yield return new WaitForSeconds(1.5f);
        TusokTusokGameManager.Instance.SetManongSprite(true, true); // Idle
        PromptNextWord();
    }

    private void OnTranscriptionError(string error)
    {
        if (!isSTTActive) return;
        TusokTusokGameManager.Instance.UpdateChatBubbleColorText("Oops! Couldn't hear that. Try again.", TusokTusokGameManager.Instance.sttWrongColor);
        ConsumeTry(0);
    }

    private void ConsumeTry(float score)
    {
        currentTries--;
        
        if (currentTries >= 0 && currentTries < triesImages.Count)
        {
            if (triesImages[2 - currentTries] != null) triesImages[2 - currentTries].sprite = tryUsedSprite;
        }

        if (currentTries > 0)
        {
            StartCoroutine(ShowWrongFeedbackRoutine());
        }
        else
        {
            TusokTusokGameManager.Instance.UpdateChatBubbleColorText($"Out of tries! ({score:F0}% Match)", TusokTusokGameManager.Instance.sttWrongColor);
            TusokTusokGameManager.Instance.SetManongSprite(false);
            StartCoroutine(EndSTTFlow(false));
        }
    }

    private IEnumerator ShowWrongFeedbackRoutine()
    {
        TusokTusokGameManager.Instance.ShowSTTWrongFeedback();
        yield return new WaitForSeconds(2f);
        
        if (isSTTActive)
        {
            // Revert back to the prompt
            TusokTusokGameManager.Instance.SetManongSprite(true, true); // Idle
            TusokTusokGameManager.Instance.ShowSTTPrompt(currentTargetWord, currentWordIsFirstWord);
        }
    }

    private IEnumerator EndSTTFlow(bool success)
    {
        isSTTActive = false;
        TusokTusokGameManager.Instance.isSTTPhaseActive = false;
        yield return new WaitForSeconds(2f);

        StartCoroutine(SlideElement(sttPanel.GetComponent<RectTransform>(), sttPanelOnscreenPos, sttPanelOffscreenPos));
        StartCoroutine(SlideElement(speakButton.GetComponent<RectTransform>(), speakBtnOnscreenPos, speakBtnOffscreenPos));

        yield return new WaitForSeconds(slideDuration);

        if (success)
        {
            TusokTusokGameManager.Instance.ShowCorrectPopup();
            TusokTusokGameManager.Instance.CompleteRound();
        }
        else
        {
            TusokTusokGameManager.Instance.ShowWrongPopup();
            TusokTusokGameManager.Instance.ResetRoundDueToSTTFail();
        }
    }

    private IEnumerator SlideElement(RectTransform rect, Vector2 from, Vector2 to)
    {
        if (rect == null) yield break;
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / slideDuration);
            rect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
        rect.anchoredPosition = to;
    }
}
