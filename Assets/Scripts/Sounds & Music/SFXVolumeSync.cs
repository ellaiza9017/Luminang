using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXVolumeSync : MonoBehaviour
{
    private AudioSource audioSource;
    private float baseVolume = 1.0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        baseVolume = audioSource.volume; // Capture the designed volume (e.g., 0.5f)
    }

    private void Start()
    {
        UpdateVolume();
        AudioManager.onSFXVolumeChange += UpdateVolume;
    }

    public void UpdateVolume()
    {
        if (audioSource != null)
        {
            // Fallback to PlayerPrefs if we are testing directly in a scene without AudioManager
            float sfxVol = AudioManager.instance != null ? AudioManager.instance.sfxVolume : PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            
            // Final volume = (Designed Volume) * (Slider Percentage)
            audioSource.volume = baseVolume * sfxVol;
        }
    }

    private void OnDestroy()
    {
        AudioManager.onSFXVolumeChange -= UpdateVolume;
    }
}
