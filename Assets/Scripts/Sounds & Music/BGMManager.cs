using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }
    private AudioSource audioSource;

    [System.Serializable]
    public class SceneBGM
    {
        [Tooltip("Part of the scene name to match (case-insensitive). E.g. 'fishing', 'mainmenu'")]
        public string sceneNameContains;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volumeMultiplier = 1f;
    }

    [Header("Per-Scene BGM")]
    [Tooltip("Map scene names to BGM clips. The first match wins.")]
    public List<SceneBGM> sceneBGMList = new List<SceneBGM>();

    [Header("Default BGM")]
    [Tooltip("Plays on any scene that doesn't match the list above")]
    public AudioClip defaultClip;
    [Range(0f, 1f)]
    public float defaultVolumeMultiplier = 1f;

    [Header("Crossfade")]
    public float crossfadeDuration = 1f;

    private float currentVolumeMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            
            UpdateVolume();

            SceneManager.sceneLoaded += OnSceneLoaded;
            AudioManager.onMusicVolumeChange += UpdateVolume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        AudioClip targetClip = null;
        float targetMultiplier = defaultVolumeMultiplier;

        // Find the first matching scene BGM entry
        foreach (var entry in sceneBGMList)
        {
            if (!string.IsNullOrEmpty(entry.sceneNameContains) &&
                sceneName.Contains(entry.sceneNameContains.ToLower()))
            {
                targetClip = entry.clip;
                targetMultiplier = entry.volumeMultiplier;
                break;
            }
        }

        // Fall back to the default clip if no match
        if (targetClip == null)
        {
            targetClip = defaultClip;
            targetMultiplier = defaultVolumeMultiplier;
        }

        // If no clip is mapped at all, stop the music
        if (targetClip == null)
        {
            audioSource.Stop();
            return;
        }

        // If it's already playing the correct track, just update multiplier if it changed
        if (audioSource.clip == targetClip && audioSource.isPlaying)
        {
            currentVolumeMultiplier = targetMultiplier;
            UpdateVolume();
            return;
        }

        // Crossfade to the new track
        StartCoroutine(CrossfadeTo(targetClip, targetMultiplier));
    }

    private IEnumerator CrossfadeTo(AudioClip newClip, float newMultiplier)
    {
        float startVolume = audioSource.volume;

        // Fade out
        yield return FadeCoroutine(0f, crossfadeDuration / 2f);

        audioSource.clip = newClip;
        currentVolumeMultiplier = newMultiplier; // Set new base volume
        audioSource.Play();

        // Target volume based on AudioManager * our new scene multiplier
        float finalVolume = (AudioManager.instance != null ? AudioManager.instance.musicVolume : 1f) * currentVolumeMultiplier;
        
        // Fade back in
        yield return FadeCoroutine(finalVolume, crossfadeDuration / 2f);
    }

    public void UpdateVolume()
    {
        if (audioSource != null)
        {
            float playerVol = AudioManager.instance != null ? AudioManager.instance.musicVolume : PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            // The actual volume is the player's setting (0-1) MULTIPLIED by the scene's setting (0-1)
            audioSource.volume = playerVol * currentVolumeMultiplier;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioManager.onMusicVolumeChange -= UpdateVolume;
        }
    }

    public void FadeVolume(float targetVolume, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeCoroutine(targetVolume, duration));
    }

    private IEnumerator FadeCoroutine(float targetVolume, float duration)
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}
