using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

[System.Serializable]
public class TusokDistractorData
{
    public string phraseId;
    public string feedback;
}

[System.Serializable]
public class CountingRoundData
{
    public string category;
    public string englishDialogue;
    public string ilokanoDialogue;
    public string cebuanoDialogue;
    public int targetFishball;
    public int targetKwekkwek;
    public int targetKikiam;
    public int targetHotdog;
    public string ilokanoTargetWords;
    public string cebuanoTargetWords;
    public string englishFeedback;
    public string ilokanoFeedback;
    public string cebuanoFeedback;
    public string wrongFeedback;
    public string ilokanoWrongFeedback;
    public string cebuanoWrongFeedback;

    // Recall Round Fields
    public bool isRecall;
    public string situationText;
    public string correctPhraseId;
    public TusokDistractorData[] distractors;
}

public class TusokTusokGameManager : MonoBehaviour
{
    public static TusokTusokGameManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI chatBubbleText;
    public Image correctOrWrongImage; // Changed to Image
    public Sprite correctPopupSprite;
    public Sprite wrongPopupSprite;
    public UnityEngine.UI.Button submitButton;
    public UnityEngine.UI.Button translateButton;

    private string currentNativeText;
    private string currentEnglishText;
    private bool isShowingEnglish = false;
    
    [Header("Win/Lose UI")]
    public GameObject winOrLoseGroup;
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI winCoinsText;
    public TextMeshProUGUI loseCoinsText;
    public Image[] winStars;
    public Sprite activeStar;
    public Sprite inactiveStar;
    public AudioClip winSFX;
    public AudioClip loseSFX;

    [Header("How To Play UI")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;

    [Header("STT Colors")]
    public Color sttNormalColor = Color.black;
    public Color sttProcessingColor = Color.cyan;
    public Color sttCorrectColor = new Color(0, 0.5f, 0); // Dark Green
    public Color sttWrongColor = Color.red;

    [Header("STT Translations")]
    public string englishSTTPrompt1 = "Can you say {0}?";
    public string ilokanoSTTPrompt1 = "Mabalin mo nga ibaga ti {0}?";
    public string cebuanoSTTPrompt1 = "Puwede nimo isulti ang {0}?";

    public string englishSTTPrompt2 = "How about {0}?";
    public string ilokanoSTTPrompt2 = "Kasano man ti {0}?";
    public string cebuanoSTTPrompt2 = "Unsa man ang {0}?";

    public string englishSTTWrong = "That doesn't sound right.";
    public string ilokanoSTTWrong = "Madi ti ibagbagam.";
    public string cebuanoSTTWrong = "Sayop imong gisulti.";

    [Header("Target UI")]
    public TextMeshProUGUI fishballTargetText;
    public TextMeshProUGUI kwekkwekTargetText;
    public TextMeshProUGUI kikiamTargetText;
    public TextMeshProUGUI hotdogTargetText;

    [Header("Hearts")]
    public Image[] hearts; // Changed to Image array
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("NPC Sprites")]
    public Image manongImage;
    public Sprite manongIdle;
    public Sprite manongHappy;
    public Sprite manongWrong;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonClickSFX;
    public AudioClip panelPopupSFX;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip tusokSFX;
    public AudioClip returnSFX;

    [Header("Groups")]
    public GameObject handGroup;
    public GameObject recallGroup; // Contains the 3 Choice buttons

    [Header("Data")]
    [Tooltip("If left empty, the game will automatically try to load a JSON file from Resources matching the SelectedCategory.")]
    public TextAsset fallbackJsonData;

