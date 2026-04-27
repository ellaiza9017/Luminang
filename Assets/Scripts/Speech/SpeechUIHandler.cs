using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Luminang.Speech
{
    public class SpeechUISettings : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI transcriptionText;
        public TextMeshProUGUI resultText;
        public Button recordButton;
        public Image recordingIndicator;

        [Header("Manager Reference")]
        public SpeechValidationManager speechManager;

        private void Start()
        {
            if (speechManager == null) speechManager = FindFirstObjectByType<SpeechValidationManager>();
            
            // Subscribe to events
            if (speechManager != null)
            {
                speechManager.OnRecordingStarted.AddListener(OnRecordingStarted);
                speechManager.OnRecordingEnded.AddListener(OnRecordingEnded);
                speechManager.OnTranscriptionReceived.AddListener(OnTranscriptionReceived);
                speechManager.OnValidationSuccess.AddListener(OnValidationSuccess);
                speechManager.OnValidationFailedDetailed.AddListener(OnValidationFailedDetailed);
            }

            UpdateStatus("Ready to Record");
            if (recordingIndicator != null) recordingIndicator.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Simple pulse animation for recording indicator
            if (recordingIndicator != null && recordingIndicator.gameObject.activeSelf)
            {
                float alpha = (Mathf.Sin(Time.time * 8f) + 1f) / 2f;
                recordingIndicator.color = new Color(1, 0, 0, alpha);
            }
        }

        public void OnRecordingStarted()
        {
            UpdateStatus("<color=red>● RECORDING</color>");
            if (transcriptionText != null) transcriptionText.text = "...";
            if (resultText != null) resultText.text = "";
            if (recordingIndicator != null) recordingIndicator.gameObject.SetActive(true);
            if (recordButton != null) recordButton.interactable = false;
        }

        public void OnRecordingEnded()
        {
            UpdateStatus("Processing Audio...");
            if (recordingIndicator != null) recordingIndicator.gameObject.SetActive(false);
        }

        public void OnTranscriptionReceived(string text)
        {
            if (transcriptionText != null) transcriptionText.text = $"Heard: <i>\"{text}\"</i>";
        }

        public void OnValidationSuccess(Phrase phrase, string language, float similarity)
        {
            UpdateStatus("<color=green>✔ MATCH FOUND</color>");
            if (resultText != null) 
            {
                resultText.text = $"<b>Match:</b> {phrase.english}\n" +
                                 $"<b>Language:</b> {language}\n" +
                                 $"<b>Confidence:</b> {similarity:P0}";
            }
            if (recordButton != null) recordButton.interactable = true;
        }

        public void OnValidationFailedDetailed(string heardText, string expectedText)
        {
            UpdateStatus("<color=red>✘ NO MATCH</color>");
            if (resultText != null) 
            {
                resultText.text = $"<color=red><b>Heard:</b> \"{heardText}\"</color>\n" +
                                 $"<color=yellow><b>Closest Match:</b> \"{expectedText}\"</color>\n" +
                                 $"<i>Try speaking closer to the mic!</i>";
            }
            if (recordButton != null) recordButton.interactable = true;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
