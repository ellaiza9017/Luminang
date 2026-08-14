using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuAudioSliders : MonoBehaviour
{
    [Header("Music / BGM Slider")]
    public Slider musicSlider;
    public Image musicFillImage;
    public TextMeshProUGUI musicPercentageText;

    [Header("SFX Slider")]
    public Slider sfxSlider;
    public Image sfxFillImage;
    public TextMeshProUGUI sfxPercentageText;

    [Header("Optional Mute Buttons")]
    public Button musicMuteButton;
    public Image musicMuteImage;
    public Button sfxMuteButton;
    public Image sfxMuteImage;

    [Header("Volume Sprites (Optional)")]
    public Sprite volumeUpSprite;
    public Sprite volumeOffSprite;

    private void Awake()
    {
        // Hook up slider listeners
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

        // Hook up mute button listeners
        if (musicMuteButton != null)
            musicMuteButton.onClick.AddListener(ToggleMusicMute);

        if (sfxMuteButton != null)
            sfxMuteButton.onClick.AddListener(ToggleSFXMute);
    }

    private void OnEnable()
    {
        // Whenever the MenuGroup opens, fetch the latest saved volumes from AudioManager / PlayerPrefs
        SyncSlidersWithCurrentVolume();
    }

    public void SyncSlidersWithCurrentVolume()
    {
        float currentMusic = 0.75f;
        float currentSFX = 0.75f;

        if (AudioManager.instance != null)
        {
            currentMusic = AudioManager.instance.musicVolume;
            currentSFX = AudioManager.instance.sfxVolume;
        }
        else
        {
            currentMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            currentSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(currentMusic);
            UpdateMusicUI(currentMusic);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(currentSFX);
            UpdateSFXUI(currentSFX);
        }
    }

    private void OnMusicSliderChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ApplyMusicVolume(value);
        }
        else
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            PlayerPrefs.Save();
            if (BGMManager.Instance != null)
                BGMManager.Instance.UpdateVolume();
        }

        UpdateMusicUI(value);
    }

    private void OnSFXSliderChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ApplySFXVolume(value);
        }
        else
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
            
            // Fallback for editor testing when AudioManager is missing
            SFXVolumeSync[] sfxSyncs = FindObjectsOfType<SFXVolumeSync>();
            foreach(var sfx in sfxSyncs)
            {
                sfx.UpdateVolume();
            }
        }

        UpdateSFXUI(value);
    }

    public void ToggleMusicMute()
    {
        if (musicSlider == null) return;
        float targetVolume = (musicSlider.value > 0) ? 0f : 1.0f;
        musicSlider.value = targetVolume; // triggers OnMusicSliderChanged
    }

    public void ToggleSFXMute()
    {
        if (sfxSlider == null) return;
        float targetVolume = (sfxSlider.value > 0) ? 0f : 1.0f;
        sfxSlider.value = targetVolume; // triggers OnSFXSliderChanged
    }

    private void UpdateMusicUI(float value)
    {
        if (musicPercentageText != null)
            musicPercentageText.text = Mathf.RoundToInt(value * 100f) + "%";

        if (musicFillImage != null)
            musicFillImage.fillAmount = value;

        if (musicMuteImage != null && volumeUpSprite != null && volumeOffSprite != null)
            musicMuteImage.sprite = (value > 0) ? volumeUpSprite : volumeOffSprite;
    }

    private void UpdateSFXUI(float value)
    {
        if (sfxPercentageText != null)
            sfxPercentageText.text = Mathf.RoundToInt(value * 100f) + "%";

        if (sfxFillImage != null)
            sfxFillImage.fillAmount = value;

        if (sfxMuteImage != null && volumeUpSprite != null && volumeOffSprite != null)
            sfxMuteImage.sprite = (value > 0) ? volumeUpSprite : volumeOffSprite;
    }
}
