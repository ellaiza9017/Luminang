using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Luminang.UI.Minigames
{
    public class TCGSTTManager : MonoBehaviour
    {
        public static TCGSTTManager Instance { get; private set; }

        [Header("Main Panels")]
        public GameObject sttGroup;
        public RectTransform sttPanel;
        public RectTransform glowTransform;

        [Header("STT Panel UI")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI sayWordText;
        public Button speakButton;
        public Image speakButtonImage;
        public Sprite speakNormalSprite;
        public Sprite speakActiveSprite;

        [Header("Result Overlay")]
        public GameObject correctResultObject;
        public GameObject wrongResultObject;

        [Header("Tries UI")]
        public List<Image> triesImages;
        public Sprite tryUnusedSprite;
        public Sprite tryUsedSprite;

        [Header("STT Debug Label")]
        public TextMeshProUGUI sttDebugText;

        [Header("Animations & Timing")]
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
        public AudioClip panelOpenSFX;
        public AudioClip successSFX;
        public AudioClip tryAgainSFX;

        private int currentTries = 3;
        private bool isRecording = false;
        private string targetWord = "";
        private bool isSTTActive = false;

        private Action onSTTSuccess;
        private Action onSTTFail;

        private Vector2 panelOffscreenPos;
        private Vector2 panelOnscreenPos;

        private Coroutine fadeInCoroutine;
        private Coroutine popInCoroutine;
        
        private Vector3 correctResultOriginalScale = Vector3.one;
        private Vector3 wrongResultOriginalScale = Vector3.one;

        public bool IsSTTActive => isSTTActive;
        public string TargetWord => targetWord;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            EnsureDependencies();

            // Cache the panel's "on-screen" position BEFORE hiding it
            if (sttPanel != null)
            {
                panelOnscreenPos = sttPanel.anchoredPosition;
                panelOffscreenPos = panelOnscreenPos + new Vector2(0, -1200f); // Slide down out of view
                sttPanel.anchoredPosition = panelOffscreenPos;
            }
            
            if (correctResultObject != null) correctResultOriginalScale = correctResultObject.transform.localScale;
            if (wrongResultObject != null) wrongResultOriginalScale = wrongResultObject.transform.localScale;

            // Hide the group after caching positions
            if (sttGroup != null) sttGroup.SetActive(false);
            if (speakButton != null) speakButton.onClick.AddListener(OnSpeakButtonClicked);
            if (sttDebugText != null) sttDebugText.text = "";
        }

        private void Update()
        {
            if (isSTTActive)
            {
                if (glowTransform != null)
                {
                    glowTransform.Rotate(0, 0, glowRotationSpeed * Time.deltaTime);
                }

                // Debug Shortcuts
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                #if ENABLE_INPUT_SYSTEM
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
                {
                    SimulateSuccess(targetWord); // Instant debug pass
                }
                #endif

                #if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.P))
                {
                    SimulateSuccess(targetWord); // Instant debug pass
                }
                #endif
                #endif
            }
        }

        public void SimulateSuccess(string spokenWord = "")
        {
            if (!isSTTActive) return;
            ShowResultOverlay(true);
            UpdateTitle(GetRandomCorrectFeedback(), colorRight);
            if (sttDebugText != null)
            {
                sttDebugText.text = $"[DEBUG BYPASS]\nTarget: {targetWord}\nScore: 100%";
            }
            StartCoroutine(EndSTTFlow(true));
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

            // Apply target region mode
            if (PhraseEvaluator.Instance != null)
                PhraseEvaluator.Instance.SetRegion(FishingGameConfig.GetRegionMode());
        }

        public void StartSTT(PhraseEntry phrase, Action onSuccess, Action onFail)
        {
            if (sttGroup == null || phrase == null) return;

            isSTTActive = true;
            currentTries = 3;
            isRecording = false;
            onSTTSuccess = onSuccess;
            onSTTFail = onFail;

            // Reset Tries UI
            if (triesImages != null)
            {
                foreach (var tryImg in triesImages)
                {
                    if (tryImg != null) tryImg.sprite = tryUnusedSprite;
                }
            }

            // Hide overlay from previous rounds
            if (correctResultObject != null) correctResultObject.SetActive(false);
            if (wrongResultObject != null) wrongResultObject.SetActive(false);

            // Determine target word/phrase using config language
            string langToUse = FishingGameConfig.TargetLanguage;
            targetWord = phrase.GetPhrase(langToUse);

            // Setup texts
            UpdateTitle("Pronounce the phrase:", colorInitial);
            if (sayWordText != null)
            {
                sayWordText.text = $"Say \"{targetWord}\"";
            }

            if (sttDebugText != null)
            {
                sttDebugText.text = $"Target: {targetWord}\nWaiting for input...";
            }

            // Reset mic button sprite
            if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;

            // Play panel open SFX
            if (sfxSource != null && panelOpenSFX != null)
            {
                sfxSource.PlayOneShot(panelOpenSFX);
            }

            // Slide in STT Panel
            sttGroup.SetActive(true);
            StartCoroutine(SlidePanel(panelOffscreenPos, panelOnscreenPos, true));

            if (fadeInCoroutine != null) StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = StartCoroutine(FadeInGlow());
        }

        private float lastSpeakClickTime = 0f;

        public void OnSpeakButtonClicked()
        {
            if (!isSTTActive) return;
            
            // Prevent double-firing from Unity Button bugs or double-taps causing instant Start/Stop
            if (Time.time - lastSpeakClickTime < 0.5f) return;
            lastSpeakClickTime = Time.time;

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
            
            var recorder = SpeechRecorder.Instance ?? FindFirstObjectByType<SpeechRecorder>();
            if (recorder != null)
            {
                recorder.StartRecording();
            }
            else
            {
                UpdateTitle("SpeechRecorder instance missing.", colorWrong);
            }
        }

        private void StopRecording()
        {
            isRecording = false;
            if (speakButtonImage != null) speakButtonImage.sprite = speakNormalSprite;
            UpdateTitle("Processing Voice...", colorProcessing);
            
            var recorder = SpeechRecorder.Instance ?? FindFirstObjectByType<SpeechRecorder>();
            string filePath = recorder != null ? recorder.StopRecording() : "";
            if (!string.IsNullOrEmpty(filePath))
            {
                string langCode = FishingGameConfig.TargetLanguage.ToLower() == "ilokano" ? "tl" : "ceb";
                var whisper = GroqWhisperManager.Instance ?? FindFirstObjectByType<GroqWhisperManager>();
                if (whisper != null)
                {
                    whisper.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
                }
                else
                {
                    UpdateTitle("GroqWhisperManager instance missing.", colorWrong);
                }
            }
            else
            {
                UpdateTitle("Failed to record. Try again.", colorWrong);
            }
        }

        private void OnTranscriptionSuccess(string result)
        {
            if (!isSTTActive) return;

            // Strip template placeholders like {item}, {name} for robust voice recognition
            string cleanTarget = System.Text.RegularExpressions.Regex.Replace(targetWord, @"\{.*?\}", "").Trim();
            string cleanResult = result != null ? result.Trim() : "";

            // Check if clean target is contained in transcription or if it matches
            bool templateMatch = false;
            if (!string.IsNullOrEmpty(cleanTarget) && cleanTarget != targetWord)
            {
                if (!string.IsNullOrEmpty(cleanResult) && cleanResult.ToLower().Contains(cleanTarget.ToLower()))
                {
                    templateMatch = true;
                }
            }

            string evalTarget = string.IsNullOrEmpty(cleanTarget) ? targetWord : cleanTarget;

            PhraseEvaluator.Instance.EvaluateSpeech(evalTarget, result, (transcript, scorePercent, evalResult) =>
            {
                if (templateMatch && scorePercent < 80f)
                {
                    scorePercent = 95f; // Template base phrase was spoken correctly
                }

                // Update Debug text on the panel
                if (sttDebugText != null)
                {
                    sttDebugText.text = $"Target: {targetWord}\nHeard: \"{transcript}\"\nScore: {scorePercent:F1}%";
                }

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
            if (sttDebugText != null)
            {
                sttDebugText.text = $"Error: {error}";
            }
            ShowResultOverlay(false);
            ConsumeTry(0);
        }

        private void ConsumeTry(float score)
        {
            currentTries--;
            
            if (triesImages != null && triesImages.Count > 0)
            {
                int indexToChange = 2 - currentTries;
                if (indexToChange >= 0 && indexToChange < triesImages.Count && triesImages[indexToChange] != null)
                {
                    triesImages[indexToChange].sprite = tryUsedSprite;
                }
            }

            if (currentTries > 0)
            {
                UpdateTitle($"Try Again! ({score:F0}% Match)", colorWrong);
            }
            else
            {
                UpdateTitle($"Out of tries! ({score:F0}% Match)", colorWrong);
                StartCoroutine(EndSTTFlow(false));
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
            if (correctResultObject != null) correctResultObject.SetActive(false);
            if (wrongResultObject != null) wrongResultObject.SetActive(false);

            GameObject targetObj = isCorrect ? correctResultObject : wrongResultObject;
            if (targetObj == null) return;

            // Play success or try-again SFX as the overlay pops in
            if (isCorrect)
            {
                if (sfxSource != null && successSFX != null) sfxSource.PlayOneShot(successSFX);
            }
            else
            {
                if (sfxSource != null && tryAgainSFX != null) sfxSource.PlayOneShot(tryAgainSFX);
            }

            targetObj.SetActive(true);
            targetObj.transform.localScale = Vector3.zero;
            
            Vector3 targetOriginalScale = isCorrect ? correctResultOriginalScale : wrongResultOriginalScale;

            if (popInCoroutine != null) StopCoroutine(popInCoroutine);
            popInCoroutine = StartCoroutine(PopInThenOut(targetObj.transform, targetOriginalScale, !isCorrect));
        }

        private IEnumerator PopInThenOut(Transform t, Vector3 originalScale, bool autoPopOut)
        {
            float elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(0f, 1.15f, elapsed / 0.18f);
                t.localScale = originalScale * s;
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(1.15f, 1f, elapsed / 0.08f);
                t.localScale = originalScale * s;
                yield return null;
            }
            t.localScale = originalScale;

            if (autoPopOut)
            {
                yield return new WaitForSeconds(1.5f);
                elapsed = 0f;
                while (elapsed < 0.15f)
                {
                    elapsed += Time.deltaTime;
                    float s = Mathf.Lerp(1f, 0f, elapsed / 0.15f);
                    t.localScale = originalScale * s;
                    yield return null;
                }
                t.localScale = Vector3.zero;
                t.gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeInGlow()
        {
            yield return new WaitForSeconds(panelAnimationDuration * 0.6f);
            if (glowTransform != null)
            {
                Image glowImg = glowTransform.GetComponent<Image>();
                if (glowImg != null) glowImg.color = new Color(1, 1, 1, 0);
                glowTransform.localScale = Vector3.one * 0.5f;

                float elapsed = 0f;
                while (elapsed < entranceDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / entranceDuration);
                    float scale = Mathf.Lerp(0.5f, 1f, t);

                    if (glowImg != null) glowImg.color = new Color(1, 1, 1, t);
                    glowTransform.localScale = Vector3.one * scale;
                    yield return null;
                }
                if (glowImg != null) glowImg.color = Color.white;
            }
        }

        private IEnumerator EndSTTFlow(bool success)
        {
            isSTTActive = false;
            yield return new WaitForSeconds(resultWaitTime);

            yield return SlidePanel(panelOnscreenPos, panelOffscreenPos, false);

            if (success)
            {
                onSTTSuccess?.Invoke();
            }
            else
            {
                onSTTFail?.Invoke();
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
                t = t * t * (3f - 2f * t); // Smoothstep easing
                
                sttPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            sttPanel.anchoredPosition = endPos;

            if (!showGroup && sttGroup != null)
            {
                if (fadeInCoroutine != null) StopCoroutine(fadeInCoroutine);
                if (popInCoroutine != null) StopCoroutine(popInCoroutine);
                sttGroup.SetActive(false);
            }
        }
    }
}
