using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TusokRecallManager : MonoBehaviour
{
    public static TusokRecallManager Instance { get; private set; }

    [Header("UI References")]
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceTexts;
    
    // We'll store the full phrase list here for quick lookup
    private Dictionary<string, string> phraseLookupIlokano = new Dictionary<string, string>();
    private Dictionary<string, string> phraseLookupCebuano = new Dictionary<string, string>();

    private void Awake()
    {
        Instance = this;
        
        LoadPhrases();
    }

    private void LoadPhrases()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("LuminangPhrases");
        if (jsonAsset != null)
        {
            PhraseList phraseList = JsonUtility.FromJson<PhraseList>(jsonAsset.text);
            if (phraseList != null && phraseList.phrases != null)
            {
                foreach (var phrase in phraseList.phrases)
                {
                    phraseLookupIlokano[phrase.id] = phrase.ilokano;
                    phraseLookupCebuano[phrase.id] = phrase.cebuano;
                }
            }
        }
    }

    public void StartRecallRound(CountingRoundData roundData)
    {
        string selectedLang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        
        // Pick the correct dictionary
        Dictionary<string, string> currentLookup = selectedLang == "Ilokano" ? phraseLookupIlokano : phraseLookupCebuano;
        
        // Combine correct phrase and distractors
        List<string> options = new List<string>();
        
        if (currentLookup.ContainsKey(roundData.correctPhraseId))
            options.Add(currentLookup[roundData.correctPhraseId]);
            
        if (roundData.distractors != null)
        {
            foreach (var distractor in roundData.distractors)
            {
                if (currentLookup.ContainsKey(distractor.phraseId))
                    options.Add(currentLookup[distractor.phraseId]);
            }
        }
        
        // Shuffle the options
        ShuffleList(options);
        
        // Assign to UI
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Count)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceTexts[i].text = options[i];
                
                // Clear previous listeners
                choiceButtons[i].onClick.RemoveAllListeners();
                
                string chosenOption = options[i];
                string correctOption = currentLookup.ContainsKey(roundData.correctPhraseId) ? currentLookup[roundData.correctPhraseId] : "";
                
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(chosenOption, correctOption, roundData, selectedLang));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnChoiceSelected(string chosen, string correct, CountingRoundData roundData, string lang)
    {
        if (chosen == correct)
        {
            // Trigger correct logic in GameManager
            TusokTusokGameManager.Instance.OnRecallCorrect(roundData, lang);
        }
        else
        {
            // Find the specific distractor feedback
            string feedback = "Incorrect choice!"; // fallback
            
            // Re-lookup the phrase ID of what they chose
            string chosenId = "";
            Dictionary<string, string> currentLookup = lang == "Ilokano" ? phraseLookupIlokano : phraseLookupCebuano;
            foreach (var kvp in currentLookup)
            {
                if (kvp.Value == chosen)
                {
                    chosenId = kvp.Key;
                    break;
                }
            }

            if (roundData.distractors != null)
            {
                foreach (var dist in roundData.distractors)
                {
                    if (dist.phraseId == chosenId)
                    {
                        feedback = dist.feedback;
                        break;
                    }
                }
            }

            TusokTusokGameManager.Instance.OnRecallWrong(feedback, roundData, lang);
        }
    }

    private void ShuffleList(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            string temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    [System.Serializable]
    public class PhraseList
    {
        public PhraseData[] phrases;
    }

    [System.Serializable]
    public class PhraseData
    {
        public string id;
        public string ilokano;
        public string cebuano;
    }
}
