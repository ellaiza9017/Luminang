using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TumbangPresoSTTManager : MonoBehaviour
{
    public static TumbangPresoSTTManager Instance { get; private set; }

    [Header("STT UI Panels")]
    public GameObject sttPanel; // The main sliding panel
    public GameObject speakButton; // The mic button
    public GameObject shadowObj; // Shadow (1) overlay

    [Header("STT Panel Elements")]
    public TextMeshProUGUI sayWordText;
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
    private string targetWord = "";
    private bool isSTTActive = false;
    
    // Store original positions for sliding
    private Vector2 sttPanelOnscreenPos;
    private Vector2 sttPanelOffscreenPos;
    private Vector2 speakBtnOnscreenPos;
    private Vector2 speakBtnOffscreenPos;

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
            RectTransform rt = sttPanel.GetComponent<RectTransform>();
            sttPanelOnscreenPos = rt.anchoredPosition;
            sttPanelOffscreenPos = sttPanelOnscreenPos + new Vector2(1000f, 0f); // Slide off to the right
            rt.anchoredPosition = sttPanelOffscreenPos;
        }

        if (speakButton != null)
        {
            RectTransform rt = speakButton.GetComponent<RectTransform>();
            speakBtnOnscreenPos = rt.anchoredPosition;
            speakBtnOffscreenPos = speakBtnOnscreenPos + new Vector2(0f, -500f); // Slide down
            rt.anchoredPosition = speakBtnOffscreenPos;
            
            Button btn = speakButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnSpeakButtonClicked);
        }

        if (shadowObj != null) shadowObj.SetActive(false);
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
            PhraseEvaluator.Instance.SetRegion(TumbangPresoGameConfig.GetRegionMode());
    }

    public void StartSTT(TumbangPresoResponseData data)
    {
        isSTTActive = true;
        currentTries = 3;
        isRecording = false;

        foreach (var tryImg in triesImages)
            tryImg.sprite = tryUnusedSprite;

        // Fetch the target word
        targetWord = "";
        string langToUse = TumbangPresoGameConfig.TargetLanguage; // Assuming TumbangPresoGameConfig holds global language setting
        if (DatasetManager.Instance != null)
        {
            PhraseEntry entry = DatasetManager.Instance.GetPhraseById(data.correctPhraseId);
            if (entry != null)
            {
                targetWord = entry.GetPhrase(langToUse);
            }
        }
        
        // Also register acceptable phrases directly to PhraseEvaluator if supported
        // But PhraseEvaluator evaluates against targetWord and its synonyms internally, 
        // so we can just check if the result matches ANY acceptable phrase ID
        
        if (sayWordText != null)
        {
            sayWordText.text = "No choices this time! Show us your memory power!";
            sayWordText.color = TumbangPresoGameManager.Instance.sttWarningTextColor;
        }

        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;

        if (shadowObj != null) shadowObj.SetActive(true);

        StartCoroutine(SlideElement(sttPanel.GetComponent<RectTransform>(), sttPanelOffscreenPos, sttPanelOnscreenPos));
        StartCoroutine(SlideElement(speakButton.GetComponent<RectTransform>(), speakBtnOffscreenPos, speakBtnOnscreenPos));
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
        
        TumbangPresoGameManager.Instance.UpdateSituationPromptText("Listening... Tap Mic to Stop.", TumbangPresoGameManager.Instance.sttProcessingColor);
        SpeechRecorder.Instance.StartRecording();
    }

    private void StopRecording()
    {
        isRecording = false;
        if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;
        
        TumbangPresoGameManager.Instance.UpdateSituationPromptText("Processing Voice...", TumbangPresoGameManager.Instance.sttProcessingColor);
        
        string filePath = SpeechRecorder.Instance.StopRecording();
        if (!string.IsNullOrEmpty(filePath))
        {
            string langCode = TumbangPresoGameConfig.TargetLanguage.ToLower() == "ilokano" ? "tl" : "ceb";
            GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
        }
        else
        {
            TumbangPresoGameManager.Instance.UpdateSituationPromptText("Failed to record. Try again.", TumbangPresoGameManager.Instance.sttWrongColor);
        }
    }

    private void OnTranscriptionSuccess(string result)
    {
        if (!isSTTActive) return;

        PhraseEvaluator.Instance.EvaluateSpeech(targetWord, result, (transcript, scorePercent, evalResult) =>
        {
            // Debug Logs
            Debug.Log($"<color=cyan>====== STT DEBUG ======</color>");
            Debug.Log($"<color=white>Target:</color> {targetWord}");
            Debug.Log($"<color=yellow>Heard:</color> {transcript}");
            Debug.Log($"<color=green>Score:</color> {scorePercent:F1}%");

            bool success = scorePercent >= 80f;
            
            // Check acceptable phrase IDs
            if (!success && DatasetManager.Instance != null && TumbangPresoGameManager.Instance != null)
            {
                var data = TumbangPresoGameManager.Instance.GetCurrentSituationData();
                if (data != null && data.acceptablePhraseIds != null)
                {
                    string langToUse = TumbangPresoGameConfig.TargetLanguage;
                    foreach (string phraseId in data.acceptablePhraseIds)
                    {
                        PhraseEntry entry = DatasetManager.Instance.GetPhraseById(phraseId);
                        if (entry != null)
                        {
                            string altTarget = entry.GetPhrase(langToUse);
                            // Re-evaluate synchronously (approximate using string match or custom logic, but for now exact contains)
                            if (transcript.ToLower().Contains(altTarget.ToLower()))
                            {
                                success = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (success)
            {
                TumbangPresoGameManager.Instance.UpdateSituationPromptText("Excellent! Correct!", TumbangPresoGameManager.Instance.sttCorrectColor);
                TumbangPresoGameManager.Instance.ShowFeedbackPopup(true);
                StartCoroutine(EndSTTFlow(true));
            }
            else
            {
                ConsumeTry(scorePercent);
            }
        });
    }

    private void OnTranscriptionError(string error)
    {
        if (!isSTTActive) return;
        TumbangPresoGameManager.Instance.UpdateSituationPromptText("Oops! Couldn't hear that. Try again.", TumbangPresoGameManager.Instance.sttWrongColor);
        TumbangPresoGameManager.Instance.ShowFeedbackPopup(false);
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
            TumbangPresoGameManager.Instance.UpdateSituationPromptText($"Try Again! ({score:F0}% Match)", TumbangPresoGameManager.Instance.sttWrongColor);
            TumbangPresoGameManager.Instance.ShowFeedbackPopup(false);
        }
        else
        {
            TumbangPresoGameManager.Instance.UpdateSituationPromptText($"Out of tries! ({score:F0}% Match)", TumbangPresoGameManager.Instance.sttWrongColor);
            TumbangPresoGameManager.Instance.ShowFeedbackPopup(false);
            StartCoroutine(EndSTTFlow(false));
        }
    }

    private IEnumerator EndSTTFlow(bool success)
    {
        isSTTActive = false;
        
        yield return new WaitForSeconds(2f);

        StartCoroutine(SlideElement(sttPanel.GetComponent<RectTransform>(), sttPanelOnscreenPos, sttPanelOffscreenPos));
        StartCoroutine(SlideElement(speakButton.GetComponent<RectTransform>(), speakBtnOnscreenPos, speakBtnOffscreenPos));
        
        if (shadowObj != null) shadowObj.SetActive(false);

        if (success)
        {
            TumbangPresoGameManager.Instance.CompleteSTTAndAdvanceRound();
        }
        else
        {
            TumbangPresoGameManager.Instance.CompleteSTTAndFailRound();
        }
    }

    private IEnumerator SlideElement(RectTransform rt, Vector2 startPos, Vector2 endPos)
    {
        if (rt == null) yield break;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            t = t * t * (3f - 2f * t);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        rt.anchoredPosition = endPos;
    }
}
