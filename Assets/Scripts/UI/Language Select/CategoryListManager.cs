#pragma warning disable 0649
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Manages the left-side chapter/lesson list inside LevelsGroup.
///
/// Data sources (two separate JSON files):
///   - lessonsData      (LessonsData.json)      → chapter titles, lesson titles, level numbers
///   - chaptersJsonData (ChaptersDemoData.json)  → isCompleted, isExpanded  (simulates DB)
///
/// Both are matched by categoryKey.
/// </summary>
public class CategoryListManager : MonoBehaviour
{
    [Header("List Settings")]
    public Transform contentParent;

    [Header("Data")]
    [Tooltip("Drag LessonsData.json (Assets/Data). Provides chapter/lesson titles.")]
    public TextAsset lessonsData;
    [Tooltip("Drag ChaptersDemoData.json (Assets/Demo Data). Provides isCompleted / isExpanded (simulates DB).")]
    public TextAsset chaptersJsonData;

    [Header("Prefabs")]
    public GameObject chapterHeaderPrefab;
    public GameObject lessonRowPrefab;

    [Header("Language Specific Colours")]
    public Color ilokanoSelectedBgColor = new Color(0.2f, 0.5f, 1f);
    public Color cebuanoSelectedBgColor = new Color(1f, 0.8f, 0.2f);
    public Color normalBgColor = Color.clear;

    [Header("Chapter Sprites (one per chapter, in order)")]
    [Tooltip("Sprites for the background behind each chapter number. Leave empty to use prefab default.")]
    public Sprite[] chapterNumberBgSprites;
    [Tooltip("Icon sprites for each chapter header. Leave empty to use prefab default.")]
    public Sprite[] chapterIconSprites;
    [Tooltip("Background color for each chapter header row. Leave array empty to use prefab default.")]
    public Color[] chapterHeaderColors;

    [Header("Lesson Row Sprites (one per chapter, in order)")]
    [Tooltip("Sprites for the background behind the lesson number.")]
    public Sprite[] lessonNumberBgSprites;
    [Tooltip("Sprites for the background behind the checkmark when the lesson is COMPLETED.")]
    public Sprite[] lessonCompletedBgSprites;
    [Tooltip("Sprite for the background behind the checkmark when the lesson is INCOMPLETE (same for all).")]
    public Sprite lessonIncompleteBgSprite;
    [Tooltip("Color tint when lesson is COMPLETED.")]
    public Color lessonCompletedBgColor = Color.white;
    [Tooltip("Color tint when lesson is INCOMPLETE.")]
    public Color lessonIncompleteBgColor = Color.white;

    [Header("Callbacks")]
    public UnityEngine.Events.UnityEvent<string> onCategorySelected;

    [Header("Expand Animation")]
    [Tooltip("How fast the chapter rows slide open/close (seconds).")]
    public float expandDuration = 0.2f;
    [Tooltip("The height (in pixels) of each lesson row. Set this to match your LessonRowPrefab's height.")]
    public float rowHeight = 60f;

    // ──────────────────────────────────────────────────
    // Internal Models — LessonsData.json
    // ──────────────────────────────────────────────────

    [System.Serializable]
    private class LessonEntry
    {
        public int levelNumber;
        public string categoryKey;
        public string lessonTitle = null;
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
        public string languageKey;  // "ilokano" | "cebuano"
        public List<ChapterEntry> chapters;
    }

    [System.Serializable]
    private class LessonsDataWrapper
    {
        public List<LanguageEntry> languages;
    }

    // ──────────────────────────────────────────────────
    // Internal Models — ChaptersDemoData.json  (DB mock) - REMOVED
    // ──────────────────────────────────────────────────

    // ──────────────────────────────────────────────────
    // Merged Runtime Model
    // ──────────────────────────────────────────────────

    private class MergedLesson
    {
        public int levelNumber;
        public string categoryKey;
        public string lessonTitle;
        public bool isCompleted;
        public bool isLocked;
    }

    private class MergedChapter
    {
        public int chapterIndex;
        public string chapterTitle;
        public bool isExpanded;
        public List<MergedLesson> lessons;
    }

    // ──────────────────────────────────────────────────
    // Private State
    // ──────────────────────────────────────────────────

    private LessonsDataWrapper _lessonsData;
    private List<MergedChapter> _chapters = new List<MergedChapter>();

    private string _selectedCategory = "Greetings";

