#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Populates the RightGroup detail panel when a lesson is selected from the left list.
///
/// Hierarchy it maps to:
///   RightGroup
///     LevelNumberBG / LevelNumber         ← "Level 9"
///     ChapterIconFrame / ChapterIconMask / ChapterIcon  ← chapter icon sprite
///     ChapterTitle                         ← "CONVERSATIONAL & SOCIAL"
///     LessonTitle                          ← "Expressions of Gratitude"
///     LessonDescription                    ← paragraph description
///     LearningsPanel / LearningsScrollView / Viewport / Content
///       LearningsList prefab (spawned)     ← one per learning bullet
///     Coins                                ← "50 Coins"
///     StartButton
///
/// Wire-up:
///   CategoryListManager → onCategorySelected → LevelDetailPanel.ShowDetails
///   LanguageCardManager → levelDetailPanel field
/// </summary>
public class LevelDetailPanel : MonoBehaviour
{
    // ──────────────────────────────────────────────────
    // Inspector References
    // ──────────────────────────────────────────────────

    [Header("Level Number")]
    [Tooltip("The TMP text inside LevelNumberBG. Shows 'Level 9'.")]
    public TextMeshProUGUI levelNumberText;

    [Header("Chapter Icon")]
    [Tooltip("The Image component on ChapterIcon. Gets the icon from the chapter's sprite array.")]
    public Image chapterIconImage;

    [Header("Titles")]
    [Tooltip("ChapterTitle TMP. Shows the chapter name in ALL CAPS e.g. 'CONVERSATIONAL & SOCIAL'.")]
    public TextMeshProUGUI chapterTitleText;

    [Tooltip("Colors used for the Chapter Title text (one per chapter).")]
    public Color[] chapterTitleColors;

    [Tooltip("LessonTitle TMP. Shows the lesson name e.g. 'Expressions of Gratitude'.")]
    public TextMeshProUGUI lessonTitleText;

    [Header("Description")]
    [Tooltip("LessonDescription TMP. Shows the long description paragraph.")]
    public TextMeshProUGUI lessonDescriptionText;

    [Header("Learnings List")]
    [Tooltip("The Content transform inside LearningsScrollView. Learnings rows are spawned here.")]
    public Transform learningsContent;

    [Tooltip("Prefab with LearningItemRow script. Should have an IconBullet Image and LessonDescri TMP.")]
    public GameObject learningItemPrefab;

    [Tooltip("Sprites used for the bullet points in the learnings list (one per chapter).")]
    public Sprite[] bulletIconSprites;

    [Header("Rewards")]
    [Tooltip("The TMP text on the Coins label. Shows e.g. '50 Coins'.")]
    public TextMeshProUGUI coinsText;

    [Header("Start Button")]
    [Tooltip("The StartButton Button component.")]
    public Button startButton;

    [Tooltip("The TMP text label inside StartButton.")]
    public TextMeshProUGUI startButtonLabel;

    [Header("Data Source")]
    [Tooltip("Drag LessonsData.json here (Assets/Data/LessonsData.json).")]
    public TextAsset lessonsDataJson;

    [Header("Chapter Icons Reference")]
    [Tooltip("Drag the same CategoryListManager that drives the left list. Used to pull chapter icon sprites.")]
    public CategoryListManager categoryListManager;

    // ──────────────────────────────────────────────────
    // JSON Data Models  (mirrors LessonsData.json)
    // ──────────────────────────────────────────────────

    [System.Serializable]
    private class LessonEntry
    {
        public int levelNumber;
        public string categoryKey;
        public string lessonTitle;
        public string description;
        public List<string> learnings;
        public RewardEntry rewards;
    }

    [System.Serializable]
    private class RewardEntry
    {
        public int coins;
        public int xp;
    }

    [System.Serializable]
    private class ChapterEntry
    {
        public int chapterIndex;
        public string chapterTitle;
        public List<LessonEntry> lessons;
    }

    [System.Serializable]
    private class LanguageEntry
    {
        public string languageKey;
        public List<ChapterEntry> chapters = null;
    }

    [System.Serializable]
    private class LessonsDataWrapper
    {
        public List<LanguageEntry> languages;
    }

