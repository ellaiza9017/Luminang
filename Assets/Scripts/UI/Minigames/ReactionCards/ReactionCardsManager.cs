using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GratitudeDistractorData
{
    public string phraseId;
    public string feedback;
}

[System.Serializable]
public class GratitudeRoundData
{
    public string situationText;
    public string correctPhraseId;
    public List<GratitudeDistractorData> distractors;
}

[System.Serializable]
public class ReactionCardImageMapping
{
    [Tooltip("Must perfectly match the situationText in Gratitude.json")]
    [TextArea(2, 3)]
    public string situationText;
    public Sprite cardImage;
}

public class ReactionCardsManager : MonoBehaviour
{
    public static ReactionCardsManager Instance;

    [Header("Data References")]
    [Tooltip("The name of the JSON file in Resources (e.g., 'Gratitude')")]
    public string jsonFileName = "Gratitude";
    public List<ReactionCardImageMapping> imageMappings;
    public Sprite defaultMemorySprite;

    [Header("Core UI")]
    public TextMeshProUGUI questionText;
    public Image polaroidPic;
    public CanvasGroup polaroidMask; // Used for fading out during animation
    public ReactionCardAnimator handAnimator;
    public TextMeshProUGUI roundsFractionText;
    
    [Header("Choice Buttons")]
    public Button[] choiceButtons; // Assign ButtonCard1, ButtonCard2, ButtonCard3
    public TextMeshProUGUI[] choiceTexts; // Assign the text children of the buttons

    [Header("Lives & Feedback")]
    public Image[] hearts; // Assign Heart1 to Heart5
    public Sprite activeHeart;
    public Sprite inactiveHeart;
    public GameObject wrongFeedbackGroup;
    public TextMeshProUGUI wrongFeedbackText;
    public RectTransform shakeTarget; // Usually the GameGroup or HandPolaroid
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 10f;

    [Header("Audio")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSFX;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip winSFX;
    public AudioClip loseSFX;
    public AudioClip cardFlipSFX;

    private int pendingRewardCoins = 0;

    [Header("How To Play UI")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;

    [Header("Win/Lose UI")]
    public GameObject winOrLoseGroup;
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI winCoinsText;
    public TextMeshProUGUI loseCoinsText;
    public Image[] winStars;
    public Sprite activeStar;
    public Sprite inactiveStar;

    private List<GratitudeRoundData> roundPool = new List<GratitudeRoundData>();
    private GratitudeRoundData currentRoundData;

#if UNITY_EDITOR
    [Header("--- EDITOR DEBUG (hidden in build) ---")]
    public int currentHearts = 5;
    public int currentRoundNumber = 1;
#else
    private int currentHearts = 5;
    private int currentRoundNumber = 1;
#endif

    private int totalRounds = 15;
    private bool isInputBlocked = false;
    private bool hasGameStarted = false;
    private Color defaultQuestionColor = Color.black;

    private class JsonWrapper
    {
        public GratitudeRoundData[] items;
    }

    private void Awake()
    {
        Instance = this;
    }

#if UNITY_EDITOR
    private void Update()
    {
        // EDITOR CHEATS — stripped from builds completely
        // W → instantly trigger Win Screen
        // L → instantly trigger Lose Screen
        // P → pass STT (advance round without speaking)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame) ShowWinScreen();
        if (kb.lKey.wasPressedThisFrame) ShowLoseScreen();
        if (kb.pKey.wasPressedThisFrame) CompleteSTTAndAdvanceRound();
    }
#endif

    private void Start()
    {
        if (questionText != null) defaultQuestionColor = questionText.color;
        
        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            howToPlayGroup.SetActive(true);
            howToPlayGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        else
        {
            LoadDataAndStart();
        }
    }

    public void OpenHowToPlay()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);

        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            howToPlayGroup.SetActive(true);
            howToPlayGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
    }

