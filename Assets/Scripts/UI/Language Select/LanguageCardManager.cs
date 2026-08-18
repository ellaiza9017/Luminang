using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Ilokano and Cebuano language card buttons in the Book.
/// When clicked, it tells BookSelectionManager to flip the page to LevelsGroup.
/// Attach this script to the LanguagesGroup object.
/// </summary>
public class LanguageCardManager : MonoBehaviour
{
    [System.Serializable]
    public class LanguageCard
    {
        public string languageName;
        public Button cardButton;
        public TMPro.TextMeshProUGUI progressText; // NEW: To show "8/12"
    }

    [Header("Data Source")]
    [Tooltip("Assign the LessonsData JSON file to calculate total available lessons (quests).")]
    public TextAsset lessonsDataJson;

    [Header("Language Cards")]
    public LanguageCard ilokanoCard;
    public LanguageCard cebuanoCard;

    [Header("Callbacks")]
    [Tooltip("Drag CategoryListManager here so it updates its button colors when a card is selected.")]
    public CategoryListManager categoryListManager;
    [Tooltip("Drag WordsListManager here so the word list updates when a card is selected.")]
    public WordsListManager wordsListManager;
    [Tooltip("Drag LevelDetailPanel here so its phrase preview updates when a language card is selected.")]
    public LevelDetailPanel levelDetailPanel;

    // Minimal JSON classes to parse the total lessons
    [System.Serializable]
    private class MinimalLesson { public string categoryKey; }
    [System.Serializable]
    private class MinimalChapter { public System.Collections.Generic.List<MinimalLesson> lessons; }
    [System.Serializable]
    private class MinimalLanguage { public string languageKey; public System.Collections.Generic.List<MinimalChapter> chapters; }
    [System.Serializable]
    private class MinimalLessonsData { public System.Collections.Generic.List<MinimalLanguage> languages; }

    private void Start()
    {
        if (ilokanoCard?.cardButton != null)
            ilokanoCard.cardButton.onClick.AddListener(() => SelectLanguage("Ilokano"));

        if (cebuanoCard?.cardButton != null)
            cebuanoCard.cardButton.onClick.AddListener(() => SelectLanguage("Cebuano"));

        UpdateProgressTexts();
    }

    private void UpdateProgressTexts()
    {
        int totalIlo = 12;
        int totalCeb = 12;

        // Calculate COMPLETED lessons from Supabase (UserProfileManager)
        int completedIlo = 0;
        int completedCeb = 0;

        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
        {
            var profile = UserProfileManager.Instance.CurrentProfile;
            completedIlo = ProgressCalculator.GetCompletedLessonsCount("ilokano", profile.CompletedObjectivesIlokano);
            completedCeb = ProgressCalculator.GetCompletedLessonsCount("cebuano", profile.CompletedObjectivesCebuano);
        }

        // 3. Update the UI Text
        if (ilokanoCard != null && ilokanoCard.progressText != null)
        {
            ilokanoCard.progressText.text = $"{completedIlo}/{totalIlo}";
        }

        if (cebuanoCard != null && cebuanoCard.progressText != null)
        {
            cebuanoCard.progressText.text = $"{completedCeb}/{totalCeb}";
        }
    }

    public void SelectLanguage(string languageName)
    {
        PlayerPrefs.SetString("SelectedLanguage", languageName);
        PlayerPrefs.Save();

        var catManager = categoryListManager != null ? categoryListManager : UnityEngine.Object.FindFirstObjectByType<CategoryListManager>();
        if (catManager != null) catManager.SetActiveLanguage(languageName);

        var wordsManager = wordsListManager != null ? wordsListManager : UnityEngine.Object.FindFirstObjectByType<WordsListManager>();
        if (wordsManager != null) wordsManager.SetLanguage(languageName);

        var levelPanel = levelDetailPanel != null ? levelDetailPanel : UnityEngine.Object.FindFirstObjectByType<LevelDetailPanel>();
        if (levelPanel != null) levelPanel.SetLanguage(languageName);

        if (BookSelectionManager.Instance != null)
        {
            BookSelectionManager.Instance.OpenLevelsGroup();
        }
    }

    public void GoToMainMenu()
    {
        if (TransitionOverlay.Instance != null)
        {
            TransitionOverlay.Instance.StartTransition("MainMenuScene");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }
}