    // ──────────────────────────────────────────────────
    // Private State
    // ──────────────────────────────────────────────────

        [System.Serializable]
    private class ObjectivesData { public System.Collections.Generic.List<CategoryObjectives> objectives; }
    
    [System.Serializable]
    private class CategoryObjectives { public string category; public System.Collections.Generic.List<ObjectiveItem> items; }
    
    [System.Serializable]
    private class ObjectiveItem { public string id; public string objective; }

    private LessonsDataWrapper _data;
    private string _currentLanguage = "ilokano";
    private string _currentCategoryKey;

    // ──────────────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────────────

    private void Awake()
    {
        if (lessonsDataJson != null)
            _data = JsonUtility.FromJson<LessonsDataWrapper>(lessonsDataJson.text);
        else
        {
            // Fallback for mobile if Inspector reference is lost
            TextAsset resourceAsset = Resources.Load<TextAsset>("LessonsData");
            if (resourceAsset != null)
                _data = JsonUtility.FromJson<LessonsDataWrapper>(resourceAsset.text);
        }

        if (_data == null)
            Debug.LogError("[LevelDetailPanel] Failed to parse LessonsData.json. Is it assigned or in the Resources folder?");

        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonPressed);
    }

    private void Start()
    {
        // Panel starts empty — CategoryListManager.FireInitialSelection() populates it one frame later
    }

    private void OnEnable()
    {
        // Ensure we always have the correct language when the panel turns on
        string savedLang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        _currentLanguage = savedLang.ToLower();

        if (!string.IsNullOrEmpty(_currentCategoryKey))
            ShowDetails(_currentCategoryKey);
    }

    // ──────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────

    /// <summary>
    /// Called by CategoryListManager.onCategorySelected.
    /// Finds the lesson in LessonsData.json and populates all right-panel fields.
    /// </summary>
    public void ShowDetails(string categoryKey)
    {
        _currentCategoryKey = categoryKey;

        // Find lesson + its parent chapter
        ChapterEntry chapter = null;
        LessonEntry lesson = null;
        FindLesson(categoryKey, out chapter, out lesson);

        if (lesson == null)
        {
            Debug.LogWarning($"[LevelDetailPanel] No lesson found for categoryKey '{categoryKey}' in language '{_currentLanguage}'");
            ShowEmpty();
            return;
        }

        // ── Level Number ──
        if (levelNumberText != null)
            levelNumberText.text = $"Quest {lesson.levelNumber}";

        // ── Chapter Icon (pulled from CategoryListManager's chapterIconSprites array) ──
        if (chapterIconImage != null && categoryListManager != null)
        {
            int iconIdx = chapter.chapterIndex - 1;
            if (categoryListManager.chapterIconSprites != null &&
                iconIdx < categoryListManager.chapterIconSprites.Length &&
                categoryListManager.chapterIconSprites[iconIdx] != null)
            {
                chapterIconImage.sprite = categoryListManager.chapterIconSprites[iconIdx];
            }
        }

        // ── Chapter Title (ALL CAPS + Color) ──
        if (chapterTitleText != null)
        {
            chapterTitleText.text = chapter.chapterTitle.ToUpper();

            if (chapterTitleColors != null)
            {
                int colorIdx = chapter.chapterIndex - 1;
                if (colorIdx >= 0 && colorIdx < chapterTitleColors.Length)
                {
                    chapterTitleText.color = chapterTitleColors[colorIdx];
                }
            }
        }

        // ── Lesson Title ──
        if (lessonTitleText != null)
            lessonTitleText.text = lesson.lessonTitle;

        // ── Description ──
        if (lessonDescriptionText != null)
            lessonDescriptionText.text = lesson.description;

        // ── Learnings List ──
        Sprite bulletSprite = null;
        if (bulletIconSprites != null)
        {
            int iconIdx = chapter.chapterIndex - 1;
            if (iconIdx >= 0 && iconIdx < bulletIconSprites.Length)
                bulletSprite = bulletIconSprites[iconIdx];
        }
        PopulateLearnings(lesson.learnings, bulletSprite);

        // ── Coins ──
        if (coinsText != null)
        {
            int coins = lesson.rewards != null ? lesson.rewards.coins : 0;
            coinsText.text = $"{coins} Coins";
        }

        // ── Start Button ──
        if (categoryListManager != null)
        {
            categoryListManager.GetLessonState(categoryKey, out bool isCompleted, out bool isLocked);

            if (startButton != null)
                startButton.interactable = !isLocked;

            if (startButtonLabel != null)
            {
                if (isLocked)
                {
                    startButtonLabel.text = "Start Lesson";
                }
                else
                {
                    int compCount = GetCompletedObjectivesCount(categoryKey, _currentLanguage);
                    int totCount = GetTotalObjectivesCount(categoryKey, _currentLanguage);
                    
                    if (isCompleted || (totCount > 0 && compCount >= totCount))
                    {
                        startButtonLabel.text = "Play Again";
                    }
                    else if (compCount > 0 && compCount < totCount)
                    {
                        startButtonLabel.text = "Continue";
                    }
                    else
                    {
                        startButtonLabel.text = "Start Lesson";
                    }
                }
            }
        }
        else
        {
            if (startButton != null) startButton.interactable = true;
            if (startButtonLabel != null) startButtonLabel.text = "Start Lesson";
        }
    }

    /// <summary>
    /// Called by LanguageCardManager when Ilokano or Cebuano is selected.
    /// Refreshes the panel if a lesson is already shown.
    /// </summary>
    public void SetLanguage(string languageName)
    {
        _currentLanguage = languageName.ToLower();

        if (!string.IsNullOrEmpty(_currentCategoryKey))
            ShowDetails(_currentCategoryKey);
    }

    // ──────────────────────────────────────────────────
    // Private Helpers
    // ──────────────────────────────────────────────────

    private void FindLesson(string categoryKey, out ChapterEntry foundChapter, out LessonEntry foundLesson)
    {
        foundChapter = null;
        foundLesson  = null;

        if (_data?.languages == null) return;

        LanguageEntry lang = _data.languages.Find(
            l => string.Equals(l.languageKey, _currentLanguage, System.StringComparison.OrdinalIgnoreCase)
        );

        if (lang?.chapters == null) return;

        foreach (var ch in lang.chapters)
        {
            if (ch.lessons == null) continue;
            foreach (var les in ch.lessons)
            {
                if (string.Equals(les.categoryKey, categoryKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundChapter = ch;
                    foundLesson  = les;
                    return;
                }
            }
        }
    }

    private void PopulateLearnings(List<string> learnings, Sprite bulletSprite)
    {
        // Clear old rows
        if (learningsContent != null)
        {
            foreach (Transform child in learningsContent)
                Destroy(child.gameObject);
        }

        if (learningsContent == null || learningItemPrefab == null || learnings == null) return;

        foreach (string item in learnings)
        {
            GameObject row = Instantiate(learningItemPrefab, learningsContent, false);
            LearningItemRow rowUI = row.GetComponent<LearningItemRow>();
            if (rowUI != null)
                rowUI.Setup(item, bulletSprite);
        }
    }

    private void ShowEmpty()
    {
        if (levelNumberText != null)       levelNumberText.text = "";
        if (chapterTitleText != null)      chapterTitleText.text = "SELECT A LESSON";
        if (lessonTitleText != null)       lessonTitleText.text = "";
        if (lessonDescriptionText != null) lessonDescriptionText.text = "Choose a lesson from the list to see its details.";
        if (coinsText != null)             coinsText.text = "— Coins";
        if (startButton != null)           startButton.interactable = false;
        if (startButtonLabel != null)      startButtonLabel.text = "Start Lesson";

        if (learningsContent != null)
            foreach (Transform child in learningsContent)
                Destroy(child.gameObject);
    }


    private int GetCompletedObjectivesCount(string categoryKey, string language)
    {
        if (UserProfileManager.Instance == null || UserProfileManager.Instance.CurrentProfile == null) return 0;
        
        string filename = (language.ToLower() == "cebuano") ? "Cebuano Objectives" : "Ilokano Objectives";
        TextAsset jsonAsset = Resources.Load<TextAsset>(filename);
        if (jsonAsset == null) return 0;
        
        ObjectivesData objData = JsonUtility.FromJson<ObjectivesData>(jsonAsset.text);
        if (objData == null || objData.objectives == null) return 0;
        
        CategoryObjectives catData = objData.objectives.Find(c => string.Equals(c.category, categoryKey, System.StringComparison.OrdinalIgnoreCase));
        if (catData == null || catData.items == null) return 0;
        
        System.Collections.Generic.List<string> completedList = (language.ToLower() == "cebuano") 
            ? UserProfileManager.Instance.CurrentProfile.CompletedObjectivesCebuano
            : UserProfileManager.Instance.CurrentProfile.CompletedObjectivesIlokano;
            
        int count = 0;
        foreach (var item in catData.items)
        {
            if (completedList.Contains(item.id)) count++;
        }
        return count;
    }
    
    private int GetTotalObjectivesCount(string categoryKey, string language)
    {
        string filename = (language.ToLower() == "cebuano") ? "Cebuano Objectives" : "Ilokano Objectives";
        TextAsset jsonAsset = Resources.Load<TextAsset>(filename);
        if (jsonAsset == null) return 0;
        
        ObjectivesData objData = JsonUtility.FromJson<ObjectivesData>(jsonAsset.text);
        if (objData == null || objData.objectives == null) return 0;
        
        CategoryObjectives catData = objData.objectives.Find(c => string.Equals(c.category, categoryKey, System.StringComparison.OrdinalIgnoreCase));
        if (catData == null || catData.items == null) return 0;
        
        return catData.items.Count;
    }

    private void OnStartButtonPressed()
    {
        if (string.IsNullOrEmpty(_currentCategoryKey)) return;

        // Remember the selected lesson category so Magellan's Cross knows what to load!
        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
        {
            // Update last_category_id or similar if needed. For now, we just save the string in PlayerPrefs as the 'ActiveQuest'
            PlayerPrefs.SetString("ActiveQuest", _currentCategoryKey);
            PlayerPrefs.Save();
        }

        StartCoroutine(StartTransitionRoutine());
    }

    private System.Collections.IEnumerator StartTransitionRoutine()
    {
        // 1. Close the Book!
        if (BookSelectionManager.Instance != null)
        {
            BookSelectionManager.Instance.CloseBook();
            
            // Wait for half the close animation
            float waitTime = (BookSelectionManager.Instance.openSprites != null) 
                ? (BookSelectionManager.Instance.openSprites.Length * BookSelectionManager.Instance.openCloseTimePerFrame) / 2f 
                : 0.5f;
            yield return new WaitForSeconds(waitTime);
        }

        // 2. Load the scene based on language and Intro flags
        string activeLang = PlayerPrefs.GetString("SelectedLanguage", _currentLanguage);
        bool isCebuano = activeLang.ToLower() == "cebuano";
        string targetScene = "";

        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
        {
            var profile = UserProfileManager.Instance.CurrentProfile;
            if (isCebuano)
            {
                if (!profile.HasSeenCebuIntro) targetScene = "CebuIntroScene";
                else targetScene = "Magellan_s_Cross";
            }
            else
            {
                if (!profile.HasSeenIlocosIntro) targetScene = "IlocosIntroScene"; // Adjust if different
                else targetScene = "Calle_Crisologo";
            }
        }
        else
        {
            // Fallback
            targetScene = isCebuano ? "Magellan_s_Cross" : "Calle_Crisologo";
        }

        // Use SceneLoader to smoothly transition with the loading screen
        var sceneLoader = Object.FindFirstObjectByType<SceneLoader>();
        if (sceneLoader == null)
        {
            Debug.Log("[LevelDetailPanel] No SceneLoader found in the scene. Creating a temporary one to trigger the loading screen.");
            GameObject tempLoaderObj = new GameObject("TempSceneLoader");
            sceneLoader = tempLoaderObj.AddComponent<SceneLoader>();
            sceneLoader.useLoadingScreenForGameScene = true;
            sceneLoader.loadingSceneName = "LoadingScene";
        }
        
        sceneLoader.LoadScene(targetScene);
    }
}
