using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameMenuManager : MonoBehaviour
{
    [Header("Menu UI")]
    public GameObject menuGroup;
    public GameObject menuPanel;

    [Header("Optional Links")]
    [Tooltip("Link the HowToPlay group if this minigame has one, so the menu can open it.")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;

    [Header("Sound Effects")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSFX;
    public AudioClip panelOpenSFX;

    public void OpenMenu()
    {
        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);
        if (sfxSource != null && panelOpenSFX != null) sfxSource.PlayOneShot(panelOpenSFX);
        menuGroup.SetActive(true);
        menuGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
        menuPanel?.GetComponent<UIPopAnimator>()?.PopIn();
    }

    public void ResumeGame()
    {
        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);
        // Instant close — safe on all platforms
        if (menuPanel != null) menuPanel.transform.localScale = Vector3.zero;
        if (menuGroup != null) menuGroup.SetActive(false);
    }

    public void RestartMinigame()
    {
        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitMinigame()
    {
        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);
        PlayerPrefs.SetInt("FishingMinigameWon", 0);
        PlayerPrefs.SetInt("MinigameWon", 0);
        PlayerPrefs.Save();

        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene");
        SceneManager.LoadScene(prevScene);
    }

    public void OpenHowToPlay()
    {
        if (howToPlayGroup == null)
        {
            Debug.LogWarning("How To Play Group is not assigned in the MinigameMenuManager!");
            return;
        }

        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);

        // Close the menu panel instantly first, then open How To Play
        if (menuPanel != null) menuPanel.transform.localScale = Vector3.zero;
        if (menuGroup != null) menuGroup.SetActive(false);

        howToPlayGroup.SetActive(true);
        howToPlayGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
        if (sfxSource != null && panelOpenSFX != null) sfxSource.PlayOneShot(panelOpenSFX);
        if (howToPlayPanel != null)
        {
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
    }
}
