using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Settings")]
    public AudioMixer mainMixer; // Optional, set in Inspector if used
    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    [Header("Runtime State")]
    public float musicVolume = 1.0f;
    public float sfxVolume = 1.0f;

    public delegate void OnVolumeChange();
    public static event OnVolumeChange onMusicVolumeChange;
    public static event OnVolumeChange onSFXVolumeChange;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ApplyMusicVolume(float value)
    {
        musicVolume = value;
        
        // Auto-save immediately
        PlayerPrefs.SetFloat(MUSIC_KEY, musicVolume);
        
        if (mainMixer != null)
            mainMixer.SetFloat("MusicVolume", LinearToDecibel(musicVolume));
            
        onMusicVolumeChange?.Invoke();
    }

    private AudioSource uiSfxSource;

    public void ApplySFXVolume(float value)
    {
        sfxVolume = value;

        // Auto-save immediately
        PlayerPrefs.SetFloat(SFX_KEY, sfxVolume);

        if (mainMixer != null)
            mainMixer.SetFloat("SFXVolume", LinearToDecibel(sfxVolume));

        onSFXVolumeChange?.Invoke();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        
        if (uiSfxSource == null)
        {
            uiSfxSource = gameObject.AddComponent<AudioSource>();
            uiSfxSource.playOnAwake = false;
            // Add SFXVolumeSync so it respects the volume sliders
            if (gameObject.GetComponent<SFXVolumeSync>() == null)
                gameObject.AddComponent<SFXVolumeSync>();
        }

        Debug.Log($"[AudioManager] Playing '{clip.name}' on uiSfxSource. Volume is currently: {uiSfxSource.volume}");
        uiSfxSource.PlayOneShot(clip);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MUSIC_KEY, musicVolume);
        PlayerPrefs.SetFloat(SFX_KEY, sfxVolume);
        PlayerPrefs.Save();
        Debug.Log("[AudioManager] Settings SAVED to disk.");
    }

    public void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 0.75f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 0.75f);
        
        // Apply immediately
        ApplyMusicVolume(musicVolume);
        ApplySFXVolume(sfxVolume);
        Debug.Log("[AudioManager] Settings LOADED from disk.");
    }

    private float LinearToDecibel(float linear)
    {
        float dB;
        if (linear != 0)
            dB = 20.0f * Mathf.Log10(linear);
        else
            dB = -80.0f;
        return dB;
    }
}