    private List<CountingRoundData> rounds = new List<CountingRoundData>();
    private int currentRoundIndex = 0;
    private int currentHearts = 5;
    private bool hasGameStarted = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadData();
        submitButton.onClick.AddListener(OnSubmitClicked);
        if (translateButton != null) translateButton.onClick.AddListener(OnTranslateClicked);
        ShowHowToPlay();
    }


    public void ShowHowToPlay()
    {
        if (audioSource != null && panelPopupSFX != null) audioSource.PlayOneShot(panelPopupSFX);
        
        if (howToPlayGroup != null) 
        {
            howToPlayGroup.SetActive(true);
            howToPlayGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
    }

    public void CloseHowToPlay()
    {
        if (audioSource != null && buttonClickSFX != null) audioSource.PlayOneShot(buttonClickSFX);
        
        if (howToPlayPanel != null) howToPlayPanel.transform.localScale = Vector3.zero; // INSTANT SNAP
        if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
        
        if (!hasGameStarted)
        {
            hasGameStarted = true;
            StartRound();
        }
    }

    public void SetChatBubbleText(string nativeText, string englishText)
    {
        currentNativeText = nativeText;
        currentEnglishText = englishText;
        chatBubbleText.text = isShowingEnglish ? currentEnglishText : currentNativeText;
    }

    private void OnTranslateClicked()
    {
        isShowingEnglish = !isShowingEnglish;
        UpdateChatBubbleVisuals();
    }

    private void UpdateChatBubbleVisuals()
    {
        chatBubbleText.text = isShowingEnglish ? currentEnglishText : currentNativeText;
    }

    public void UpdateChatBubbleColorText(string text, Color color)
    {
        currentEnglishText = text;
        currentNativeText = text;
        UpdateChatBubbleVisuals();
        chatBubbleText.color = color;
    }

    public void SetManongSprite(bool isHappy, bool isIdle = false)
    {
        if (isIdle) manongImage.sprite = manongIdle;
        else manongImage.sprite = isHappy ? manongHappy : manongWrong;
    }

    public void ShowSTTPrompt(string word, bool isFirstWord)
    {
        string selectedLang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        
        string engTemplate = isFirstWord ? englishSTTPrompt1 : englishSTTPrompt2;
        string nativeTemplate = isFirstWord ? (selectedLang == "Ilokano" ? ilokanoSTTPrompt1 : cebuanoSTTPrompt1) : (selectedLang == "Ilokano" ? ilokanoSTTPrompt2 : cebuanoSTTPrompt2);

        string engText = string.Format(engTemplate, $"<color=#006400>{word}</color>");
        string nativeText = string.Format(nativeTemplate, $"<color=#006400>{word}</color>");

        chatBubbleText.color = sttNormalColor;
        SetChatBubbleText(nativeText, engText);
    }

    public void ShowSTTWrongFeedback()
    {
        string selectedLang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        string nativeWrong = selectedLang == "Ilokano" ? ilokanoSTTWrong : cebuanoSTTWrong;
        
        chatBubbleText.color = sttWrongColor;
        SetChatBubbleText(nativeWrong, englishSTTWrong);
        SetManongSprite(false);
    }

    public void CompleteRound()
    {
        StartCoroutine(AdvanceRoundRoutine());
    }

    public bool isSTTPhaseActive = false;

    public void ResetRoundDueToSTTFail()
    {
        StartCoroutine(ResetRoundRoutine());
    }

    private IEnumerator ResetRoundRoutine()
    {
        isSTTPhaseActive = false;
        
        // Wait 2 seconds so the player can see the Wrong popup
        yield return new WaitForSeconds(2f);
        
        // Don't deduct heart, just reset the stick and let them try again
        if (TusokStickManager.Instance != null) TusokStickManager.Instance.ClearStick();
        
        // Restart round UI
        StartRound();
    }

    private string GetHybridEnglishDialogue(string nativeDialogue, string englishDialogue)
    {
        if (string.IsNullOrEmpty(nativeDialogue) || string.IsNullOrEmpty(englishDialogue)) return englishDialogue;

        MatchCollection nativeMatches = Regex.Matches(nativeDialogue, "<color=.*?>(.*?)</color>");
        if (nativeMatches.Count == 0) return englishDialogue;

        int matchIndex = 0;
        string hybridDialogue = Regex.Replace(englishDialogue, "<color=.*?>(.*?)</color>", match =>
        {
            if (matchIndex < nativeMatches.Count)
            {
                string replacement = nativeMatches[matchIndex].Value;
                matchIndex++;
                return replacement;
            }
            return match.Value;
        });

        return hybridDialogue;
    }

    private void LoadData()
    {
        string selectedCategory = PlayerPrefs.GetString("SelectedCategory", "Count");
        TextAsset jsonAsset = Resources.Load<TextAsset>(selectedCategory);
        
        // If it wasn't found in the root Resources folder, we try the fallback
        if (jsonAsset == null)
        {
            jsonAsset = fallbackJsonData;
        }

        if (jsonAsset != null)
        {
            string jsonString = "{\"items\":" + jsonAsset.text + "}";
            CountingRoundDataList dataList = JsonUtility.FromJson<CountingRoundDataList>(jsonString);
            if (dataList != null)
            {
                BuildRoundList(new List<CountingRoundData>(dataList.items));
            }
        }
        else
        {
            Debug.LogError($"Could not find JSON data for category: {selectedCategory}");
        }
    }

    private void BuildRoundList(List<CountingRoundData> allData)
    {
        List<CountingRoundData> countingPool = new List<CountingRoundData>();
        List<CountingRoundData> recallPool = new List<CountingRoundData>();

        foreach (var data in allData)
        {
            if (data.isRecall) recallPool.Add(data);
            else countingPool.Add(data);
        }

        // Shuffle counting pool and take 15
        ShuffleList(countingPool);
        List<CountingRoundData> selectedRounds = new List<CountingRoundData>();
        for (int i = 0; i < Mathf.Min(15, countingPool.Count); i++)
        {
            selectedRounds.Add(countingPool[i]);
        }

        // Select 5 recall rounds using weights based on category recency
        Dictionary<string, int> categoryWeights = new Dictionary<string, int>
        {
            { "Greetings", 1 }, { "Gratitude", 2 }, { "Responses", 3 },
            { "Identity", 4 }, { "Requests", 5 }, { "Directions", 6 }
        };

        for (int i = 0; i < 5 && recallPool.Count > 0; i++)
        {
            int totalWeight = 0;
            foreach (var r in recallPool)
            {
                totalWeight += categoryWeights.ContainsKey(r.category) ? categoryWeights[r.category] : 1;
            }

            int randomVal = Random.Range(0, totalWeight);
            int currentSum = 0;
            for (int j = 0; j < recallPool.Count; j++)
            {
                currentSum += categoryWeights.ContainsKey(recallPool[j].category) ? categoryWeights[recallPool[j].category] : 1;
                if (randomVal < currentSum)
                {
                    selectedRounds.Add(recallPool[j]);
                    recallPool.RemoveAt(j);
                    break;
                }
            }
        }

        // Shuffle the final 20 rounds so recall and counting are mixed!
        ShuffleList(selectedRounds);
        rounds = selectedRounds;
    }

    private void ShuffleList(List<CountingRoundData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            CountingRoundData temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public bool IsRecallRound()
    {
        if (rounds == null || rounds.Count == 0 || currentRoundIndex >= rounds.Count) return false;
        return rounds[currentRoundIndex].isRecall;
    }

    private void StartRound()
    {
        if (currentRoundIndex >= rounds.Count)
        {
            ShowWinScreen();
            return;
        }

        CountingRoundData currentRound = rounds[currentRoundIndex];
        
        roundText.text = $"Round {currentRoundIndex + 1}/20";
        
        // Hide the popup image at the start of the round
        if (correctOrWrongImage != null) correctOrWrongImage.gameObject.SetActive(false);
        
        manongImage.sprite = manongIdle;
        chatBubbleText.color = sttNormalColor; // Reset to normal font color

        string selectedLang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");

        if (currentRound.isRecall)
        {
            // Set up Recall UI
            if (handGroup != null) handGroup.SetActive(false);
            if (submitButton != null) submitButton.gameObject.SetActive(false);
            if (recallGroup != null) recallGroup.SetActive(true);

            SetChatBubbleText(currentRound.situationText, currentRound.situationText);

            // Start Recall manager
            if (TusokRecallManager.Instance != null)
            {
                TusokRecallManager.Instance.StartRecallRound(currentRound);
            }
        }
        else
        {
            // Set up Counting UI
            if (handGroup != null) handGroup.SetActive(true);
            if (submitButton != null) submitButton.gameObject.SetActive(true);
            if (recallGroup != null) recallGroup.SetActive(false);
            
            chatBubbleText.color = sttNormalColor;
            
            // Clear the stick from previous round
            if (TusokStickManager.Instance != null) TusokStickManager.Instance.ClearStick();

            // Use appropriate language dialogue
            string nativeDialogue = selectedLang == "Ilokano" ? currentRound.ilokanoDialogue : (selectedLang == "Cebuano" ? currentRound.cebuanoDialogue : currentRound.englishDialogue);
            string hybridEnglish = GetHybridEnglishDialogue(nativeDialogue, currentRound.englishDialogue);
            SetChatBubbleText(nativeDialogue, hybridEnglish);

            // Start at 0 for the inventory UI
            UpdateInventoryUI();
        }
    }

    public void UpdateInventoryUI()
    {
        if (TusokStickManager.Instance == null) return;
        
        int fbCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Fishball);
        int kkCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Kwekkwek);
        int kCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Kikiam);
        int hdCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Hotdog);

        if (fishballTargetText != null) fishballTargetText.text = $"x {fbCount}";
        if (kwekkwekTargetText != null) kwekkwekTargetText.text = $"x {kkCount}";
        if (kikiamTargetText != null) kikiamTargetText.text = $"x {kCount}";
        if (hotdogTargetText != null) hotdogTargetText.text = $"x {hdCount}";
    }

    private void OnSubmitClicked()
    {
        if (currentRoundIndex >= rounds.Count) return;

        CountingRoundData currentRound = rounds[currentRoundIndex];
        
        int fbCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Fishball);
        int kkCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Kwekkwek);
        int kCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Kikiam);
        int hdCount = TusokStickManager.Instance.GetFoodCount(TusokWokItem.FoodType.Hotdog);

        bool isCorrect = fbCount == currentRound.targetFishball &&
                         kkCount == currentRound.targetKwekkwek &&
                         kCount == currentRound.targetKikiam &&
                         hdCount == currentRound.targetHotdog;

        string selectedLang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");

        if (!isCorrect)
        {
            if (correctOrWrongImage != null)
            {
                correctOrWrongImage.gameObject.SetActive(true);
                UIPopAnimator popAnim = correctOrWrongImage.GetComponent<UIPopAnimator>();
            if (popAnim != null) popAnim.PopIn();
            }
        }

        if (isCorrect)
        {
            if (audioSource != null && correctSFX != null) audioSource.PlayOneShot(correctSFX);
            manongImage.sprite = manongHappy;
            
            // Put the feedback text inside the chat bubble!
            string nativeFeedback = selectedLang == "Ilokano" ? currentRound.ilokanoFeedback : (selectedLang == "Cebuano" ? currentRound.cebuanoFeedback : currentRound.englishFeedback);
            SetChatBubbleText(nativeFeedback, currentRound.englishFeedback);

            // Prevent interacting with wok items and stick items during STT
            isSTTPhaseActive = true;

            if (TusokSTTManager.Instance != null)
            {
                if (submitButton != null) submitButton.gameObject.SetActive(false);
                TusokSTTManager.Instance.StartSTT(currentRound);
            }
            else
            {
                // Fallback if no STTManager
                StartCoroutine(AdvanceRoundRoutine());
            }
        }
        else
        {
            if (audioSource != null && wrongSFX != null) audioSource.PlayOneShot(wrongSFX);
            manongImage.sprite = manongWrong;
            if (correctOrWrongImage != null) correctOrWrongImage.sprite = wrongPopupSprite;
            
            // Put the feedback text inside the chat bubble!
            string nativeWrong = selectedLang == "Ilokano" ? currentRound.ilokanoWrongFeedback : (selectedLang == "Cebuano" ? currentRound.cebuanoWrongFeedback : currentRound.wrongFeedback);
            SetChatBubbleText(nativeWrong, currentRound.wrongFeedback);

            LoseHeart();
            
            if (currentHearts > 0)
            {
                StartCoroutine(WrongFeedbackRoutine(currentRound, selectedLang));
            }
        }
    }

    public void ShowCorrectPopup()
    {
        if (correctOrWrongImage != null)
        {
            correctOrWrongImage.sprite = correctPopupSprite;
            correctOrWrongImage.gameObject.SetActive(true);
            UIPopAnimator popAnim = correctOrWrongImage.GetComponent<UIPopAnimator>();
            if (popAnim != null) popAnim.PopIn();
        }
    }

    public void ShowWrongPopup()
    {
        if (correctOrWrongImage != null)
        {
            correctOrWrongImage.sprite = wrongPopupSprite;
            correctOrWrongImage.gameObject.SetActive(true);
            UIPopAnimator popAnim = correctOrWrongImage.GetComponent<UIPopAnimator>();
            if (popAnim != null) popAnim.PopIn();
        }
    }

    private IEnumerator WrongFeedbackRoutine(CountingRoundData roundData, string lang)
    {
        submitButton.interactable = false;
        yield return new WaitForSeconds(2.5f);
        
        // Revert UI to let them try again
        if (correctOrWrongImage != null) correctOrWrongImage.gameObject.SetActive(false);
        manongImage.sprite = manongIdle;
        
        string revertNative = lang == "Ilokano" ? roundData.ilokanoDialogue : (lang == "Cebuano" ? roundData.cebuanoDialogue : roundData.englishDialogue);
        string revertEnglish = GetHybridEnglishDialogue(revertNative, roundData.englishDialogue);
        SetChatBubbleText(revertNative, revertEnglish);
        
        submitButton.interactable = true;
    }

    private IEnumerator AdvanceRoundRoutine()
    {
        submitButton.interactable = false;
        yield return new WaitForSeconds(2f);
        currentRoundIndex++;
        submitButton.interactable = true;
        StartRound();
    }

    private void LoseHeart()
    {
        if (currentHearts <= 0) return;
        currentHearts--;
        UpdateHeartsUI();
        
        if (currentHearts <= 0)
        {
            ShowLoseScreen();
        }
    }

    private void UpdateHeartsUI()
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
                hearts[i].sprite = (i < currentHearts) ? fullHeartSprite : emptyHeartSprite;
        }
    }

    // --- Recall Event Callbacks ---
    public void OnRecallCorrect(CountingRoundData roundData, string lang)
    {
        if (audioSource != null && correctSFX != null) audioSource.PlayOneShot(correctSFX);
        
        if (correctOrWrongImage != null)
        {
            correctOrWrongImage.gameObject.SetActive(true);
            correctOrWrongImage.sprite = correctPopupSprite;
            if (_localPopupAnim != null) StopCoroutine(_localPopupAnim);
            _localPopupAnim = StartCoroutine(SafePopupAnim(correctOrWrongImage.transform));
        }
        
        manongImage.sprite = manongHappy;
        SetChatBubbleText(roundData.englishFeedback, roundData.englishFeedback); // Or localized if needed
            // Start STT Phase
            if (TusokSTTManager.Instance != null)
            {
                if (recallGroup != null) recallGroup.SetActive(false);
                TusokSTTManager.Instance.StartSTT(roundData);
            }
            else
            {
                StartCoroutine(AdvanceRoundRoutine());
            }
        }

    public void OnRecallWrong(string distractorFeedback, CountingRoundData roundData, string lang)
    {
        if (audioSource != null && wrongSFX != null) audioSource.PlayOneShot(wrongSFX);
        
        if (correctOrWrongImage != null)
        {
            correctOrWrongImage.gameObject.SetActive(true);
            correctOrWrongImage.sprite = wrongPopupSprite;
            if (_localPopupAnim != null) StopCoroutine(_localPopupAnim);
            _localPopupAnim = StartCoroutine(SafePopupAnim(correctOrWrongImage.transform));
        }
        
        manongImage.sprite = manongWrong;
        SetChatBubbleText(distractorFeedback, distractorFeedback);
        
        LoseHeart();
        
        if (currentHearts > 0)
        {
            // Use same routine, it will revert back to situationText
            StartCoroutine(WrongRecallRoutine(roundData, lang));
        }
    }



    public void PlayTusokSFX()
    {
        if (audioSource != null && tusokSFX != null) audioSource.PlayOneShot(tusokSFX);
    }

    public void PlayReturnSFX()
    {
        if (audioSource != null && returnSFX != null) audioSource.PlayOneShot(returnSFX);
    }

    private IEnumerator WrongRecallRoutine(CountingRoundData roundData, string lang)
    {
        // Disable buttons temporarily
        if (recallGroup != null) recallGroup.SetActive(false);
        
        yield return new WaitForSeconds(2.5f);
        
        if (correctOrWrongImage != null) correctOrWrongImage.gameObject.SetActive(false);
        manongImage.sprite = manongIdle;
        SetChatBubbleText(roundData.situationText, roundData.situationText);
        
        if (recallGroup != null) recallGroup.SetActive(true);
    }

    private void ShowWinScreen()
    {
        if (audioSource != null && winSFX != null) audioSource.PlayOneShot(winSFX);
        
        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            UIPopAnimator popAnim = winPanel.GetComponent<UIPopAnimator>();
            if (popAnim != null) popAnim.PopIn();
        }
        if (losePanel != null) losePanel.SetActive(false);

        int stars = 0;
        int coinsEarned = 0;

        // Based on hearts left
        if (currentHearts >= 5) { stars = 5; coinsEarned = 50; }
        else if (currentHearts == 4) { stars = 4; coinsEarned = 40; }
        else if (currentHearts == 3) { stars = 3; coinsEarned = 30; }
        else if (currentHearts == 2) { stars = 2; coinsEarned = 20; }
        else if (currentHearts == 1) { stars = 1; coinsEarned = 10; }
        else { stars = 0; coinsEarned = 5; }

        if (winStars != null)
        {
            for (int i = 0; i < winStars.Length; i++)
            {
                if (winStars[i] != null)
                    winStars[i].sprite = (i < stars) ? activeStar : inactiveStar;
            }
        }

        if (winCoinsText != null) winCoinsText.text = $"+{coinsEarned}";

        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
        PlayerPrefs.SetInt("TusokTusokMinigameWon", 1);
        PlayerPrefs.Save();
    }

    private void ShowLoseScreen()
    {
        if (audioSource != null && loseSFX != null) audioSource.PlayOneShot(loseSFX);

        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) 
        {
            losePanel.SetActive(true);
            UIPopAnimator popAnim = losePanel.GetComponent<UIPopAnimator>();
            if (popAnim != null) popAnim.PopIn();
        }
        
        if (loseCoinsText != null) loseCoinsText.text = "+2";

        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentCoins + 2);
        PlayerPrefs.Save();
    }

    public void RestartGame()
    {
        if (audioSource != null && buttonClickSFX != null) audioSource.PlayOneShot(buttonClickSFX);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        if (audioSource != null && buttonClickSFX != null) audioSource.PlayOneShot(buttonClickSFX);
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene");
        SceneManager.LoadScene(prevScene);
    }

    private Coroutine _localPopupAnim;
    private System.Collections.IEnumerator SafePopupAnim(Transform target)
    {
        target.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(0f, 1.1f, elapsed / 0.15f);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, elapsed / 0.15f);
            yield return null;
        }
        target.localScale = Vector3.one;
        _localPopupAnim = null;
    }

    [System.Serializable]
    public class CountingRoundDataList
    {
        public CountingRoundData[] items;
    }
}
