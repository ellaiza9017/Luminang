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
        int totalIlo = 0;
        int totalCeb = 0;

        // 1. Calculate TOTAL lessons (quests) from LessonsData JSON
        if (lessonsDataJson != null)
        {
            MinimalLessonsData data = JsonUtility.FromJson<MinimalLessonsData>(lessonsDataJson.text);
            if (data != null && data.languages != null)
            {
                foreach (var lang in data.languages)
                {
                    int lessonCount = 0;
                    if (lang.chapters != null)
                    {
                        foreach (var chap in lang.chapters)
                        {
                            if (chap.lessons != null)
                                lessonCount += chap.lessons.Count;
                        }
                    }

                    if (string.Equals(lang.languageKey, "ilokano", System.StringComparison.OrdinalIgnoreCase))
                        totalIlo = lessonCount;
                    else if (string.Equals(lang.languageKey, "cebuano", System.StringComparison.OrdinalIgnoreCase))
                        totalCeb = lessonCount;
                }
            }
        }
        else
        {
            Debug.LogWarning("[LanguageCardManager] LessonsData JSON File is not assigned, cannot calculate total lessons!");
        }

        // 2. Calculate COMPLETED lessons from Supabase (UserProfileManager)
        int completedIlo = 0;
        int completedCeb = 0;

        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
        {
            var profile = UserProfileManager.Instance.CurrentProfile;
            completedIlo = profile.CompletedObjectivesIlokano != null ? profile.CompletedObjectivesIlokano.Count : 0;
            completedCeb = profile.CompletedObjectivesCebuano != null ? profile.CompletedObjectivesCebuano.Count : 0;
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
        if (categoryListManager != null) categoryListManager.SetActiveLanguage(languageName);
        if (wordsListManager != null) wordsListManager.SetLanguage(languageName);
        if (levelDetailPanel != null) levelDetailPanel.SetLanguage(languageName);

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
