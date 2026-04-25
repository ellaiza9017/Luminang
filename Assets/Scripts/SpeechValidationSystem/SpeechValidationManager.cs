using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace Luminang.SpeechValidation
{
    [RequireComponent(typeof(AudioRecorder))]
    [RequireComponent(typeof(WhisperAPIHandler))]
    public class SpeechValidationManager : MonoBehaviour
    {
        [Header("References")]
        public PhraseDatabase phraseDatabase;
        
        [Header("Settings")]
        public string openAIApiKey = ""; // Set your Groq API key in the Unity Inspector — never commit keys to Git!
        [Range(0f, 1f)]
        public float similarityThreshold = 0.7f;

        [Header("Events")]
        public UnityEvent<string> OnTranscriptionReceived;
        public UnityEvent<ValidPhrase, float> OnValidationSuccess;
        public UnityEvent<string> OnValidationFailed;
        public UnityEvent<string> OnError;

        private AudioRecorder audioRecorder;
        private WhisperAPIHandler whisperHandler;

        private void Awake()
        {
            audioRecorder = GetComponent<AudioRecorder>();
            whisperHandler = GetComponent<WhisperAPIHandler>();

            // Hook up recorder callback
            audioRecorder.OnRecordingStopped.AddListener(ProcessRecordedAudio);

            // Auto-populate database at runtime if it is empty or missing
            if (phraseDatabase == null)
            {
                phraseDatabase = ScriptableObject.CreateInstance<PhraseDatabase>();
                Debug.Log("SpeechValidationManager: No PhraseDatabase assigned. Creating runtime instance.");
            }

            if (phraseDatabase.phrases == null || phraseDatabase.phrases.Count == 0)
            {
                phraseDatabase.PopulateDefaultDataset();
                Debug.Log($"SpeechValidationManager: Auto-populated {phraseDatabase.phrases.Count} phrases at runtime.");
            }
            else
            {
                Debug.Log($"SpeechValidationManager: Loaded {phraseDatabase.phrases.Count} phrases from database.");
            }
        }

        public void StartRecording()
        {
            if (phraseDatabase == null || phraseDatabase.phrases.Count == 0)
            {
                OnError?.Invoke("Phrase database is missing or empty.");
                return;
            }
            audioRecorder.StartRecording();
        }

        public void StopRecording()
        {
            audioRecorder.StopRecording();
        }

        private void ProcessRecordedAudio(AudioClip clip)
        {
            Debug.Log("SpeechValidationManager: Received audio clip. Preparing to send to Whisper...");
            if (clip == null)
            {
                OnError?.Invoke("Recorded audio is null.");
                return;
            }

            byte[] wavData = WavUtility.FromAudioClip(clip);
            Debug.Log($"SpeechValidationManager: Converted to WAV, byte size: {wavData.Length}");
            StartCoroutine(whisperHandler.SendAudioToWhisper(wavData, openAIApiKey, OnWhisperSuccess, OnWhisperError));
        }

        private void OnWhisperSuccess(string transcribedText)
        {
            OnTranscriptionReceived?.Invoke(transcribedText);
            ValidateSpeech(transcribedText);
        }

        private void OnWhisperError(string error)
        {
            OnError?.Invoke(error);
        }

        private void ValidateSpeech(string spokenText)
        {
            ValidPhrase bestMatchPhrase = null;
            float highestSimilarity = 0f;

            foreach (var phrase in phraseDatabase.phrases)
            {
                float similarity = FuzzyMatcher.GetSimilarity(spokenText, phrase.NativeText);
                
                if (similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    bestMatchPhrase = phrase;
                }
            }

            if (highestSimilarity >= similarityThreshold && bestMatchPhrase != null)
            {
                Debug.Log($"[Validation Success] Match: {bestMatchPhrase.NativeText} ({highestSimilarity * 100:F1}%) -> Base: {bestMatchPhrase.EnglishBase}");
                OnValidationSuccess?.Invoke(bestMatchPhrase, highestSimilarity);
            }
            else
            {
                Debug.Log($"[Validation Failed] Input: {spokenText}. Highest Match: {(bestMatchPhrase != null ? bestMatchPhrase.NativeText : "None")} ({highestSimilarity * 100:F1}%)");
                OnValidationFailed?.Invoke("Phrase not recognized or similarity too low. Please try again.");
            }
        }
    }
}
