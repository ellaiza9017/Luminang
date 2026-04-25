using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming you are using TextMeshPro

namespace Luminang.SpeechValidation
{
    public class SpeechValidationTester : MonoBehaviour
    {
        [Header("References")]
        public SpeechValidationManager speechManager;
        
        [Header("UI Elements")]
        public Button recordButton;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI resultText;

        private void Start()
        {
            if (speechManager == null)
                speechManager = FindObjectOfType<SpeechValidationManager>();

            if (recordButton != null)
            {
                // Hook up the button to start recording
                recordButton.onClick.AddListener(OnRecordButtonPressed);
            }

            // Hook up the manager events to update our UI text
            speechManager.OnTranscriptionReceived.AddListener(OnTranscription);
            speechManager.OnValidationSuccess.AddListener(OnSuccess);
            speechManager.OnValidationFailed.AddListener(OnFailed);
            speechManager.OnError.AddListener(OnError);

            UpdateStatus("Ready to record. Press the button.");
        }

        private void OnRecordButtonPressed()
        {
            UpdateStatus("Recording for 5 seconds... Speak now!");
            resultText.text = "";
            speechManager.StartRecording();
        }

        private void OnTranscription(string text)
        {
            UpdateStatus("OpenAI Heard: " + text);
        }

        private void OnSuccess(ValidPhrase phrase, float similarity)
        {
            resultText.color = Color.green;
            resultText.text = $"ACCEPTED!\n\nSpoken: {phrase.NativeText}\nEnglish: {phrase.EnglishBase}\nMatch: {similarity * 100:F1}%\nCategory: {phrase.Category}";
            UpdateStatus("Validation Complete.");
        }

        private void OnFailed(string reason)
        {
            resultText.color = Color.red;
            resultText.text = "REJECTED!\n\n" + reason;
            UpdateStatus("Validation Complete.");
        }

        private void OnError(string errorMsg)
        {
            resultText.color = Color.red;
            resultText.text = "ERROR: " + errorMsg;
            UpdateStatus("An error occurred.");
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
