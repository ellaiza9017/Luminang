using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardJournalPreview : MonoBehaviour
{
    [Header("Data Source")]
    public TextAsset journalJsonFile;

    [Header("Buttons")]
    public Button ilokanoCardButton;
    public Button cebuanoCardButton;

    [Header("Categories UI")]
    public Transform categoryListParent;
    public GameObject categoryButtonPrefab;
    public TextMeshProUGUI categoriesText;

    [Header("Words UI")]
    public Transform wordListParent;
    public GameObject wordRowPrefab;
    public TextMeshProUGUI categoryTitleText;
    public TextMeshProUGUI wordsLearnedText;

    private LuminangPhraseData _phraseData;
    private Dictionary<string, LuminangPhrase> _allEntriesDict = new Dictionary<string, LuminangPhrase>();

    private LeaderboardEntry _currentEntry;
    private string _currentLanguage = "ilokano";
    private string _currentCategory = "";

    // A mapping from Category Name -> List of Phrases the user unlocked in that category
    private Dictionary<string, List<string>> _currentLanguageCategories = new Dictionary<string, List<string>>();

    private void Start()
    {
        LoadJournalData();

        if (ilokanoCardButton != null)
        {
            ilokanoCardButton.onClick.RemoveAllListeners();
            ilokanoCardButton.onClick.AddListener(() => SetLanguage("ilokano"));
        }

        if (cebuanoCardButton != null)
        {
            cebuanoCardButton.onClick.RemoveAllListeners();
            cebuanoCardButton.onClick.AddListener(() => SetLanguage("cebuano"));
        }
    }

    private void LoadJournalData()
    {
        if (journalJsonFile != null)
        {
            _phraseData = JsonUtility.FromJson<LuminangPhraseData>(journalJsonFile.text);
            if (_phraseData != null && _phraseData.phrases != null && _phraseData.phrases.Count > 0)
            {
                foreach (var entry in _phraseData.phrases)
                {
                    _allEntriesDict[entry.id] = entry;
                }
                Debug.Log($"[LeaderboardJournalPreview] Loaded {_allEntriesDict.Count} phrases successfully.");
            }
            else
            {
                Debug.LogError("[LeaderboardJournalPreview] The assigned JSON file does not contain a valid 'phrases' array! Did you assign LuminangJournalDictionary instead of LuminangPhrases.json?");
            }
        }
        else
        {
            Debug.LogError("[LeaderboardJournalPreview] LuminangPhrases JSON file is not assigned in the Inspector!");
        }
    }

    public void SetPlayer(LeaderboardEntry entry)
    {
        _currentEntry = entry;
        
        // Refresh with current language
        SetLanguage(_currentLanguage);
    }

    private void SetLanguage(string language)
    {
        if (_currentEntry == null) return;
        
        _currentLanguage = language.ToLower();

        // Visual feedback for tabs (optional, assuming they are Buttons with Images)
        if (ilokanoCardButton != null && ilokanoCardButton.image != null)
        {
            ilokanoCardButton.image.color = (_currentLanguage == "ilokano") ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        }
        if (cebuanoCardButton != null && cebuanoCardButton.image != null)
        {
            cebuanoCardButton.image.color = (_currentLanguage == "cebuano") ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        List<string> unlockedIds = (_currentLanguage == "ilokano") ? _currentEntry.UnlockedPhrasesIlokano : _currentEntry.UnlockedPhrasesCebuano;
        
        // Group by category
        _currentLanguageCategories.Clear();

        if (unlockedIds != null)
        {
            foreach (string phraseId in unlockedIds)
            {
                if (_allEntriesDict.TryGetValue(phraseId, out var phraseEntry))
                {
                    string cat = phraseEntry.category;
                    if (!_currentLanguageCategories.ContainsKey(cat))
                    {
                        _currentLanguageCategories[cat] = new List<string>();
                    }
                    
                    // Pull the correct translation string based on the active tab
                    string phraseText = (_currentLanguage == "ilokano") ? phraseEntry.ilokano : phraseEntry.cebuano;
                    _currentLanguageCategories[cat].Add(phraseText);
                }
            }
        }

        PopulateCategories();
    }

    private void PopulateCategories()
    {
        // Clear old categories
        foreach (Transform child in categoryListParent)
        {
            if (!child.gameObject.activeSelf || child.gameObject == categoryButtonPrefab) continue;
            Destroy(child.gameObject);
        }

        if (categoriesText != null)
        {
            categoriesText.text = $"{_currentLanguageCategories.Count} Categories";
        }

        if (_currentLanguageCategories.Count == 0)
        {
            _currentCategory = "";
            PopulateWords();
            return;
        }

        bool isFirst = true;

        foreach (var categoryName in _currentLanguageCategories.Keys.OrderBy(k => k))
        {
            GameObject btnObj = Instantiate(categoryButtonPrefab, categoryListParent, false);
            btnObj.SetActive(true);
            LeaderboardCategoryButton btnScript = btnObj.GetComponent<LeaderboardCategoryButton>();
            if (btnScript != null)
            {
                btnScript.Setup(categoryName, this);
            }

            if (isFirst)
            {
                SelectCategory(categoryName);
                isFirst = false;
            }
        }
    }

    public void SelectCategory(string categoryName)
    {
        _currentCategory = categoryName;
        PopulateWords();
    }

    private void PopulateWords()
    {
        // Clear old words
        foreach (Transform child in wordListParent)
        {
            if (!child.gameObject.activeSelf || child.gameObject == wordRowPrefab) continue;
            Destroy(child.gameObject);
        }

        if (categoryTitleText != null)
        {
            categoryTitleText.text = string.IsNullOrEmpty(_currentCategory) ? "No Phrases Unlocked" : _currentCategory;
        }

        if (string.IsNullOrEmpty(_currentCategory) || !_currentLanguageCategories.ContainsKey(_currentCategory))
        {
            if (wordsLearnedText != null) wordsLearnedText.text = "0 Phrases";
            return;
        }

        List<string> words = _currentLanguageCategories[_currentCategory];

        if (wordsLearnedText != null)
        {
            wordsLearnedText.text = $"{words.Count} Phrases";
        }

        foreach (var word in words)
        {
            if (wordRowPrefab == null)
            {
                Debug.LogError("[LeaderboardJournalPreview] wordRowPrefab has been destroyed! Make sure you are assigning the Prefab ASSET from the Project Window, NOT a scene object!");
                return;
            }

            GameObject rowObj = Instantiate(wordRowPrefab, wordListParent, false);
            rowObj.SetActive(true);
            LeaderboardWordRow rowScript = rowObj.GetComponent<LeaderboardWordRow>();
            if (rowScript != null)
            {
                rowScript.Setup(word);
            }
        }
    }
}