    private enum Language { Ilokano, Cebuano }
    private Language _activeLanguage = Language.Ilokano;

    private Dictionary<int, List<GameObject>> _chapterLessonRows = new Dictionary<int, List<GameObject>>();
    private Dictionary<int, Coroutine> _chapterAnimCoroutines = new Dictionary<int, Coroutine>();

    // ──────────────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────────────

    private void Awake()
    {
        ParseJsonFiles();
        MergeData();
    }

    private void Start()
    {
        BuildCategoryList();
        StartCoroutine(ForceLayoutRebuild());
        // Delay by one frame so all other Start() methods (e.g. LevelDetailPanel) finish first
        StartCoroutine(FireInitialSelection());
    }

    private IEnumerator FireInitialSelection()
    {
        yield return null; // wait one frame
        if (!string.IsNullOrEmpty(_selectedCategory))
            SelectCategory(_selectedCategory);
    }

    // ──────────────────────────────────────────────────
    // Data Loading
    // ──────────────────────────────────────────────────

    private void ParseJsonFiles()
    {
        // Parse LessonsData.json — try Inspector slot first, then Resources fallback (mobile-safe)
        if (lessonsData != null)
        {
            _lessonsData = JsonUtility.FromJson<LessonsDataWrapper>(lessonsData.text);
        }
        else
        {
            // Fallback: load from Resources/LessonsData (works on mobile even if Inspector slot is unassigned)
            TextAsset resourceAsset = Resources.Load<TextAsset>("LessonsData");
            if (resourceAsset != null)
                _lessonsData = JsonUtility.FromJson<LessonsDataWrapper>(resourceAsset.text);
        }

        if (_lessonsData == null)
        {
            Debug.LogError("[CategoryListManager] Failed to parse LessonsData.json. Is the file assigned?");
            _lessonsData = new LessonsDataWrapper { languages = new List<LanguageEntry>() };
        }
    }


    /// <summary>
    /// Builds the merged chapter list for the active language by:
    /// 1. Taking chapter + lesson structure from LessonsData
    /// 2. Overlaying isCompleted from Supabase Profile (UserProfileManager)
    /// </summary>
    private void MergeData()
    {
        _chapters.Clear();

        string langKey = _activeLanguage == Language.Ilokano ? "ilokano" : "cebuano";

        // Find the matching language block in LessonsData
        LanguageEntry langEntry = _lessonsData.languages?.Find(
            l => string.Equals(l.languageKey, langKey, System.StringComparison.OrdinalIgnoreCase)
        );

        if (langEntry == null || langEntry.chapters == null)
        {
            Debug.LogWarning($"[CategoryListManager] No LessonsData found for language: {langKey}");
            return;
        }

        // Track when we hit the first incomplete lesson to lock everything after it
        bool foundFirstIncomplete = false;

        // Get completed objectives from Supabase
        List<string> completedKeys = new List<string>();
        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
        {
            var profile = UserProfileManager.Instance.CurrentProfile;
            completedKeys = _activeLanguage == Language.Ilokano 
                ? (profile.CompletedObjectivesIlokano ?? new List<string>())
                : (profile.CompletedObjectivesCebuano ?? new List<string>());
        }

        for (int ci = 0; ci < langEntry.chapters.Count; ci++)
        {
            ChapterEntry chapter = langEntry.chapters[ci];

            MergedChapter merged = new MergedChapter
            {
                chapterIndex = chapter.chapterIndex,
                chapterTitle = chapter.chapterTitle,
                isExpanded = (ci == 0), // By default, only expand the first chapter
                lessons = new List<MergedLesson>()
            };

            if (chapter.lessons != null)
            {
                foreach (var lesson in chapter.lessons)
                {
                    bool isCompleted = completedKeys.Contains(lesson.categoryKey);

                    bool isLocked = false;
                    if (!isCompleted)
                    {
                        if (foundFirstIncomplete)
                        {
                            // We already found an incomplete lesson earlier, so this one is locked
                            isLocked = true;
                        }
                        else
                        {
                            // This is the first incomplete lesson, so it is unlocked (ready to play)
                            foundFirstIncomplete = true;
                        }
                    }

                    merged.lessons.Add(new MergedLesson
                    {
                        levelNumber = lesson.levelNumber,
                        categoryKey = lesson.categoryKey,
                        lessonTitle = lesson.lessonTitle,
                        isCompleted = isCompleted,
                        isLocked = isLocked
                    });
                }
            }

            _chapters.Add(merged);
        }
    }

