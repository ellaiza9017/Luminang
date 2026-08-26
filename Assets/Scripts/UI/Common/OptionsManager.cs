using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("Music Slider")]
    public Slider musicSlider;
    public Image musicFillImage;
    public TextMeshProUGUI musicPercentageText;

    [Header("SFX Slider")]
    public Slider sfxSlider;
    public Image sfxFillImage;
    public TextMeshProUGUI sfxPercentageText;

    [Header("Look Sensitivity")]
    public Slider lookSensitivitySlider;
    public Image lookSensitivityFillImage;
    public TextMeshProUGUI lookSensitivityPercentageText;

    [Header("Mute Buttons")]
    public Button musicMuteButton;
    public Image musicMuteImage;
    public Button sfxMuteButton;
    public Image sfxMuteImage;

    [Header("Volume Sprites")]
    public Sprite volumeUpSprite;
    public Sprite volumeOffSprite;



    [Header("Dynamic Background")]
    public Image backgroundImage;
    public Sprite mainMenuBg;
    public Sprite languageSelectionBg;
    public Sprite calleCrisologoBg;
    public Sprite magellansCrossBg;

    // Static variable to track which scene triggered the options menu
    public static string PreviousSceneName = "";

    [Header("Panels (Disabled)")]
    public GameObject confirmSavePanel; // Keep for inspector safety, but not used
    public GameObject noChangesPanel;


    private void Start()
    {
        // Set dynamic background based on the previous scene
        if (backgroundImage != null)
        {
            if (PreviousSceneName == "MainMenuScene")
            {
                backgroundImage.sprite = mainMenuBg;
            }
            else if (PreviousSceneName == "LanguageSelectionScene")
            {
                backgroundImage.sprite = languageSelectionBg;
            }
            else if (PreviousSceneName == "Calle_Crisologo")
            {
                backgroundImage.sprite = calleCrisologoBg;
            }
            else if (PreviousSceneName == "Magellan_s_Cross")
            {
                backgroundImage.sprite = magellansCrossBg;
            }
        }

        // 1. Load & Set Volume
        if (AudioManager.instance != null)
        {
            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

            musicSlider.value = savedMusic;
            sfxSlider.value = savedSFX;
            
            // Apply (which now auto-saves internally)
            AudioManager.instance.ApplyMusicVolume(savedMusic);
            AudioManager.instance.ApplySFXVolume(savedSFX);
        }

        // 2. Load & Set Look Sensitivity
        if (lookSensitivitySlider != null)
        {
            float savedSensitivity = PlayerPrefs.GetFloat("LookSensitivity", 1.5f);
            lookSensitivitySlider.value = savedSensitivity;
            lookSensitivitySlider.onValueChanged.AddListener(OnLookSensitivityChanged);
            UpdateLookSensitivityUI(savedSensitivity);
        }



        // Hide panels if they exist (even though not used anymore)
        if (confirmSavePanel != null) confirmSavePanel.SetActive(false);
        if (noChangesPanel != null) noChangesPanel.SetActive(false);

        // 3. Add Listeners for Sliders
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

        // 4. Auto-assign Images if Buttons are set
        if (musicMuteButton != null && musicMuteImage == null) musicMuteImage = musicMuteButton.GetComponent<Image>();
        if (sfxMuteButton != null && sfxMuteImage == null) sfxMuteImage = sfxMuteButton.GetComponent<Image>();



        // 5. Add Listeners for Mute Buttons
        if (musicMuteButton != null) musicMuteButton.onClick.AddListener(ToggleMusicMute);
        if (sfxMuteButton != null) sfxMuteButton.onClick.AddListener(ToggleSFXMute);

        // Update Text & Icons initially
        UpdateMusicUI(musicSlider.value);
        UpdateSFXUI(sfxSlider.value);
    }

    private void OnMusicSliderChanged(float value)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.ApplyMusicVolume(value); // This now auto-saves

        UpdateMusicUI(value);
    }

    private void OnSFXSliderChanged(float value)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.ApplySFXVolume(value); // This now auto-saves

        UpdateSFXUI(value);
    }

    private void OnLookSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("LookSensitivity", value);
        PlayerPrefs.Save();
        UpdateLookSensitivityUI(value);
    }

    private void UpdateLookSensitivityUI(float value)
    {
        if (lookSensitivityPercentageText != null)
        {
            // Assuming the slider is set from 0.1 to 5.0 in the inspector.
            // If they set it from 0 to 1, we can multiply by 100 to show %.
            // But if it's 1.5f default, showing "1.5x" or "150%" makes sense.
            // Let's format it nicely to 1 decimal place.
            lookSensitivityPercentageText.text = value.ToString("F1") + "x";
        }

        if (lookSensitivityFillImage != null && lookSensitivitySlider != null)
        {
            // Slider value is between 0.1 and 5.0, but fillAmount only accepts 0 to 1.
            // We use normalizedValue to perfectly convert the slider's position into a 0 to 1 range!
            lookSensitivityFillImage.fillAmount = lookSensitivitySlider.normalizedValue;
        }
    }



    public void ToggleMusicMute()
    {
        float targetVolume = (musicSlider.value > 0) ? 0 : 1.0f;
        musicSlider.value = targetVolume; // This triggers OnMusicSliderChanged
    }

    public void ToggleSFXMute()
    {
        float targetVolume = (sfxSlider.value > 0) ? 0 : 1.0f;
        sfxSlider.value = targetVolume; // This triggers OnSFXSliderChanged
    }

    // --- Helper methods (cleaned up) ---

    [ContextMenu("Auto Find All UI Elements")]
    public void AutoFindButtons()
    {
        // Find existing buttons in children
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string n = btn.name.ToLower();

            
            if (n.Contains("music") && n.Contains("mute")) musicMuteButton = btn;
            if (n.Contains("soundeffects") && n.Contains("mute")) sfxMuteButton = btn;
            
            if (n.Contains("music") && n.Contains("slider")) musicSlider = btn.GetComponent<Slider>();
            if (n.Contains("soundeffects") && n.Contains("slider")) sfxSlider = btn.GetComponent<Slider>();
            if (n.Contains("look") && n.Contains("slider")) lookSensitivitySlider = btn.GetComponent<Slider>();
        }

        // Find TMP texts
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            string n = t.name.ToLower();
            if (n.Contains("music") && n.Contains("percentage")) musicPercentageText = t;
            if (n.Contains("soundeffects") && n.Contains("percentage")) sfxPercentageText = t;
            if (n.Contains("look") && n.Contains("percentage")) lookSensitivityPercentageText = t;
        }

        // Assign Images
        if (musicMuteButton != null) musicMuteImage = musicMuteButton.GetComponent<Image>();
        if (sfxMuteButton != null) sfxMuteImage = sfxMuteButton.GetComponent<Image>();
        
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            string n = img.name.ToLower();
            if (n.Contains("look") && n.Contains("fill")) lookSensitivityFillImage = img;
        }
        
        Debug.Log("<color=green>[OptionsManager] UI Elements AUTO-FOUND!</color>");
    }

    [ContextMenu("Reset All Settings to Factory Default")]
    public void ResetSettings()
    {
        // Deletes saved volumes and graphics
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=cyan>[OptionsManager] All settings RESET! Please restart the game.</color>");
    }

    private System.Collections.IEnumerator AnimateButtonPress(Transform t, System.Action onComplete)
    {
        float duration = 0.05f;
        Vector3 originalScale = Vector3.one;
        Vector3 pressedScale = new Vector3(0.92f, 0.92f, 1f);

        // Squeeze down
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(originalScale, pressedScale, elapsed / duration);
            yield return null;
        }

        // Pop back up
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(pressedScale, originalScale, elapsed / duration);
            yield return null;
        }

        t.localScale = originalScale;
        onComplete?.Invoke();
    }

    private void HidePanel(GameObject panel)
    {
        // This method is no longer used.
    }

    private System.Collections.IEnumerator AnimateScale(Transform t, Vector3 start, Vector3 end, float duration, System.Action onComplete = null)
    {
        // This method is no longer used.
        yield break;
    }

    private void UpdateMusicUI(float value)
    {
        if (musicPercentageText != null)
            musicPercentageText.text = Mathf.RoundToInt(value * 100f).ToString() + "%";

        if (musicFillImage != null)
            musicFillImage.fillAmount = value;

        if (musicMuteImage != null)
            musicMuteImage.sprite = (value > 0) ? volumeUpSprite : volumeOffSprite;
    }

    private void UpdateSFXUI(float value)
    {
        if (sfxPercentageText != null)
            sfxPercentageText.text = Mathf.RoundToInt(value * 100f).ToString() + "%";

        if (sfxFillImage != null)
            sfxFillImage.fillAmount = value;

        if (sfxMuteImage != null)
            sfxMuteImage.sprite = (value > 0) ? volumeUpSprite : volumeOffSprite;
    }

    public void GoBack()
    {
        if (SceneManager.sceneCount > 1)
        {
            // OptionScene was loaded additively — just unload it.
            // The background scene (e.g. Magellan's Cross) is still alive, player position preserved.

            // Re-show the PauseMenuCanvas (button + panel) that we hid when opening Options
            if (PauseMenuController.Instance != null)
                PauseMenuController.Instance.gameObject.SetActive(true);

            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        else
        {
            // OptionScene was loaded as the only scene (e.g. from MainMenu) — full transition back
            string targetScene = string.IsNullOrEmpty(PreviousSceneName) ? "MainMenuScene" : PreviousSceneName;

            if (TransitionOverlay.Instance != null)
                TransitionOverlay.Instance.StartTransition(targetScene);
            else
                SceneManager.LoadScene(targetScene);
        }
    }
}

