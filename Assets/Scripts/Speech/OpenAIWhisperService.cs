using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Luminang.Speech
{
    public class OpenAIWhisperService
    {
        private const string API_URL = "https://api.groq.com/openai/v1/audio/transcriptions";
        private string apiKey;

        public OpenAIWhisperService(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public IEnumerator Transcribe(byte[] audioData, Action<string> onSuccess, Action<string> onError)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", audioData, "audio.wav", "audio/wav");
            form.AddField("model", "whisper-large-v3");
            // Optional: Specify language to improve accuracy for multilingual input
            // form.AddField("language", "tl"); // Using Tagalog as a proxy for better local dialect detection if needed

            using (UnityWebRequest www = UnityWebRequest.Post(API_URL, form))
            {
                www.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(www.error + " : " + www.downloadHandler.text);
                }
                else
                {
                    var response = JsonUtility.FromJson<WhisperResponse>(www.downloadHandler.text);
                    onSuccess?.Invoke(response.text);
                }
            }
        }

        [Serializable]
        private class WhisperResponse
        {
            public string text;
        }
    }
}