    // ──────────────────────────────────────────────────
    // List Building
    // ──────────────────────────────────────────────────

    public void BuildCategoryList()
    {
        if (contentParent == null || chapterHeaderPrefab == null || lessonRowPrefab == null) return;

        // Clear everything
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        _chapterLessonRows.Clear();
        _chapterAnimCoroutines.Clear();

        Color selectedBg = GetSelectedBgColor();
        int chapterNum = 1;

        foreach (var chapter in _chapters)
        {
            // ── Chapter Header ──
            GameObject headerObj = Instantiate(chapterHeaderPrefab, contentParent, false);
            ChapterHeaderUI headerUI = headerObj.GetComponent<ChapterHeaderUI>();
            if (headerUI != null)
            {
                int completedCount = chapter.lessons.FindAll(l => l.isCompleted).Count;
                string progressStr = $"{completedCount}/{chapter.lessons.Count}";

                int spriteIdx = chapterNum - 1;
                Sprite numBg = (chapterNumberBgSprites != null && spriteIdx < chapterNumberBgSprites.Length)
                    ? chapterNumberBgSprites[spriteIdx] : null;
                Sprite icon = (chapterIconSprites != null && spriteIdx < chapterIconSprites.Length)
                    ? chapterIconSprites[spriteIdx] : null;
                Color? headerColor = (chapterHeaderColors != null && spriteIdx < chapterHeaderColors.Length)
                    ? chapterHeaderColors[spriteIdx] : (Color?)null;

                headerUI.Setup(chapterNum, chapter.chapterTitle, progressStr, chapter.isExpanded, this, numBg, icon, headerColor);
            }

            // ── Lesson Rows ──
            List<GameObject> lessonRows = new List<GameObject>();

            foreach (var lesson in chapter.lessons)
            {
                GameObject lessonObj = Instantiate(lessonRowPrefab, contentParent, false);
                LessonRowUI lessonUI = lessonObj.GetComponent<LessonRowUI>();

                if (lessonUI != null)
                {
                    bool isSelected = (lesson.categoryKey == _selectedCategory);
                    lessonUI.selectedBgColor = selectedBg;
                    lessonUI.normalBgColor = normalBgColor;

                    // Use chapterNum - 1 as sprite index so all lessons in the same chapter share the same sprite/color
                    int spriteIdx = chapterNum - 1;

                    Sprite rowNumBg = (lessonNumberBgSprites != null && spriteIdx < lessonNumberBgSprites.Length)
                        ? lessonNumberBgSprites[spriteIdx] : null;

                    Sprite rowCheckBg = lesson.isCompleted
                        ? ((lessonCompletedBgSprites != null && spriteIdx < lessonCompletedBgSprites.Length)
                            ? lessonCompletedBgSprites[spriteIdx] : null)
                        : lessonIncompleteBgSprite;

                    Color rowCheckColor = lesson.isCompleted ? lessonCompletedBgColor : lessonIncompleteBgColor;

                    lessonUI.Setup(
                        lesson.levelNumber, // global level number (continues across chapters)
                        lesson.lessonTitle,
                        lesson.categoryKey,
                        lesson.isCompleted,
                        isSelected,
                        SelectCategory,
                        rowNumBg,
                        rowCheckBg,
                        rowCheckColor
                    );
                }

                lessonObj.SetActive(chapter.isExpanded);
                lessonRows.Add(lessonObj);
            }

            _chapterLessonRows[chapterNum] = lessonRows;
            chapterNum++;
        }
    }

    // ──────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────

    public void GetLessonState(string categoryKey, out bool isCompleted, out bool isLocked)
    {
        isCompleted = false;
        isLocked = false;
        foreach (var ch in _chapters)
        {
            foreach (var l in ch.lessons)
            {
                if (l.categoryKey == categoryKey)
                {
                    isCompleted = l.isCompleted;
                    isLocked = l.isLocked;
                    return;
                }
            }
        }
    }

    public void SelectCategory(string categoryKey)
    {
        _selectedCategory = categoryKey;
        onCategorySelected?.Invoke(_selectedCategory);
        RefreshLessonSelectionVisuals();
    }

