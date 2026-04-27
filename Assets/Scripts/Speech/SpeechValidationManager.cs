using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Android;
using System.Linq;

namespace Luminang.Speech
{
    [Serializable] public class SuccessEvent : UnityEvent<Phrase, string, float> { }
    [Serializable] public class FailedDetailedEvent : UnityEvent<string, string> { }

    public class SpeechValidationManager : MonoBehaviour
    {
        [Header("OpenAI Configuration")]
        [SerializeField] private string apiKey = "";
        
        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float similarityThreshold = 0.7f;
        [SerializeField] private int recordingDuration = 5;

        [Header("Data")]
        [SerializeField] private PhraseDatabase phraseDatabase;

        [Header("Debug / Testing")]
        [SerializeField] private string debugSimulationText = "naimbag a bigat";

        [Header("Events")]
        public UnityEvent OnRecordingStarted;
        public UnityEvent OnRecordingEnded;
        public UnityEvent<string> OnTranscriptionReceived;
        public SuccessEvent OnValidationSuccess; // Phrase, Language, Similarity
        public FailedDetailedEvent OnValidationFailedDetailed; // Heard, ClosestPhrase, Similarity

        private OpenAIWhisperService whisperService;
        private AudioClip recordingClip;
        private bool isRecording = false;

        private void Awake()
        {
            whisperService = new OpenAIWhisperService(apiKey);
            
            if (phraseDatabase == null)
            {
                Debug.LogWarning("PhraseDatabase is missing! Please assign one in the inspector.");
            }

            #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
            #endif
        }

        public void StartRecording()
        {
            if (isRecording) return;
            
            // Check if microphone is available
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone detected!");
                return;
            }

            Debug.Log("Recording started...");
            isRecording = true;
            OnRecordingStarted?.Invoke();
            
            recordingClip = Microphone.Start(null, false, recordingDuration, 44100);
            
            StartCoroutine(WaitAndProcess());
        }