    public void CloseHowToPlay()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);

        if (howToPlayPanel != null) howToPlayPanel.transform.localScale = Vector3.zero;
        if (howToPlayGroup != null) howToPlayGroup.SetActive(false);

        if (!hasGameStarted)
        {
            LoadDataAndStart();
        }
    }

    [Header("Editor Debug")]
    [Tooltip("If true, ignores PlayerPrefs and forces the game to load the jsonFileName specified above. Useful for testing directly in this scene!")]
    public bool forceInspectorJsonInEditor = false;

    private void LoadDataAndStart()
    {
        hasGameStarted = true;
        // Check if the previous scene told us exactly which category to load
        string overrideJson = PlayerPrefs.GetString("ReactionCardCategory", "");

#if UNITY_EDITOR
        if (forceInspectorJsonInEditor)
        {
            overrideJson = ""; // Force it to use the inspector value
            Debug.LogWarning("[ReactionCardsManager] forceInspectorJsonInEditor is TRUE. Ignoring PlayerPrefs and loading: " + jsonFileName);
        }
#endif

        if (!string.IsNullOrEmpty(overrideJson))
        {
            jsonFileName = overrideJson;
            // NOTE: We do NOT clear the key here — we keep it so Try Again still loads the right JSON.
            // It gets cleared in QuitToMenu() and RestartGame() when the player actually exits.
        }

        Debug.Log($"[ReactionCardsManager] Currently loading JSON file: {jsonFileName}");

        // Load JSON
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);
        if (jsonFile != null)
        {
            string wrappedJson = "{\"items\":" + jsonFile.text + "}";
            JsonWrapper wrapper = JsonUtility.FromJson<JsonWrapper>(wrappedJson);
            if (wrapper != null && wrapper.items != null)
            {
                List<GratitudeRoundData> allLoaded = new List<GratitudeRoundData>(wrapper.items);
                List<GratitudeRoundData> standardRounds = new List<GratitudeRoundData>();
                List<GratitudeRoundData> memoryRounds = new List<GratitudeRoundData>();

                // Separate memory rounds from standard rounds
                foreach (var item in allLoaded)
                {
                    if (!string.IsNullOrEmpty(item.situationText) && item.situationText.TrimStart().StartsWith("[MEMORY]", System.StringComparison.OrdinalIgnoreCase))
                    {
                        memoryRounds.Add(item);
                    }
                    else
                    {
                        standardRounds.Add(item);
                    }
                }

                if (jsonFileName.Equals("Pronouns", System.StringComparison.OrdinalIgnoreCase))
                {
                    // For Pronouns: 9 standard, 6 memory
                    
                    // Shuffle standard rounds and take up to 9
                    for (int i = 0; i < standardRounds.Count; i++)
                    {
                        GratitudeRoundData temp = standardRounds[i];
                        int randomIndex = Random.Range(i, standardRounds.Count);
                        standardRounds[i] = standardRounds[randomIndex];
                        standardRounds[randomIndex] = temp;
                    }
                    if (standardRounds.Count > 9)
                        standardRounds.RemoveRange(9, standardRounds.Count - 9);

                    // Shuffle memory rounds and take up to 6
                    if (memoryRounds.Count > 0)
                    {
                        for (int i = 0; i < memoryRounds.Count; i++)
                        {
                            GratitudeRoundData temp = memoryRounds[i];
                            int randomIndex = Random.Range(i, memoryRounds.Count);
                            memoryRounds[i] = memoryRounds[randomIndex];
                            memoryRounds[randomIndex] = temp;
                        }

                        int memoryCountToTake = Mathf.Min(6, memoryRounds.Count);
                        for (int i = 0; i < memoryCountToTake; i++)
                        {
                            standardRounds.Add(memoryRounds[i]);
                        }
                    }
                    roundPool = standardRounds;
                }
                else
                {
                    // For Gratitude (and anything else): just combine them all and take up to 15
                    List<GratitudeRoundData> combined = new List<GratitudeRoundData>();
                    combined.AddRange(standardRounds);
                    combined.AddRange(memoryRounds);
                    
                    // Shuffle combined
                    for (int i = 0; i < combined.Count; i++)
                    {
                        GratitudeRoundData temp = combined[i];
                        int randomIndex = Random.Range(i, combined.Count);
                        combined[i] = combined[randomIndex];
                        combined[randomIndex] = temp;
                    }

                    if (combined.Count > 15)
                        combined.RemoveRange(15, combined.Count - 15);
                        
                    roundPool = combined;
                }
            }
        }
        else
        {
            Debug.LogError($"[ReactionCardsManager] Could not find {jsonFileName}.json in Resources!");
            return;
        }

        // Shuffle rounds
        for (int i = 0; i < roundPool.Count; i++)
        {
            GratitudeRoundData temp = roundPool[i];
            int randomIndex = Random.Range(i, roundPool.Count);
            roundPool[i] = roundPool[randomIndex];
            roundPool[randomIndex] = temp;
        }

        currentHearts = 5;
        currentRoundNumber = 1;
        totalRounds = roundPool.Count;
        
        UpdateHeartsUI();
        wrongFeedbackGroup.SetActive(false);
        winOrLoseGroup.SetActive(false);
        isInputBlocked = false;

        NextRound();
    }

    private void NextRound()
    {
        if (currentRoundNumber > totalRounds || roundPool.Count == 0)
        {
            ShowWinScreen();
            return;
        }

        currentRoundData = roundPool[0];
        roundPool.RemoveAt(0);

        roundsFractionText.text = $"{currentRoundNumber}/{totalRounds}";
        questionText.text = currentRoundData.situationText;

        // Set Polaroid Sprite
        Sprite foundSprite = null;
        foreach (var mapping in imageMappings)
        {
            // Clean up strings: lower case, trim, remove literal backslashes, and remove underscores (to ignore "___" vs "____" typos)
            string inspectorText = mapping.situationText.Replace("\\\"", "\"").Replace("_", "").Trim().ToLower();
            string loadedText = currentRoundData.situationText.Replace("\\\"", "\"").Replace("_", "").Trim().ToLower();

            if (inspectorText == loadedText)
            {
                foundSprite = mapping.cardImage;
                break;
            }
        }

        if (foundSprite != null)
        {
            polaroidPic.sprite = foundSprite;
        }
        else if (currentRoundData.situationText.TrimStart().StartsWith("[MEMORY]", System.StringComparison.OrdinalIgnoreCase) && defaultMemorySprite != null)
        {
            polaroidPic.sprite = defaultMemorySprite;
        }
        else
        {
            Debug.LogWarning($"[ReactionCardsManager] No image mapping found for situation: {currentRoundData.situationText}");
        }

        SetupChoiceButtons();
        isInputBlocked = false;
    }

    private void SetupChoiceButtons()
    {
        // We have 1 correct and up to 2 distractors
        List<string> options = new List<string> { currentRoundData.correctPhraseId };
        
        foreach (var dist in currentRoundData.distractors)
        {
            if (options.Count < 3) options.Add(dist.phraseId);
        }

        // Shuffle options
        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int r = Random.Range(i, options.Count);
            options[i] = options[r];
            options[r] = temp;
        }

        string lang = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Count)
            {
                choiceButtons[i].gameObject.SetActive(true);
                string phraseId = options[i];

                // Ensure DatasetManager exists (in case they test the scene directly without going through MainMenu)
                if (DatasetManager.Instance == null)
                {
                    gameObject.AddComponent<DatasetManager>();
                }

                // Fetch translation from DatasetManager
                PhraseEntry entry = DatasetManager.Instance?.GetPhraseById(phraseId);
                
                string translatedText = $"[{phraseId}]";
                if (entry != null)
                {
                    if (jsonFileName.Equals("Pronouns", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // For Pronouns, show English word on top and local translation in parentheses below
                        translatedText = $"{entry.english}\n<size=80%><color=#555555>({entry.GetPhrase(lang)})</color></size>";
                    }
                    else
                    {
                        // For other categories (like Gratitude), just show the local phrase normally
                        translatedText = entry.GetPhrase(lang);
                    }
                }

                choiceTexts[i].text = translatedText;

                // Setup listener
                int index = i; // capture for closure
                choiceButtons[i].interactable = true;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnAnswerSelected(phraseId));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnAnswerSelected(string selectedPhraseId)
    {
        if (isInputBlocked) return;
        
        if (uiAudioSource != null && buttonClickSFX != null) 
            uiAudioSource.PlayOneShot(buttonClickSFX);

        if (selectedPhraseId == currentRoundData.correctPhraseId)
        {
            // Correct Answer -> Trigger STT Phase
            isInputBlocked = true;
            if (uiAudioSource != null && correctSFX != null) uiAudioSource.PlayOneShot(correctSFX);

            foreach (var btn in choiceButtons) btn.interactable = false;

            if (ReactionCardsSTTManager.Instance != null)
            {
                ReactionCardsSTTManager.Instance.StartSTT(selectedPhraseId);
            }
            else
            {
                // Fallback if STT manager is missing
                StartCoroutine(CorrectAnswerSequence());
            }
        }
        else
        {
            // Wrong Answer
            if (uiAudioSource != null && wrongSFX != null) uiAudioSource.PlayOneShot(wrongSFX);
            
            currentHearts--;
            UpdateHeartsUI();

            // Find specific feedback
            string feedbackMsg = "Try again!";
            foreach (var dist in currentRoundData.distractors)
            {
                if (dist.phraseId == selectedPhraseId)
                {
                    feedbackMsg = dist.feedback;
                    break;
                }
            }

            StartCoroutine(ShakeScreen());
            StartCoroutine(ShowWrongFeedback(feedbackMsg));

            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif

            if (currentHearts <= 0)
            {
                isInputBlocked = true;
                ShowLoseScreen();
            }
        }
    }

    public void UpdateQuestionText(string text, Color color)
    {
        if (questionText != null)
        {
            questionText.text = text;
            questionText.color = color;
        }
    }

    public void CompleteSTTAndAdvanceRound()
    {
        if (ReactionCardsSTTManager.Instance != null) ReactionCardsSTTManager.Instance.ForceHidePanel();
        StartCoroutine(CorrectAnswerSequence());
    }

    public void CompleteSTTAndFailRound()
    {
        isInputBlocked = false;
        
        if (ReactionCardsSTTManager.Instance != null) ReactionCardsSTTManager.Instance.ForceHidePanel();
        
        // Re-enable buttons and restore original text
        foreach (var btn in choiceButtons) btn.interactable = true;
        if (questionText != null)
        {
            questionText.text = currentRoundData.situationText;
            questionText.color = defaultQuestionColor;
        }
    }

    private IEnumerator CorrectAnswerSequence()
    {
        // 1. Add a slight delay before fading out
        yield return new WaitForSeconds(0.4f);

        // 2. Fade out the Mask and the Question Text simultaneously
        float elapsed = 0f;
        float fadeDuration = 0.3f;
        Color originalQuestionColor = defaultQuestionColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            
            if (polaroidMask != null) polaroidMask.alpha = alpha;
            if (questionText != null) questionText.color = new Color(originalQuestionColor.r, originalQuestionColor.g, originalQuestionColor.b, alpha);
            
            yield return null;
        }
        
        if (polaroidMask != null) polaroidMask.alpha = 0f;
        if (questionText != null) questionText.color = new Color(originalQuestionColor.r, originalQuestionColor.g, originalQuestionColor.b, 0f);

        // 3. Play animation of hand putting card to the back
        if (handAnimator != null)
        {
            handAnimator.PlayAnimation();
            if (uiAudioSource != null && cardFlipSFX != null) uiAudioSource.PlayOneShot(cardFlipSFX);
        }

        // 4. Wait for animation to finish
        yield return new WaitForSeconds(1.2f);

        // 5. Advance Round (updates picture and question text)
        currentRoundNumber++;
        NextRound();

        // 6. Fade Mask and Question Text back in
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            
            if (polaroidMask != null) polaroidMask.alpha = alpha;
            if (questionText != null) questionText.color = new Color(originalQuestionColor.r, originalQuestionColor.g, originalQuestionColor.b, alpha);
            
            yield return null;
        }
        
        if (polaroidMask != null) polaroidMask.alpha = 1f;
        if (questionText != null) questionText.color = new Color(originalQuestionColor.r, originalQuestionColor.g, originalQuestionColor.b, 1f);
    }

    private void UpdateHeartsUI()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
            {
                hearts[i].sprite = (i < currentHearts) ? activeHeart : inactiveHeart;
            }
        }
    }

    private IEnumerator ShowWrongFeedback(string msg)
    {
        wrongFeedbackText.text = msg;
        wrongFeedbackGroup.SetActive(true);
        
        // Ensure scale starts at 0 for pop-in if not using a separate animator script
        wrongFeedbackGroup.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            wrongFeedbackGroup.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, elapsed / 0.2f);
            yield return null;
        }
        wrongFeedbackGroup.transform.localScale = Vector3.one;

        // Auto hide after 5 seconds
        yield return new WaitForSeconds(5f);

        // Only hide if it's still active (might have been closed by another answer)
        if (wrongFeedbackGroup.activeSelf)
        {
            wrongFeedbackGroup.SetActive(false);
        }
    }

    private IEnumerator ShakeScreen()
    {
        if (shakeTarget == null) yield break;

        Vector3 originalPos = shakeTarget.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = originalPos.y + Random.Range(-shakeMagnitude, shakeMagnitude);
            shakeTarget.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = originalPos;
    }

    private void ShowWinScreen()
    {
        if (uiAudioSource != null && winSFX != null) uiAudioSource.PlayOneShot(winSFX);
        
        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);

        int stars = 0;
        int coinsEarned = 0;

        // Hearts left: 5 -> perfect, 1 -> poor
        if (currentHearts == 5) { stars = 5; coinsEarned = 50; }
        else if (currentHearts == 4) { stars = 4; coinsEarned = 40; }
        else if (currentHearts == 3) { stars = 3; coinsEarned = 30; }
        else if (currentHearts == 2) { stars = 2; coinsEarned = 20; }
        else { stars = 1; coinsEarned = 10; }

        for (int i = 0; i < winStars.Length; i++)
        {
            if (winStars[i] != null)
                winStars[i].sprite = (i < stars) ? activeStar : inactiveStar;
        }

        if (winCoinsText != null) winCoinsText.text = $"+{coinsEarned}";

        pendingRewardCoins = coinsEarned;

        PlayerPrefs.SetInt("ReactionMinigameWon", 1);
        PlayerPrefs.Save();
    }

    private void ShowLoseScreen()
    {
        if (uiAudioSource != null && loseSFX != null) uiAudioSource.PlayOneShot(loseSFX);

        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);
        
        if (loseCoinsText != null) loseCoinsText.text = "+2";

        pendingRewardCoins = 2;

        PlayerPrefs.SetInt("ReactionMinigameWon", 0);
    }

    public void RestartGame()
    {
        if (winOrLoseGroup != null && winOrLoseGroup.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        // Keep category alive — MinigameReloader reloads the same scene so it needs the pref.
        // It will be cleared once the player fully quits instead.
        MinigameReloader.ReloadActiveMinigame();
    }

    public void QuitToMenu()
    {
        if (winOrLoseGroup != null && winOrLoseGroup.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        // Clear the category now that the player is fully exiting the minigame.
        PlayerPrefs.DeleteKey("ReactionCardCategory");
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene"); PlayerPrefs.SetString("PreviousScene", prevScene); SceneLoader.ResetLoadingFlag(); SceneLoader.targetSceneForLoading = prevScene; SceneLoader.keepBackgroundPersistent = false; UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }
}
