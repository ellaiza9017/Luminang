using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;
    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton pattern: Ensure only one instance of BGMManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            audioSource = GetComponent<AudioSource>();
            
            // Init volume
            UpdateVolume();

            // Start listening for scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Start listening for volume changes
            AudioManager.onMusicVolumeChange += UpdateVolume;
        }
        else
        {
            // If another instance already exists, destroy this duplicate
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string name = scene.name.ToLower();
        
        // KEEP music playing in gameplay (Removed stop logic)
        if (name.Contains("sample") || name.Contains("game"))
        {
             // audioSource.Stop(); 
        }
        else
        {
            // For all other scenes (MainMenu, Login, Signup, Loading, etc.)
            // Keep the music running!
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    private void UpdateVolume()
    {
        if (audioSource != null && AudioManager.instance != null)
        {
            audioSource.volume = AudioManager.instance.musicVolume;
        }
    }

    private void OnDestroy()
    {
        // Clean up event listener when destroyed
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioManager.onMusicVolumeChange -= UpdateVolume;
        }
    }
}
