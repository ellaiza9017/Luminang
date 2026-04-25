using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace Luminang.SpeechValidation
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioRecorder : MonoBehaviour
    {
        [Header("Recording Settings")]
        public int maxRecordingDuration = 5; // 5 seconds
        public int sampleRate = 44100;

        private AudioClip recordedClip;
        private string microphoneDevice;
        private bool isRecording = false;

        [Header("Events")]
        public UnityEvent OnRecordingStarted;
        public UnityEvent<AudioClip> OnRecordingStopped;

        private void Start()
        {
            if (Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
            }
            else
            {
                Debug.LogError("No microphone detected!");
            }
        }

        public void StartRecording()
        {
            if (isRecording) return;
            if (string.IsNullOrEmpty(microphoneDevice))
            {
                Debug.LogError("AudioRecorder: Cannot start, microphone device is empty.");
                return;
            }

            Debug.Log($"AudioRecorder: Starting recording with device '{microphoneDevice}'...");
            recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingDuration, sampleRate);
            if (recordedClip == null)
            {
                Debug.LogError("AudioRecorder: Microphone.Start returned null!");
                return;
            }

            isRecording = true;
            OnRecordingStarted?.Invoke();
            
            // Automatically stop after max duration
            StartCoroutine(AutoStopRecording());
        }

        public void StopRecording()
        {
            if (!isRecording) return;

            Debug.Log("AudioRecorder: StopRecording called. Processing audio data...");
            Microphone.End(microphoneDevice);
            isRecording = false;
            StopAllCoroutines();
            
            // Trim the clip to actual length recorded
            int lastPosition = Microphone.GetPosition(microphoneDevice);
            if (lastPosition > 0)
            {
                float[] samples = new float[lastPosition * recordedClip.channels];
                recordedClip.GetData(samples, 0);
                AudioClip trimmedClip = AudioClip.Create("RecordedAudio", lastPosition, recordedClip.channels, recordedClip.frequency, false);
                trimmedClip.SetData(samples, 0);
                recordedClip = trimmedClip;
                Debug.Log($"AudioRecorder: Trimmed clip to {lastPosition} samples.");
            }
            else
            {
                Debug.LogWarning("AudioRecorder: lastPosition was 0! No audio recorded.");
            }

            Debug.Log("AudioRecorder: Firing OnRecordingStopped event.");
            OnRecordingStopped?.Invoke(recordedClip);
        }

        private IEnumerator AutoStopRecording()
        {
            yield return new WaitForSeconds(maxRecordingDuration);
            if (isRecording)
            {
                StopRecording();
            }
        }
    }
}
