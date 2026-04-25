using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace Luminang.SpeechValidation
{
    public class WhisperAPIHandler : MonoBehaviour
    {
        private const string WHISPER_API_URL = "https://api.groq.com/openai/v1/audio/transcriptions";

        // JSON mapping for OpenAI response
        [System.Serializable]
        private class WhisperResponse
        {
            public string text;
        }

        public IEnumerator SendAudioToWhisper(byte[] audioData, string apiKey, System.Action<string> onSuccess, System.Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("OpenAI API Key is missing.");
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", audioData, "audio.wav", "audio/wav");
            form.AddField("model", "whisper-large-v3");

            using (UnityWebRequest request = UnityWebRequest.Post(WHISPER_API_URL, form))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Whisper API Error: {request.error}\n{request.downloadHandler.text}");
                    onError?.Invoke(request.error);
                }
                else
                {
                    string responseText = request.downloadHandler.text;
                    try
                    {
                        WhisperResponse responseJson = JsonUtility.FromJson<WhisperResponse>(responseText);
                        onSuccess?.Invoke(responseJson.text);
                    }
                    catch (System.Exception e)
                    {
                        onError?.Invoke("Failed to parse JSON response: " + e.Message);
                    }
                }
            }
        }
    }
}
