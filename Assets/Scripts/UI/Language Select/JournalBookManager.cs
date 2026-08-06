using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class JournalBookManager : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("The JSON file from Demo Data")]
    public TextAsset journalJsonFile;

    [Header("List Settings (Left)")]
    public Transform listContentParent;
    public GameObject journalRowPrefab;
    
    [Header("Details Panel (Right)")]
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI pronunciationText;
    public TextMeshProUGUI meaningText;
    public TextMeshProUGUI sampleSentenceText;
    public TextMeshProUGUI howUsedText;
    public Button soundButton;

    [Header("Tabs & Themes")]
    public Button ilokanoTabButton;
    public Button cebuanoTabButton;
    
    [Header("Ilokano Theme")]
    public Sprite ilokanoRowIcon;
    public Sprite ilokanoSoundIcon;
    public Color ilokanoWordColor = new Color(0.2f, 0.4f, 0.1f);
    public Color ilokanoRowNormal = new Color(0.85f, 0.95f, 0.85f);
    public Color ilokanoRowPressed = new Color(0.7f, 0.85f, 0.7f);

    [Header("Cebuano Theme")]
    public Sprite cebuanoRowIcon;
    public Sprite cebuanoSoundIcon;
    public Color cebuanoWordColor = new Color(0.1f, 0.3f, 0.5f);
    public Color cebuanoRowNormal = new Color(0.85f, 0.9f, 0.95f);
    public Color cebuanoRowPressed = new Color(0.7f, 0.8f, 0.9f);

    [Header("Empty States")]
    public GameObject emptyStateLeft;
    public GameObject emptyStateRight;
    public List<GameObject> objectsToHideLeft;
    public List<GameObject> objectsToHideRight;

    private JournalData _journalData;
    private string _currentLanguage = "Ilokano"; // Default tab
    private string _currentCategory = "All Categories"; // Default category
    private JournalEntry _selectedEntry;
    private readonly List<JournalRowItem> _spawnedRows = new List<JournalRowItem>();

    private void Start()
    {
        LoadData();
        SetLanguage("Ilokano"); // Also calls RefreshList and updates tab visuals
        
        if (soundButton != null)
        {
            soundButton.onClick.AddListener(PlaySound);
        }
    }

    // Public accessor so other managers (e.g. CategoryListManager) can read the journal entries
    public JournalData GetJournalData() => _journalData;

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

        if (_journalData == null || _journalData.journal_entries == null)
        {
            Debug.LogError("JournalData failed to parse from JSON! Is it assigned or in the Resources folder?");
        }
        else
        {
            Debug.Log($"Loaded {_journalData.journal_entries.Count} journal entries from JSON.");
        }
    }

    // Called by the Ilokano and Cebuano tab buttons
    public void SetLanguage(string language)
    {
        _currentLanguage = language;
        
        // Active tab looks bright, inactive tab looks grayed out (but remains clickable!)
        if (ilokanoTabButton != null) 
        {
            ilokanoTabButton.interactable = true;
            ilokanoTabButton.image.color = (language == "Ilokano") ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        }
        if (cebuanoTabButton != null) 
        {
            cebuanoTabButton.interactable = true;
            cebuanoTabButton.image.color = (language == "Cebuano") ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        RefreshList();
    }

    // Called by the CategoryDropdown event
    public void SetCategory(string category)
    {
        _currentCategory = category;
        RefreshList();
    }

    private void RefreshList()
    {
        // Clear existing rows
        foreach (Transform child in listContentParent)
        {
            Destroy(child.gameObject);
        }
        _spawnedRows.Clear();

        if (_journalData == null || _journalData.journal_entries == null) 
        {
            Debug.LogWarning("RefreshList: No journal data available.");
            return;
        }

        Debug.Log($"RefreshList called. Lang: {_currentLanguage}, Cat: {_currentCategory}. Total entries: {_journalData.journal_entries.Count}");

        bool firstEntryDisplayed = false;

        foreach (var entry in _journalData.journal_entries)
        {
            // Filter by Language
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

            // Filter by Category
            if (_currentCategory != "All Categories" && _currentCategory != "All")
            {
                if (!string.Equals(entry.category, _currentCategory, System.StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            Debug.Log($"Spawning row for: {entry.phrase} ({entry.language})");

            Sprite activeIcon = _currentLanguage == "Ilokano" ? ilokanoRowIcon : cebuanoRowIcon;
            Color activeNormal = _currentLanguage == "Ilokano" ? ilokanoRowNormal : cebuanoRowNormal;
            Color activePressed = _currentLanguage == "Ilokano" ? ilokanoRowPressed : cebuanoRowPressed;

            // Spawn row with worldPositionStays = false to prevent UI scale/position bugs
            GameObject newRow = Instantiate(journalRowPrefab, listContentParent, false);
            JournalRowItem rowScript = newRow.GetComponent<JournalRowItem>();
            if (rowScript != null)
            {
                rowScript.Setup(entry, this, activeIcon, activeNormal, activePressed);
                _spawnedRows.Add(rowScript);
            }

            // Auto-select the first visible item
            if (!firstEntryDisplayed)
            {
                DisplayDetails(entry);
                firstEntryDisplayed = true;
            }
        }

        // If list is empty after filtering, clear details
        if (!firstEntryDisplayed)
        {
            if (emptyStateLeft != null) emptyStateLeft.SetActive(true);
            if (emptyStateRight != null) emptyStateRight.SetActive(true);
            if (objectsToHideLeft != null) foreach (var obj in objectsToHideLeft) if (obj != null) obj.SetActive(false);
            if (objectsToHideRight != null) foreach (var obj in objectsToHideRight) if (obj != null) obj.SetActive(false);
            ClearDetails();
        }
        else
        {
            if (emptyStateLeft != null) emptyStateLeft.SetActive(false);
            if (emptyStateRight != null) emptyStateRight.SetActive(false);
            if (objectsToHideLeft != null) foreach (var obj in objectsToHideLeft) if (obj != null) obj.SetActive(true);
            if (objectsToHideRight != null) foreach (var obj in objectsToHideRight) if (obj != null) obj.SetActive(true);
        }
    }

    public void DisplayDetails(JournalEntry entry)
    {
        if (entry == null) return;
        _selectedEntry = entry;

        if (wordText != null) 
        {
            wordText.text = entry.phrase;
            wordText.color = _currentLanguage == "Ilokano" ? ilokanoWordColor : cebuanoWordColor;
        }

        if (pronunciationText != null) pronunciationText.text = entry.pronunciation;
        if (meaningText != null) meaningText.text = entry.meaning;
        if (howUsedText != null) howUsedText.text = entry.usage_note;

        if (sampleSentenceText != null && entry.sample_sentence != null)
        {
            sampleSentenceText.text = $"{entry.sample_sentence.native}\n{entry.sample_sentence.translation}";
        }

        // We don't load a clip yet, but we enable/disable based on if sound_file is populated
        if (soundButton != null)
        {
            soundButton.interactable = !string.IsNullOrEmpty(entry.sound_file);
            Image soundImg = soundButton.GetComponent<Image>();
            if (soundImg != null)
            {
                soundImg.sprite = _currentLanguage == "Ilokano" ? ilokanoSoundIcon : cebuanoSoundIcon;
            }
        }

        UpdateRowSelectionVisuals();
    }

    private void UpdateRowSelectionVisuals()
    {
        foreach (var row in _spawnedRows)
        {
            if (row != null)
            {
                row.SetSelected(row.Entry == _selectedEntry);
            }
        }
    }

    private void ClearDetails()
    {
        if (wordText != null) wordText.text = "-";
        if (pronunciationText != null) pronunciationText.text = "-";
        if (meaningText != null) meaningText.text = "-";
        if (sampleSentenceText != null) sampleSentenceText.text = "-";
        if (howUsedText != null) howUsedText.text = "-";
        
        if (soundButton != null) soundButton.interactable = false;
    }

    private void PlaySound()
    {
        Debug.Log("Playing sound... (Sound files not yet implemented)");
        // Add AudioSource logic here later
    }
}
