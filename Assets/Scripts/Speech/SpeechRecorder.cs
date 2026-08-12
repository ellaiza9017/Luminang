using System.Collections;
using System.IO;
using UnityEngine;
using System;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class SpeechRecorder : MonoBehaviour
{
    public static SpeechRecorder Instance { get; private set; }

    private string _deviceName;
    private AudioClip _recording;
    private bool _isRecording = false;
    private float _startTime;

    public bool IsRecording => _isRecording;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Don't initialize microphone here; do it in Start or when needed
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        RequestPermissions();
    }

    private void RequestPermissions()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            // Pick first non-empty device
            _deviceName = Microphone.devices[0];
            Debug.Log($"[SpeechRecorder] Microphone initialized: '{_deviceName}' (Total available devices: {Microphone.devices.Length})");
        }
        else
        {
            Debug.LogError("[SpeechRecorder] No microphone device detected on this system!");
            _deviceName = null;
        }
    }

    public void StartRecording()
    {
        InitializeMicrophone();

        if (string.IsNullOrEmpty(_deviceName))
        {
            Debug.LogError("[SpeechRecorder] Cannot start recording: No microphone device found.");
            return;
        }
        
        _recording = Microphone.Start(_deviceName, false, 10, 16000); // 16kHz for Whisper
        _isRecording = true;
        _startTime = Time.time;
        Debug.Log($"[SpeechRecorder] Recording started on '{_deviceName}'...");
    }

    public string StopRecording()
    {
        if (!_isRecording) return null;

        int position = Microphone.GetPosition(_deviceName);
        Microphone.End(_deviceName);
        _isRecording = false;

        Debug.Log($"[SpeechRecorder] StopRecording called. Position: {position}, Device: '{_deviceName}'");

        if (position <= 0)
        {
            if (_recording != null && _recording.samples > 0)
            {
                position = Mathf.Min(_recording.samples, (int)(16000 * (Time.time - _startTime)));
            }
        }

        if (position <= 0)
        {
            Debug.LogWarning("[SpeechRecorder] Failed to capture microphone audio: Position was 0. Check microphone permissions or input device settings.");
            return null;
        }

        // Trim the clip to actual recorded length
        AudioClip trimmedClip = AudioClip.Create("TrimmedClip", position, _recording.channels, _recording.frequency, false);
        float[] data = new float[position * _recording.channels];
        _recording.GetData(data, 0);
        trimmedClip.SetData(data, 0);

        string filePath = Path.Combine(Application.persistentDataPath, "speech.wav");
        SaveAsWav(trimmedClip, filePath);
        
        Debug.Log($"[SpeechRecorder] Recording saved successfully to {filePath}");
        return filePath;
    }

    private void SaveAsWav(AudioClip clip, string filePath)
    {
        byte[] wavData = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(filePath, wavData);
    }
}

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                var samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples.Length * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((short)(clip.channels * 2));
                writer.Write((short)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * 32767));
                }
            }
            return stream.ToArray();
        }
    }
}
