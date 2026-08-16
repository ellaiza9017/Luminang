using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MemoryGameSTTManager : MonoBehaviour
{
    public static MemoryGameSTTManager Instance { get; private set; }

    [Header("STT UI References")]
    public RectTransform sttPanel;
    public TextMeshProUGUI instructionsStatusText;
    
    [Header("Colors")]
    public Color colorNormal = Color.white;
    public Color colorListening = Color.cyan;
    public Color colorProcessing = Color.yellow;
    public Color colorWrong = Color.red;
    public Color colorCorrect = Color.green;
    
    [Header("Speak Button")]
    public Image speakButtonImg;
    public Sprite activeSpeakSprite;
    public Sprite inactiveSpeakSprite;
    
    [Header("Tries UI")]
    public Image[] triesIcons;
    public Sprite tryUsedSprite;
    public Sprite tryUnusedSprite;
    
    [Header("Popups")]
    public GameObject correctOrWrongPopup;
    public Image correctOrWrongImage;
    public Sprite correctPopupSprite;
    public Sprite wrongPopupSprite;
    
    [Header("SFX")]
    public AudioClip matchCorrectClip;
    public AudioClip matchWrongClip;
    
    private int sttTriesLeft = 3;
    private string currentTargetSentence = "";
    private bool isRecordingSTT = false;
    private bool isSTTActive = false;
    private Vector2 originalSttPos;

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
            originalSttPos = sttPanel.anchoredPosition;
            sttPanel.gameObject.SetActive(false);
            if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(false);
        }
        
        if (speakButtonImg != null)
        {
            Button btn = speakButtonImg.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnSpeakButtonClicked);
        }
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
    }

    public void StartSTT(string targetWord)
    {
        isSTTActive = true;
        isRecordingSTT = false;
        sttTriesLeft = 3;
        currentTargetSentence = targetWord;
        
        for (int i = 0; i < triesIcons.Length; i++)
        {
            if (triesIcons[i] != null) triesIcons[i].sprite = tryUnusedSprite;
        }
        
        UpdateSTTStatus($"Say <u>{targetWord}</u> to proceed!", colorNormal, false, true);
        
        if (sttPanel != null) 
        {
            sttPanel.anchoredPosition = new Vector2(originalSttPos.x, originalSttPos.y - 1000f);
            sttPanel.gameObject.SetActive(true);
            StartCoroutine(SlidePanelY(sttPanel, originalSttPos.y, 0.5f));
        }
    }

    private void UpdateSTTStatus(string message, Color color, bool isRecording, bool buttonInteractable)
    {
        if (instructionsStatusText != null)
        {
            instructionsStatusText.text = message;
            instructionsStatusText.color = color;
        }
        if (speakButtonImg != null)
        {
            speakButtonImg.sprite = isRecording ? activeSpeakSprite : inactiveSpeakSprite;
            Button btn = speakButtonImg.GetComponent<Button>();
            if (btn != null) btn.interactable = buttonInteractable;
        }
    }

    public void OnSpeakButtonClicked()
    {
        if (!isSTTActive) return;

        if (!isRecordingSTT)
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
        isRecordingSTT = true;
        UpdateSTTStatus("Listening... Tap the mic to stop", colorListening, true, true);
        SpeechRecorder.Instance.StartRecording();
    }

    private void StopRecording()
    {
        isRecordingSTT = false;
        UpdateSTTStatus("Processing...", colorProcessing, false, false);
        
        string filePath = SpeechRecorder.Instance.StopRecording();
        if (!string.IsNullOrEmpty(filePath))
        {
            string langCode = MemoryGameManager.Instance.isIlokano ? "tl" : "ceb";
            GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
        }
        else
        {
            UpdateSTTStatus("Failed to record. Try again.", colorWrong, false, true);
        }
    }

    private void OnTranscriptionSuccess(string result)
    {
        if (!isSTTActive) return;

        PhraseEvaluator.Instance.EvaluateSpeech(currentTargetSentence, result, (transcript, backendScore, evalResult) =>
        {
            Debug.Log($"<color=cyan>====== STT DEBUG ======</color>");
            Debug.Log($"<color=white>Target:</color> {currentTargetSentence}");
            Debug.Log($"<color=yellow>Heard:</color> {transcript}");
            Debug.Log($"<color=green>Final Score:</color> {backendScore:F1}%");

            bool success = backendScore >= 80f;

            if (success)
            {
                UpdateSTTStatus("Correct!", colorCorrect, false, false);
                StartCoroutine(EndSTTFlow(true));
            }
            else
            {
                ConsumeTry(backendScore);
            }
        });
    }

    private void OnTranscriptionError(string error)
    {
        if (!isSTTActive) return;
        StartCoroutine(ShowNetworkErrorRoutine());
    }

    private IEnumerator ShowNetworkErrorRoutine()
    {
        if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(true);
        if (correctOrWrongImage != null) correctOrWrongImage.sprite = wrongPopupSprite;
        UpdateSTTStatus("Network Error! Try Again.", colorWrong, false, false);
        
        yield return new WaitForSeconds(1.5f);
        
        if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(false);
        UpdateSTTStatus($"Say <u>{currentTargetSentence}</u> to proceed!", colorNormal, false, true);
    }

    private void ConsumeTry(float score)
    {
        sttTriesLeft--;
        
        if (sttTriesLeft >= 0 && triesIcons.Length > 0)
        {
            if (triesIcons[sttTriesLeft] != null) triesIcons[sttTriesLeft].sprite = tryUsedSprite;
        }

        if (AudioManager.instance != null && matchWrongClip != null) 
            AudioManager.instance.PlaySFX(matchWrongClip);
            
        if (correctOrWrongImage != null) correctOrWrongImage.sprite = wrongPopupSprite;
        
        if (sttTriesLeft > 0)
        {
            StartCoroutine(ShowTemporaryPopupAndResume("Try Again!", colorNormal, score));
        }
        else
        {
            UpdateSTTStatus($"The correct word is: {currentTargetSentence.ToUpper()}", colorWrong, false, false);
            StartCoroutine(EndSTTFlow(false));
        }
    }

    private IEnumerator ShowTemporaryPopupAndResume(string nextMessage, Color nextColor, float score)
    {
        if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(true);
        UpdateSTTStatus($"Wrong ({score:F0}% Match)", colorWrong, false, false);
        
        yield return new WaitForSeconds(1.5f);
        
        if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(false);
        UpdateSTTStatus(nextMessage, nextColor, false, true);
    }

    private IEnumerator EndSTTFlow(bool success)
    {
        isSTTActive = false;
        
        if (success)
        {
            if (correctOrWrongImage != null) correctOrWrongImage.sprite = correctPopupSprite;
            if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(true);
            if (AudioManager.instance != null && matchCorrectClip != null) AudioManager.instance.PlaySFX(matchCorrectClip);
            
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(true);
            yield return new WaitForSeconds(2.5f);
        }
        
        if (correctOrWrongPopup != null) correctOrWrongPopup.SetActive(false);
        
        if (sttPanel != null) StartCoroutine(SlidePanelY(sttPanel, originalSttPos.y - 1000f, 0.5f));
        
        yield return new WaitForSeconds(0.5f);
        
        if (sttPanel != null) sttPanel.gameObject.SetActive(false);

        if (success)
            MemoryGameManager.Instance.OnSTTSuccess();
        else
            MemoryGameManager.Instance.OnSTTFailure();
    }

    private IEnumerator SlidePanelY(RectTransform panel, float targetY, float duration)
    {
        if (panel == null) yield break;
        
        Vector2 startPos = panel.anchoredPosition;
        Vector2 targetPos = new Vector2(startPos.x, targetY);
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            panel.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }
        
        panel.anchoredPosition = targetPos;
    }
}
