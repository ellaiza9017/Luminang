using UnityEngine;
using UnityEngine.UI;

// Attach this to any Button or Toggle GameObject.
// Drag your click clip into the Inspector slot.
public class ButtonSFX : MonoBehaviour
{
    [Tooltip("Leave this blank to use the global UI audio source, or assign one if you want 3D spatial audio.")]
    public AudioSource sfxSource;
    public AudioClip clickSFX;

    void Start()
    {
        // Check if it's a Button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayClick);
        }

        // Check if it's a Toggle instead
        Toggle tgl = GetComponent<Toggle>();
        if (tgl != null)
        {
            tgl.onValueChanged.AddListener((bool isOn) => PlayClick());
        }
    }

    void PlayClick()
    {
        if (clickSFX == null) 
        {
            Debug.LogWarning("[ButtonSFX] Clicked, but clickSFX is missing!");
            return;
        }

        // If a specific source is assigned (like for 3D positional audio), use it
        if (sfxSource != null)
        {
            Debug.Log("[ButtonSFX] Playing via local sfxSource");
            sfxSource.PlayOneShot(clickSFX);
        }
        // Otherwise, use the global UI Audio Manager so prefabs don't get cut off!
        else if (AudioManager.instance != null)
        {
            Debug.Log("[ButtonSFX] Playing via AudioManager.instance");
            AudioManager.instance.PlaySFX(clickSFX);
        }
        else 
        {
            Debug.LogWarning("[ButtonSFX] AudioManager.instance is NULL! Did you put the AudioManager prefab in the scene?");
        }
    }
}