        private IEnumerator WaitAndProcess()
        {
            float timer = 0;
            while (timer < recordingDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            Microphone.End(null);
            isRecording = false;
            OnRecordingEnded?.Invoke();
            Debug.Log("Recording ended. Transcribing...");

            byte[] wavData = WavUtility.FromAudioClip(recordingClip);
            
            StartCoroutine(whisperService.Transcribe(wavData, OnTranscriptionSuccess, OnTranscriptionError));
        }

        private void OnTranscriptionSuccess(string transcribedText)
        {
            Debug.Log($"Transcribed: {transcribedText}");
            OnTranscriptionReceived?.Invoke(transcribedText);
            ValidatePhrase(transcribedText);
        }

        private void OnTranscriptionError(string error)
        {
            Debug.LogError($"Transcription Error: {error}");
            OnValidationFailedDetailed?.Invoke("API Error", error);
        }

        private void ValidatePhrase(string text)
        {
            if (phraseDatabase == null || phraseDatabase.phrases.Count == 0)
            {
                Debug.LogError("No phrases to validate against.");
                return;
            }

            Phrase bestMatch = null;
            float highestSimilarity = 0f;
            string matchedLanguage = "";

            // Pre-normalize the incoming text
            string normalizedInput = FuzzyMatcher.Normalize(text);

            foreach (var phrase in phraseDatabase.phrases)
            {
                // Check all supported languages
                string[] languages = { "ilokano", "cebuano", "maranao" };
                foreach (var lang in languages)
                {
                    string targetPhrase = phrase.GetTextByLanguage(lang);
                    if (string.IsNullOrEmpty(targetPhrase)) continue;

                    float similarity = FuzzyMatcher.GetSimilarity(normalizedInput, targetPhrase);
                    
                    if (similarity > highestSimilarity)
                    {
                        highestSimilarity = similarity;
                        bestMatch = phrase;
                        matchedLanguage = lang;
                    }
                }
            }

            if (highestSimilarity >= similarityThreshold)
            {
                Debug.Log($"<color=green>SUCCESS!</color> Matched: '{bestMatch.english}' via {matchedLanguage} ({highestSimilarity:P0})");
                OnValidationSuccess?.Invoke(bestMatch, matchedLanguage, highestSimilarity);
            }
            else
            {
                Debug.Log($"<color=red>REJECTED.</color> No match found for '{text}'. Best similarity: {highestSimilarity:P0}");
                string expectedText = bestMatch != null ? bestMatch.english : "No match found";
                OnValidationFailedDetailed?.Invoke(text, expectedText);
            }
        }

        #region Initialization Utility
        [ContextMenu("Populate Default Dataset")]
        public void PopulateDataset()
        {
            if (phraseDatabase == null)
            {
                Debug.LogError("Assign a PhraseDatabase ScriptableObject first!");
                return;
            }

            phraseDatabase.phrases.Clear();
            
            // GREETINGS
            AddPhrase("Good morning", "Naimbag a bigat", "Maayong buntag", "Mapia a kapipita", PhraseCategory.Greetings);
            AddPhrase("Good afternoon", "Naimbag a malem", "Maayong hapon", "Mapia a kaapon", PhraseCategory.Greetings);
            AddPhrase("Good evening", "Naimbag a rabii", "Maayong gabii", "Mapia a kagabii", PhraseCategory.Greetings);
            AddPhrase("How are you?", "Kumusta ka?", "Kumusta ka?", "Kapya ka?", PhraseCategory.Greetings);
            AddPhrase("I’m doing well", "Nasayaatak", "Maayo ra ko", "Mapia ako", PhraseCategory.Greetings);

            // IDENTITY
            AddPhrase("What is your name?", "Ania ti nagan mo?", "Unsay imong ngalan?", "Ngai ngaran ka?", PhraseCategory.Identity);
            AddPhrase("My name is ___", "Ti nagan ko ket", "Ako si", "So ngaran ko si", PhraseCategory.Identity);
            AddPhrase("Where are you from?", "Taga sadino ka?", "Taga asa ka?", "Taga anda ka?", PhraseCategory.Identity);
            AddPhrase("I am from ___", "Taga ak", "Taga ko", "Taga ako", PhraseCategory.Identity);

            // REQUESTS
            AddPhrase("Can you help me?", "Mabalin kadi a tulunganak?", "Pwede ko nimo tabangan?", "Mapakay ka tabanga ako?", PhraseCategory.Requests);
            AddPhrase("Please help me", "Tulunganak man", "Tabangi ko palihug", "Tabanga ako", PhraseCategory.Requests);
            AddPhrase("Please wait for me", "Urayennak man", "Hulata ko palihug", "Antay ako", PhraseCategory.Requests);
            AddPhrase("Can I ask something?", "Mabalin kadi agsaludsod?", "Pwede ko mangutana?", "Pwede ako magtanong?", PhraseCategory.Requests);

            // DIRECTIONS
            AddPhrase("Please go straight", "Agdiretso ka man", "Padayon lang palihug", "Diretsu lang", PhraseCategory.Directions);
            AddPhrase("Please turn left", "Agliko ka iti kannigid", "Liko sa wala palihug", "Liko sa wala", PhraseCategory.Directions);
            AddPhrase("Please turn right", "Agliko ka iti kannawan", "Liko sa tuo palihug", "Liko sa tuo", PhraseCategory.Directions);
            AddPhrase("Please go up", "Umuli ka iti ngato", "Saka pataas palihug", "Saka pataas", PhraseCategory.Directions);
            AddPhrase("Please go down", "Bumaba ka man", "Naog paubos palihug", "Manaog", PhraseCategory.Directions);
            AddPhrase("Please stop here", "Agsardeng ka ditoy", "Hunong diri palihug", "Hinto dito", PhraseCategory.Directions);
            AddPhrase("Please come here", "Umay ka ditoy man", "Ari diri palihug", "Diri ka", PhraseCategory.Directions);
            AddPhrase("Please go there", "Mapan ka idiay man", "Adto didto palihug", "Lakad doon", PhraseCategory.Directions);
            AddPhrase("Please follow me", "Surotennak man", "Sunda ko palihug", "Sumunod ka", PhraseCategory.Directions);
            AddPhrase("Please wait here", "Uray ka ditoy man", "Hulata diri palihug", "Antay dito", PhraseCategory.Directions);

            // GRATITUDE
            AddPhrase("Thank you very much", "Agyamanak unay", "Daghang salamat", "Mapiya salamat", PhraseCategory.Gratitude);
            AddPhrase("Thank you for your help", "Agyamanak iti tulong mo", "Salamat sa imong tabang", "Salamat sa tulong mo", PhraseCategory.Gratitude);
            AddPhrase("I am sorry", "Pakawanen nak", "Pasayloa ko", "Pasensya ako", PhraseCategory.Gratitude);
            AddPhrase("Excuse me please", "Pakawanen nak man", "Pasayloa ko palihug", "Tabi lang", PhraseCategory.Gratitude);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(phraseDatabase);
            #endif
            
            Debug.Log("Dataset populated successfully!");
        }

        [ContextMenu("Simulate Input")]
        public void SimulateInput()
        {
            Debug.Log($"Simulating input: {debugSimulationText}");
            OnTranscriptionSuccess(debugSimulationText);
        }

        private void AddPhrase(string eng, string ilo, string ceb, string mar, PhraseCategory cat)
        {
            phraseDatabase.phrases.Add(new Phrase { 
                english = eng, ilokano = ilo, cebuano = ceb, maranao = mar, category = cat 
            });
        }
        #endregion
    }
}
