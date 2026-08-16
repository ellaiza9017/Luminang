#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

// Duplicate RegionMode removed – use the definition in RegionFlowController.cs

public class PhraseEvaluator : MonoBehaviour
{
    public static PhraseEvaluator Instance { get; private set; }
    public RegionMode CurrentRegion { get; private set; } = RegionMode.Ilokano;
    public string CurrentCategory { get; set; } = "";

    private const string BACKEND_URL = "https://luminang-nlp-service.onrender.com";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetRegion(RegionMode mode)
    {
        CurrentRegion = mode;
        Debug.Log($"Speech Region set to: {mode}");
    }

    public float CalculateAccuracy(string input, string target)
    {
        Debug.LogWarning("CalculateAccuracy is deprecated. Use EvaluateSpeech instead.");
        return 0f;
    }

    public string GetFeedback(float accuracy)
    {
        if (accuracy >= 90f) return "Perfect match! Well done!";
        if (accuracy >= 80f) return "Excellent! You passed!";
        if (accuracy >= 65f) return "Great! You're getting it.";
        if (accuracy >= 45f) return "Good effort! Try again.";
        if (accuracy >= 25f) return "Not quite, but you're close.";
        return "Keep practicing!";
    }

    // New Async/Coroutine evaluations

    public void EvaluateSpeech(string expectedPhrase, string transcribedText, Action<string, float, string> callback)
    {
        StartCoroutine(EvaluateSpeechCoroutine(expectedPhrase, transcribedText, callback));
    }

    private IEnumerator EvaluateSpeechCoroutine(string expectedPhrase, string transcribedText, Action<string, float, string> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("expected_phrase", expectedPhrase);
        form.AddField("transcribed_text", transcribedText);
        if (!string.IsNullOrEmpty(CurrentCategory)) form.AddField("category", CurrentCategory);

        using (UnityWebRequest request = UnityWebRequest.Post($"{BACKEND_URL}/evaluate", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<EvaluateResponse>(request.downloadHandler.text);
                callback?.Invoke(response.transcript, response.score * 100.0f, response.result); // Scale to 0-100%
            }
            else
            {
                Debug.LogError($"Evaluation API Error: {request.error}\n{request.downloadHandler.text}");
                callback?.Invoke(transcribedText, 0f, "try_again");
            }
        }
    }

    public void FindBestMatch(string transcribedText, Action<PhraseEntry, string, float, bool, string> callback)
    {
        StartCoroutine(FindBestMatchCoroutine(transcribedText, callback));
    }

    private IEnumerator FindBestMatchCoroutine(string transcribedText, Action<PhraseEntry, string, float, bool, string> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("region", CurrentRegion.ToString());
        form.AddField("transcribed_text", transcribedText);
        if (!string.IsNullOrEmpty(CurrentCategory)) form.AddField("category", CurrentCategory);

        using (UnityWebRequest request = UnityWebRequest.Post($"{BACKEND_URL}/find_best_match", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                var response = JsonUtility.FromJson<BestMatchResponse>(json);
                if (response.best_entry != null)
                {
                    PhraseEntry entry = null;
                    if (DatasetManager.Instance != null)
                    {
                        entry = DatasetManager.Instance.GetAllPhrases().Find(p => p.english == response.best_entry.english);
                    }
                    if (entry == null) entry = response.best_entry;
                    callback?.Invoke(entry, response.language, response.score * 100.0f, response.is_english, response.result);
                }
                else
                {
                    callback?.Invoke(null, "", 0f, false, "fail");
                }
            }
            else
            {
                Debug.LogError($"FindBestMatch API Error: {request.error}\n{request.downloadHandler.text}");
                callback?.Invoke(null, "", 0f, false, "fail");
            }
        }
    }

    public void FindAllMatches(string transcribedText, Action<List<(PhraseEntry entry, string language, float score)>> callback)
    {
        StartCoroutine(FindAllMatchesCoroutine(transcribedText, callback));
    }

    private IEnumerator FindAllMatchesCoroutine(string transcribedText, Action<List<(PhraseEntry entry, string language, float score)>> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("region", CurrentRegion.ToString());
        form.AddField("transcribed_text", transcribedText);
        if (!string.IsNullOrEmpty(CurrentCategory)) form.AddField("category", CurrentCategory);

        using (UnityWebRequest request = UnityWebRequest.Post($"{BACKEND_URL}/find_all_matches", form))
        {
            yield return request.SendWebRequest();

            var matches = new List<(PhraseEntry entry, string language, float score)>();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                var response = JsonUtility.FromJson<AllMatchesResponse>(json);
                if (response.matches != null)
                {
                    foreach (var item in response.matches)
                    {
                        if (item.entry != null)
                        {
                            PhraseEntry entry = DatasetManager.Instance.GetAllPhrases().Find(p => p.english == item.entry.english);
                            if (entry == null) entry = item.entry;
                            matches.Add((entry, item.language, item.score)); // Python already scales score for find_all_matches
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"FindAllMatches API Error: {request.error}\n{request.downloadHandler.text}");
            }

            callback?.Invoke(matches);
        }
    }

    public void ParseIntent(string transcribedText, string allowedIntents, string contextPrompt, Action<IntentResponse> callback)
    {
        StartCoroutine(ParseIntentCoroutine(transcribedText, allowedIntents, contextPrompt, callback));
    }

    private IEnumerator ParseIntentCoroutine(string transcribedText, string allowedIntents, string contextPrompt, Action<IntentResponse> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("transcribed_text", transcribedText);
        form.AddField("allowed_intents", allowedIntents);
        form.AddField("context_prompt", contextPrompt);

        using (UnityWebRequest request = UnityWebRequest.Post($"{BACKEND_URL}/parse_intent", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                var response = JsonUtility.FromJson<IntentResponse>(json);
                callback?.Invoke(response);
            }
            else
            {
                Debug.LogError($"ParseIntent API Error: {request.error}\n{request.downloadHandler.text}");
                callback?.Invoke(new IntentResponse { intent = "Error", transcript = transcribedText, slots_json = "{}" });
            }
        }
    }

    // Helper classes for JSON Deserialization

    [Serializable]
    public class EvaluateResponse
    {
        public string transcript;
        public float score;
        public string result;
        public float exact_score;
        public float lexical_score;
        public float phonetic_score;
        public float semantic_score;
        public float template_score;
        public float final_confidence;
    }

    [Serializable]
    public class BestMatchResponse
    {
        public string transcript;
        public PhraseEntry best_entry;
        public string language;
        public float score;
        public bool is_english;
        public string result;
        public float exact_score;
        public float lexical_score;
        public float phonetic_score;
        public float semantic_score;
        public float template_score;
        public float final_confidence;
    }

    [Serializable]
    private class MatchItem
    {
        public PhraseEntry entry;
        public string language;
        public float score;
    }

    [Serializable]
    private class AllMatchesResponse
    {
        public string transcript;
        public List<MatchItem> matches;
    }

    [Serializable]
    public class IntentResponse
    {
        public string transcript;
        public string intent;
        public string slots_json;
    }
}
