using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent GameObject containing the dark background and buttons.")]
    public GameObject pauseMenuPanel;
    
    [Header("Scene Settings")]
    [Tooltip("The exact name of your Slambook/Language Select scene.")]
    public string returnSceneName = "LanguageSelectScene"; // Change this if your scene is named differently!

    private bool isPaused = false;

    private void Start()
    {
        // Ensure the menu is hidden when the scene starts
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Listen for the Escape key to toggle the pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // This freezes the game (animations, movement, etc.)
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // This unfreezes the game
    }

    public void OpenOptions()
    {
        // Set the previous scene name so the OptionsManager knows which background to show
        OptionsManager.PreviousSceneName = SceneManager.GetActiveScene().name;

        // Load the options scene additively so it pops up over the paused game
        SceneManager.LoadScene("OptionsScene", LoadSceneMode.Additive);
    }

    public void ExitLevel()
    {
        // Always reset time scale before loading a new scene!
        Time.timeScale = 1f; 
        SceneManager.LoadScene(returnSceneName);
    }
}
