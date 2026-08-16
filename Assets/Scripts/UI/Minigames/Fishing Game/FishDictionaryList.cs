using UnityEngine;
using System.Collections.Generic;

public class FishDictionaryList : MonoBehaviour
{
    [System.Serializable]
    public class PhraseData
    {
        public string id;
        public string category;
        public string type;
        public string english;
        public string ilokano;
        public string cebuano;
    }

    [System.Serializable]
    public class PhraseList
    {
        public List<PhraseData> phrases;
    }

    [Header("UI References")]
    public Transform contentParent; 
    public GameObject listItemPrefab; 
    
    [Header("Data")]
    public TextAsset jsonFile; 
    
    [Header("Settings")]
    public string targetLanguage = "cebuano";
    public string categoryFilter = "Greetings"; 
    
    void Start()
    {
        // Use FishingGameConfig values if they were set by a previous scene.
        // The Inspector values act as defaults if nothing sets the config.
        if (!string.IsNullOrEmpty(FishingGameConfig.TargetLanguage))
            targetLanguage = FishingGameConfig.TargetLanguage;
        if (!string.IsNullOrEmpty(FishingGameConfig.CategoryFilter))
            categoryFilter = FishingGameConfig.CategoryFilter;

        // Wait just a tiny fraction of a second to make sure all the fishes in the pond have spawned!
        Invoke("PopulateListAndAssignFishes", 0.1f);
    }

    void PopulateListAndAssignFishes()
    {
        if (jsonFile == null || listItemPrefab == null || contentParent == null) return;

        PhraseList data = JsonUtility.FromJson<PhraseList>(jsonFile.text);
        if (data == null || data.phrases == null) return;

        List<PhraseData> validPhrases = new List<PhraseData>();
        foreach (var phrase in data.phrases)
        {
            if (phrase.category == categoryFilter || string.IsNullOrEmpty(categoryFilter)) 
                validPhrases.Add(phrase);
        }

        // Shuffle the words randomly
        for (int i = 0; i < validPhrases.Count; i++)
        {
            PhraseData temp = validPhrases[i];
            int randomIndex = Random.Range(i, validPhrases.Count);
            validPhrases[i] = validPhrases[randomIndex];
            validPhrases[randomIndex] = temp;
        }

        // Find ALL the swimming fishes in the scene
        FishingQuizManager fqm = Object.FindFirstObjectByType<FishingQuizManager>();
        FishController[] swimmingFishes = FindObjectsByType<FishController>(FindObjectsSortMode.None);

        // We only need as many words as we have fishes in the pond!
        int itemsToSpawn = Mathf.Min(validPhrases.Count, swimmingFishes.Length);

        for (int i = 0; i < itemsToSpawn; i++)
        {
            PhraseData assignedPhrase = validPhrases[i];
            FishController theFish = swimmingFishes[i];

            string wordToDisplay = "";
            if (targetLanguage.ToLower() == "cebuano") wordToDisplay = assignedPhrase.cebuano;
            else if (targetLanguage.ToLower() == "ilokano") wordToDisplay = assignedPhrase.ilokano;
            else wordToDisplay = assignedPhrase.english;

            // 1. Tell the swimming fish what word it represents!
            theFish.assignedWord = wordToDisplay;
            theFish.assignedId = assignedPhrase.id;

            // 2. Create the dictionary entry in the list, and use the exact picture of that specific fish!
            CreateListItem(assignedPhrase, theFish.iconSprite, wordToDisplay);
        }
    }

    void CreateListItem(PhraseData phraseData, Sprite fishSprite, string word)
    {
        GameObject newItem = Instantiate(listItemPrefab, contentParent);
        FishListItem itemScript = newItem.GetComponent<FishListItem>();
        
        if (itemScript != null)
        {
            itemScript.Setup(fishSprite, word);
        }
    }
}
