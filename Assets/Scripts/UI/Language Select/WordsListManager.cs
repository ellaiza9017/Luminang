using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the WordsGroup list panel.
/// Shows only the phrases/words the player has learned (from demo data for now),
/// filtered by the currently selected language card and category.
/// 
/// Attach to the WordsGroup object.
/// Called externally by CategoryListManager (OnCategorySelected) and LanguageCardManager.
/// </summary>
public class WordsListManager : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("The JournalDemoData JSON file (Assets/Demo Data/JournalDemoData.json)")]
    public TextAsset journalJsonFile;

    [Header("List UI")]
    public Transform contentParent;
    public GameObject wordRowPrefab;

    [Header("Header Texts")]
    [Tooltip("Shows the current category name e.g. 'CONVERSATIONAL & SOCIAL'")]
    public TextMeshProUGUI categoryHeaderText;
    [Tooltip("Shows the subtitle e.g. 'WORDS YOU'VE LEARNED (ILOKANO)'")]
    public TextMeshProUGUI subtitleText;

    [Header("Language Icons")]
    [Tooltip("Icon shown beside each row when Ilokano is selected")]
    public Sprite ilokanoIcon;
    [Tooltip("Icon shown beside each row when Cebuano is selected")]
    public Sprite cebuanoIcon;

    // Private state
    private JournalData _journalData;
    private string _currentLanguage = "Ilokano";
    private string _currentCategory = "All";

    private void Start()
    {
        LoadData();
        RefreshList();
    }

    private void LoadData()
    {
        if (journalJsonFile != null)
        {
            _journalData = JsonUtility.FromJson<JournalData>(journalJsonFile.text);
        }
        else
        {
            // Fallback for mobile if Inspector reference is lost
            TextAsset resourceAsset = Resources.Load<TextAsset>("LuminangJournalDictionary");
            if (resourceAsset != null)
                _journalData = JsonUtility.FromJson<JournalData>(resourceAsset.text);
        }

        if (_journalData == null)
            Debug.LogError("[WordsListManager] Failed to parse JournalData JSON! Is it assigned or in the Resources folder?");
    }

    // Called by LanguageCardManager when a card is selected
    public void SetLanguage(string language)
    {
        _currentLanguage = language;
        RefreshList();
    }

    // Called by CategoryListManager's OnCategorySelected event
    public void SetCategory(string category)
    {
        _currentCategory = category;
        RefreshList();
    }

    private void RefreshList()
    {
        // Clear existing rows
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (_journalData == null || _journalData.journal_entries == null) return;

        // Update header texts
        if (categoryHeaderText != null)
            categoryHeaderText.text = (_currentCategory == "All" || _currentCategory == "All Categories")
                ? "ALL WORDS"
                : _currentCategory.ToUpper();

        if (subtitleText != null)
            subtitleText.text = $"WORDS THIS PLAYER HAS LEARNED ({_currentLanguage.ToUpper()})";

        // Pick the right icon
        Sprite activeIcon = _currentLanguage == "Ilokano" ? ilokanoIcon : cebuanoIcon;

        // Filter and spawn rows
        int count = 0;
        foreach (var entry in _journalData.journal_entries)
        {
            // Filter by language
            if (!string.Equals(entry.language, _currentLanguage, System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Filter by Unlocked Status in Supabase
            if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
            {
                string baseId = entry.id.Replace("ilo_", "").Replace("ceb_", "");
                List<string> unlockedIds = (_currentLanguage == "Ilokano")
                    ? UserProfileManager.Instance.CurrentProfile.UnlockedPhrasesIlokano
                    : UserProfileManager.Instance.CurrentProfile.UnlockedPhrasesCebuano;

                if (unlockedIds == null || !unlockedIds.Contains(baseId))
                    continue;
            }

            // Filter by category (skip if "All")
            if (_currentCategory != "All" && _currentCategory != "All Categories")
            {
                if (!string.Equals(entry.category, _currentCategory, System.StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            // Spawn row
            GameObject row = Instantiate(wordRowPrefab, contentParent, false);
            WordRowItem rowItem = row.GetComponent<WordRowItem>();
            if (rowItem != null)
                rowItem.Setup(entry.phrase, activeIcon);

            count++;
        }

        if (count == 0)
        {
            // Optionally show an empty state message
            if (categoryHeaderText != null)
                categoryHeaderText.text = "NO WORDS LEARNED YET";
        }

        Debug.Log($"[WordsListManager] Showing {count} words for {_currentLanguage} / {_currentCategory}");
    }
}
