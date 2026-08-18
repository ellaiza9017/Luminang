using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent GameObject containing the dark background and buttons.")]
    public GameObject pauseMenuPanel;
    
    [Header("Scene Settings")]
    [Tooltip("The exact name of your Slambook/Language Select scene.")]
    public string returnSceneName = "LanguageSelectionScene";

    // Static reference so OptionsManager can re-show us when it closes
    public static PauseMenuController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Ensure the menu panel is hidden when the scene starts
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    public void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenOptions()
    {
        // Reset timeScale before leaving
        Time.timeScale = 1f;

        // Hide the ENTIRE PauseMenuCanvas (button + panel) while OptionScene is open.
        // OptionsManager.GoBack() will re-show it when returning.
        gameObject.SetActive(false);

        // Tell OptionsManager which scene we came from (for background image + GoBack)
        OptionsManager.PreviousSceneName = SceneManager.GetActiveScene().name;

        // Load additively — Magellan's Cross stays alive in the background
        SceneManager.LoadScene("OptionScene", LoadSceneMode.Additive);
    }

    public void ExitLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(returnSceneName);
    }
}