    /// <summary>Called from LanguageCardManager when Ilokano or Cebuano is selected.</summary>
    public void SetActiveLanguage(string languageName)
    {
        if (languageName.Equals("Ilokano", System.StringComparison.OrdinalIgnoreCase))
            _activeLanguage = Language.Ilokano;
        else if (languageName.Equals("Cebuano", System.StringComparison.OrdinalIgnoreCase))
            _activeLanguage = Language.Cebuano;
        else
            _activeLanguage = Language.Ilokano;

        // MergeData first so _chapters reflects the new language
        MergeData();

        // Now pick the first lesson of the new language
        _selectedCategory = "";
        if (_chapters.Count > 0 && _chapters[0].lessons.Count > 0)
            _selectedCategory = _chapters[0].lessons[0].categoryKey;

        BuildCategoryList();
        StartCoroutine(ForceLayoutRebuild());

        // Fire so the right panel updates immediately
        if (!string.IsNullOrEmpty(_selectedCategory))
            SelectCategory(_selectedCategory);
    }

    // ──────────────────────────────────────────────────
    // Chapter Expand / Collapse
    // ──────────────────────────────────────────────────

    public void ToggleChapter(int chapterIndex)
    {
        int listIndex = chapterIndex - 1;
        if (listIndex < 0 || listIndex >= _chapters.Count) return;

        _chapters[listIndex].isExpanded = !_chapters[listIndex].isExpanded;
        bool expand = _chapters[listIndex].isExpanded;

        ChapterHeaderUI header = GetChapterHeader(chapterIndex);
        if (header != null) header.UpdateChevron(expand);

        if (!_chapterLessonRows.ContainsKey(chapterIndex)) return;

        List<GameObject> rows = _chapterLessonRows[chapterIndex];

        if (_chapterAnimCoroutines.ContainsKey(chapterIndex) && _chapterAnimCoroutines[chapterIndex] != null)
            StopCoroutine(_chapterAnimCoroutines[chapterIndex]);

        _chapterAnimCoroutines[chapterIndex] = StartCoroutine(AnimateRows(rows, expand));
    }

    private ChapterHeaderUI GetChapterHeader(int chapterIndex)
    {
        int headerCount = 0;
        foreach (Transform child in contentParent)
        {
            ChapterHeaderUI h = child.GetComponent<ChapterHeaderUI>();
            if (h != null)
            {
                headerCount++;
                if (headerCount == chapterIndex) return h;
            }
        }
        return null;
    }

    // ──────────────────────────────────────────────────
    // Animation
    // ──────────────────────────────────────────────────

    private IEnumerator AnimateRows(List<GameObject> rows, bool expand)
    {
        float targetHeight = expand ? rowHeight : 0f;
        float startHeight  = expand ? 0f : rowHeight;

        if (expand)
        {
            foreach (var row in rows)
            {
                row.SetActive(true);
                var le = GetOrAddLayoutElement(row);
                le.preferredHeight = 0f;
                le.minHeight = 0f;
            }
        }

        float elapsed = 0f;
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / expandDuration);
            float h = Mathf.Lerp(startHeight, targetHeight, t);

            foreach (var row in rows)
            {
                var le = GetOrAddLayoutElement(row);
                le.preferredHeight = h;
                le.minHeight = 0f;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
            yield return null;
        }

        foreach (var row in rows)
        {
            if (!expand)
            {
                row.SetActive(false);
                var le = row.GetComponent<LayoutElement>();
                if (le != null) { le.preferredHeight = -1f; le.minHeight = -1f; }
            }
            else
            {
                var le = GetOrAddLayoutElement(row);
                le.preferredHeight = rowHeight;
                le.minHeight = 0f;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }

    // ──────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────

    private void RefreshLessonSelectionVisuals()
    {
        Color selectedBg = GetSelectedBgColor();
        foreach (Transform child in contentParent)
        {
            LessonRowUI lessonUI = child.GetComponent<LessonRowUI>();
            if (lessonUI != null)
            {
                lessonUI.selectedBgColor = selectedBg;
                lessonUI.SetSelected(lessonUI.CategoryName == _selectedCategory);
            }
        }
    }

    private Color GetSelectedBgColor()
    {
        return _activeLanguage == Language.Ilokano ? ilokanoSelectedBgColor : cebuanoSelectedBgColor;
    }

    private LayoutElement GetOrAddLayoutElement(GameObject go)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        return le;
    }

    private IEnumerator ForceLayoutRebuild()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }
}
