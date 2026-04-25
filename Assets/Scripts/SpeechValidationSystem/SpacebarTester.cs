using UnityEngine;

namespace Luminang.SpeechValidation
{
    public class SpacebarTester : MonoBehaviour
    {
        private SpeechValidationManager manager;

        private void Start()
        {
            manager = FindObjectOfType<SpeechValidationManager>();
            
            // Connect to the events so we can print the result to the Unity Console
            manager.OnTranscriptionReceived.AddListener(text => Debug.Log($"[HEARD]: {text}"));
            manager.OnValidationSuccess.AddListener((phrase, sim) => Debug.Log($"<color=green>[ACCEPTED]</color> You said: {phrase.NativeText} ({sim * 100:F1}% match)"));
            manager.OnValidationFailed.AddListener(error => Debug.Log($"<color=red>[REJECTED]</color> {error}"));
            manager.OnError.AddListener(error => Debug.LogError($"<color=red>[ERROR]</color> {error}"));
        }

        private void Update()
        {
            // When you press the Spacebar, it will start recording!
            // Updated to use the new Unity Input System
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>🎤 SPACEBAR PRESSED! Recording for 5 seconds... SPEAK NOW!</color>");
                manager.StartRecording();
            }
        }
    }
}
