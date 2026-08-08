using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class DistractorData
{
    public string phraseId;
    public string feedback;
}

[System.Serializable]
public class TumbangPresoResponseData
{
    public bool isRecall;
    public string englishDialogue;
    public string ilokanoDialogue;
    public string cebuanoDialogue;
    public string situationText;
    public string correctPhraseId;
    public string[] acceptablePhraseIds;
    public string englishFeedback;
    public string ilokanoFeedback;
    public string cebuanoFeedback;
    public DistractorData[] distractors;
    public string npcDescription;
    public string npcKeyword;
}

[System.Serializable]
public class TumbangPresoDataWrapper
{
    public TumbangPresoResponseData[] items;
}

[System.Serializable]
public class NPCPortraitMapping
{
    [Tooltip("A unique word from the situation or NPC description to match this image (e.g. 'basketball', 'grandmother')")]
    public string keyword;
    public Sprite npcSprite;
}

public class TumbangPresoGameManager : MonoBehaviour
{
    [Header("STT Colors")]
    public Color normalColor = Color.white;
    public Color sttWarningTextColor = Color.red;
    public Color sttProcessingColor = Color.cyan;
    public Color sttCorrectColor = Color.green;
    public Color sttWrongColor = Color.red;

    public static TumbangPresoGameManager Instance { get; private set; }
    [Header("Game Settings")]
    public int startingTsinelas = 25;
    public int totalRounds = 20;
    
    [Header("UI - Top Bar")]
    public TextMeshProUGUI tsinelasCountText;
    public TextMeshProUGUI roundText;
    public Slider roundSlider;
    public TextMeshProUGUI situationPromptText;
    
    [Header("UI - NPC & Chat")]
    public Image npcImage;
    public TextMeshProUGUI npcDialogueText;
    public UnityEngine.UI.Button translateButton;
    private bool isShowingEnglish = true;
    private bool isShowingFeedback = false;
    private bool hasGameStarted = false;
    
    [Header("NPC Data")]
    public NPCPortraitMapping[] npcPortraits;
    public Sprite defaultNpcSprite;
    
    [Header("Tin Cans")]
    [Tooltip("Drag the 3 Tin Can GameObjects here")]
    public TumbangPresoCanController[] cans;
    
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
    public AudioClip buttonClickSFX;

    [Header("How To Play UI")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;

    [Header("Feedback Popup")]
    public UnityEngine.UI.Image feedbackImage;
    public Sprite correctSprite;
    public Sprite wrongSprite;
    
    [Header("Animations")]
    [Tooltip("Drag the Chat Bubble GameObject here (It MUST have the UIPopAnimator script attached)")]
    public GameObject chatBubble;
    [Tooltip("The RectTransform of the NPC Image to slide it in")]
    public RectTransform npcRectTransform;
    private Vector2 npcOriginalPosition;
    
    [Header("Sound Effects")]
    public AudioSource uiAudioSource;
    public AudioClip correctAnswerSFX;
    public AudioClip wrongAnswerSFX;
    public AudioClip throwSFX;
    public AudioClip canHitSound1;
    public AudioClip canHitSound2;
    public AudioClip canHitSound3;
    
#if UNITY_EDITOR
    [Header("--- EDITOR DEBUG (hidden in build) ---")]
    public int currentTsinelas;
    public int currentRound = 0;
#else
    private int currentTsinelas;
    private int currentRound = 0;
#endif
    private List<TumbangPresoResponseData> allSituations = new List<TumbangPresoResponseData>();
    private TumbangPresoResponseData currentSituation;
    
    private Dictionary<string, LuminangPhrase> globalDictionary = new Dictionary<string, LuminangPhrase>();
    
        private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void Update()
    {
        // EDITOR CHEATS - press W to Win, L to Lose
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame) ShowWinScreen();
        if (kb.lKey.wasPressedThisFrame) ShowLoseScreen();
    }
#endif

    private void Start()
    {
        if (npcRectTransform != null)
        {
            npcOriginalPosition = npcRectTransform.anchoredPosition;
        }
        
        if (translateButton != null)
        {
            translateButton.onClick.RemoveAllListeners();
            translateButton.onClick.AddListener(ToggleTranslation);
        }

        if (feedbackImage != null)
        {
            feedbackImage.gameObject.SetActive(false);
        }
        
        currentTsinelas = startingTsinelas;
        UpdateTsinelasUI();
        
        LoadGlobalDictionary();
        LoadGameData();
        
        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
            if (controller != null) controller.isInputBlocked = true; // Block input while open
            
            howToPlayGroup.SetActive(true);
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        else
        {
            StartNextRound();
        }
    }
    
    private void LoadGlobalDictionary()
    {
        TextAsset dictAsset = Resources.Load<TextAsset>("LuminangPhrases");
        if (dictAsset != null)
        {
            LuminangPhraseData data = JsonUtility.FromJson<LuminangPhraseData>(dictAsset.text);
            if (data != null && data.phrases != null)
            {
                foreach (LuminangPhrase phrase in data.phrases)
                {
                    if (!globalDictionary.ContainsKey(phrase.id))
                    {
                        globalDictionary.Add(phrase.id, phrase);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Could not find LuminangPhrases.json in Resources!");
        }
    }
    
    private void LoadGameData()
    {
        string fileName = string.IsNullOrEmpty(TumbangPresoGameConfig.CategoryFilter) ? "Responses" : TumbangPresoGameConfig.CategoryFilter;
        TextAsset jsonAsset = Resources.Load<TextAsset>(fileName);
        if (jsonAsset != null)
        {
            string jsonString = "{\"items\":" + jsonAsset.text + "}";
            TumbangPresoDataWrapper wrapper = JsonUtility.FromJson<TumbangPresoDataWrapper>(jsonString);
            
            if (wrapper != null && wrapper.items != null)
            {
                allSituations = wrapper.items.ToList();
                allSituations = allSituations.OrderBy(x => Random.value).ToList();
                
                // We no longer need to swap based on isRecall since we determine STT dynamically per round now!
            }
        }
    }
    
    public void StartNextRound()
    {
        if (currentRound >= totalRounds)
        {
            ShowWinScreen();
            return;
        }
        
        currentRound++;
        
        // Update Round UI
        if (roundText != null) roundText.text = "Round " + currentRound + "/" + totalRounds;
        if (roundSlider != null) roundSlider.value = (float)currentRound / totalRounds;
        
        // Reset Cans
        if (cans != null)
        {
            foreach (var can in cans)
            {
                can.ResetCan();
            }
        }
        
        // Hide Chat Bubble initially
        if (chatBubble != null)
        {
            chatBubble.SetActive(false);
        }
        
        // Pick a situation
        if (allSituations.Count > 0)
        {
            currentSituation = allSituations[0];
            allSituations.RemoveAt(0);
            
            SetupSituationUI(currentSituation);
            DistributeAnswersToCans(currentSituation);
            
            // Trigger NPC Slide In via Coroutine
                        if (npcRectTransform != null)
            {
                StartCoroutine(SlideNPCRoutine());
            }

            // 40% chance for an STT round, but NEVER on the very first round!
            bool isSttRound = (currentRound > 1) && (Random.value <= 0.40f);

            if (isSttRound && TumbangPresoSTTManager.Instance != null)
            {
                StartCoroutine(SetupSTTRoundCoroutine());
            }
            else
            {
                UpdateSituationPromptText(currentSituation.situationText, normalColor);
                var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
                if (controller != null) controller.isInputBlocked = false;
            }
        }
    }
    
    private System.Collections.IEnumerator SlideNPCRoutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector2 startPos = new Vector2(-1000f, npcOriginalPosition.y);
        Vector2 endPos = npcOriginalPosition;
        
        npcRectTransform.anchoredPosition = startPos;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Simple ease out cubic
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            npcRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        npcRectTransform.anchoredPosition = endPos;
        
        // Trigger Chat Bubble Pop-In when NPC finishes sliding
        if (chatBubble != null)
        {
            chatBubble.SetActive(true);
            chatBubble.SendMessage("PopIn", SendMessageOptions.DontRequireReceiver);
        }
    }
    
    private void DistributeAnswersToCans(TumbangPresoResponseData data)
    {
        if (cans == null || cans.Length < 3) return;
        
        // Gather the 3 Phrase IDs
        List<string> answerIds = new List<string>();
        answerIds.Add(data.correctPhraseId);
        if (data.distractors != null && data.distractors.Length >= 2)
        {
            answerIds.Add(data.distractors[0].phraseId);
            answerIds.Add(data.distractors[1].phraseId);
        }
        
        // Shuffle the IDs so the correct answer is random
        answerIds = answerIds.OrderBy(x => Random.value).ToList();
        
        // Distribute to the 3 cans
        for (int i = 0; i < 3; i++)
        {
            TumbangPresoCanController can = cans[i];
            string phraseId = answerIds[i];
            
            // Check if this can holds the correct answer
            can.isCorrectAnswer = (phraseId == data.correctPhraseId);
            
            if (can.isCorrectAnswer)
            {
                can.feedbackText = "";
            }
            else
            {
                var distractor = data.distractors.FirstOrDefault(d => d.phraseId == phraseId);
                if (distractor != null)
                {
                    can.feedbackText = distractor.feedback;
                }
            }
            
            // Reset the text panel alpha just in case it was faded out last round
            if (can.textPanelCanvasGroup != null)
            {
                can.textPanelCanvasGroup.alpha = 1f;
            }
            
            // Look up the translation (Using Ilokano for now, as requested)
            if (globalDictionary.ContainsKey(phraseId))
            {
                LuminangPhrase phraseData = globalDictionary[phraseId];
                if (can.choiceText != null)
                {
                    // Capitalize the first letter for aesthetics
                    string text = phraseData.ilokano;
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = char.ToUpper(text[0]) + text.Substring(1);
                    }
                    can.choiceText.text = text;
                }
            }
            else
            {
                if (can.choiceText != null) can.choiceText.text = "Missing Translation";
            }
        }
    }
    
    private void SetupSituationUI(TumbangPresoResponseData data)
    {
        // Set the Prompt (Situation Text)
        if (situationPromptText != null)
        {
            situationPromptText.text = data.situationText;
        }
        
        // Reset translation state to default (true or false based on preference)
        isShowingEnglish = false; 
        isShowingFeedback = false;
        UpdateDialogueText();
        
        // Find the right NPC Portrait
        if (npcImage != null)
        {
            Sprite foundSprite = defaultNpcSprite;
            
            string targetKeyword = data.npcKeyword != null ? data.npcKeyword.ToLower() : "";
            
            foreach (NPCPortraitMapping mapping in npcPortraits)
            {
                if (string.IsNullOrEmpty(mapping.keyword) || string.IsNullOrEmpty(targetKeyword)) continue;
                
                if (mapping.keyword.ToLower() == targetKeyword)
                {
                    foundSprite = mapping.npcSprite;
                    break;
                }
            }
            
            npcImage.sprite = foundSprite;
        }
    }

    private void ShowWinScreen()
    {
        var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
        if (controller != null) controller.isInputBlocked = true;

        if (uiAudioSource != null && winSFX != null) uiAudioSource.PlayOneShot(winSFX);
        
        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            winPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        if (losePanel != null) losePanel.SetActive(false);

        int stars = 0;
        int coinsEarned = 0;

        // Perfect run: 20 throws used out of 25 means 5 tsinelas left.
        if (currentTsinelas >= 5) { stars = 5; coinsEarned = 50; }
        else if (currentTsinelas == 4) { stars = 4; coinsEarned = 40; }
        else if (currentTsinelas == 3) { stars = 3; coinsEarned = 30; }
        else if (currentTsinelas == 2) { stars = 2; coinsEarned = 20; }
        else { stars = 1; coinsEarned = 10; } // 1 or 0 tsinelas left means they barely made it!

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
        PlayerPrefs.SetInt("TumbangPresoMinigameWon", 1);
        PlayerPrefs.Save();
    }

    private void ShowLoseScreen()
    {
        var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
        if (controller != null) controller.isInputBlocked = true;

        if (uiAudioSource != null && loseSFX != null) uiAudioSource.PlayOneShot(loseSFX);

        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) 
        {
            losePanel.SetActive(true);
            losePanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        
        if (loseCoinsText != null) loseCoinsText.text = "+2";

        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentCoins + 2); // Consolation prize
        PlayerPrefs.SetInt("TumbangPresoMinigameWon", 0);
        PlayerPrefs.Save();
    }

    public void OpenHowToPlay()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);

        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
            if (controller != null) controller.isInputBlocked = true; // Block input while open

            howToPlayGroup.SetActive(true);
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
    }

    public void CloseHowToPlay()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);
        
        if (howToPlayPanel != null) howToPlayPanel.transform.localScale = Vector3.zero; // Instant snap
        if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
        
        if (!hasGameStarted)
        {
            hasGameStarted = true;
            StartNextRound();
        }
        else
        {
            // Resume gameplay if mid-game
            var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
            if (controller != null) controller.isInputBlocked = false;
        }
    }

    public void RestartGame()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene");
        SceneManager.LoadScene(prevScene);
    }

    public void ToggleTranslation()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);
        isShowingEnglish = !isShowingEnglish;
        UpdateDialogueText();
    }

    private void UpdateDialogueText()
    {
        if (npcDialogueText != null && currentSituation != null)
        {
            if (isShowingEnglish)
            {
                npcDialogueText.text = isShowingFeedback ? currentSituation.englishFeedback : currentSituation.englishDialogue;
            }
            else
            {
                if (TumbangPresoGameConfig.TargetLanguage.ToLower() == "cebuano")
                    npcDialogueText.text = isShowingFeedback ? currentSituation.cebuanoFeedback : currentSituation.cebuanoDialogue;
                else
                    npcDialogueText.text = isShowingFeedback ? currentSituation.ilokanoFeedback : currentSituation.ilokanoDialogue;
            }
        }
    }
    
    public void DeductTsinelas()
    {
        if (currentTsinelas > 0)
        {
            currentTsinelas--;
            UpdateTsinelasUI();
        }
    }
    
    public void OnCanHit(TumbangPresoCanController hitCan)
    {
        // Play random hit SFX
        if (uiAudioSource != null)
        {
            AudioClip[] hits = { canHitSound1, canHitSound2, canHitSound3 };
            var validHits = hits.Where(c => c != null).ToArray();
            if (validHits.Length > 0)
            {
                uiAudioSource.PlayOneShot(validHits[Random.Range(0, validHits.Length)]);
            }
        }
        
        if (hitCan.isCorrectAnswer)
        {
            DeductTsinelas();
            Debug.Log("Correct Answer Hit!");
            if (uiAudioSource != null && correctAnswerSFX != null)
            {
                uiAudioSource.PlayOneShot(correctAnswerSFX);
            }
            ShowFeedbackPopup(true);
            
            // Show Feedback Dialogue
            isShowingFeedback = true;
            UpdateDialogueText();
            
            // Advance to next round after delay
            Invoke(nameof(StartNextRound), 2f);
        }
        else
        {
            DeductTsinelas();
            Debug.Log("Wrong Answer Hit!");
            if (uiAudioSource != null && wrongAnswerSFX != null)
            {
                uiAudioSource.PlayOneShot(wrongAnswerSFX);
            }
            ShowFeedbackPopup(false);
            
            if (!string.IsNullOrEmpty(hitCan.feedbackText))
            {
                string hexColor = ColorUtility.ToHtmlStringRGB(sttWarningTextColor);
                UpdateSituationPromptText($"<color=#{hexColor}>{hitCan.feedbackText}</color>", normalColor);
            }
            
            if (currentTsinelas <= 0)
            {
                ShowLoseScreen();
            }
            else
            {
                // Wait a moment then let them try again (restore the can)
                Invoke(nameof(RestoreWrongCan), 2f);
            }
        }
    }
    
    private void RestoreWrongCan()
    {
        UpdateSituationPromptText(currentSituation.situationText, normalColor);
        if (cans != null)
        {
            foreach (var can in cans)
            {
                // Only restore the ones that were knocked down (wrong answers)
                if (!can.isCorrectAnswer && can.hasFallen)
                {
                    can.ResetCan();
                }
            }
        }
    }
    
    public void ShowFeedbackPopup(bool isCorrect)
    {
        if (feedbackImage == null) return;

        feedbackImage.sprite = isCorrect ? correctSprite : wrongSprite;
        
        StopAllCoroutines();
        StartCoroutine(AnimateFeedbackPopupRoutine());
    }
    
    private System.Collections.IEnumerator AnimateFeedbackPopupRoutine()
    {
        feedbackImage.gameObject.SetActive(true);
        Transform t = feedbackImage.transform;
        
        // Quick pop in
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.2f, elapsed / 0.2f);
            yield return null;
        }

        // Settle
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, elapsed / 0.1f);
            yield return null;
        }

        t.localScale = Vector3.one;

        // Wait
        yield return new WaitForSeconds(1.5f);

        // Shrink out
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, elapsed / 0.2f);
            yield return null;
        }

        feedbackImage.gameObject.SetActive(false);
    }
    
    private void UpdateTsinelasUI()
    {
        if (tsinelasCountText != null)
        {
            tsinelasCountText.text = currentTsinelas.ToString();
        }
    }
    
    
    private System.Collections.IEnumerator SetupSTTRoundCoroutine()
    {
        var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
        if (controller != null) controller.isInputBlocked = true;

        UpdateSituationPromptText(currentSituation.situationText, normalColor);

        yield return new WaitForSeconds(0.5f);

        if (uiAudioSource != null)
        {
            if (canHitSound1 != null) uiAudioSource.PlayOneShot(canHitSound1);
            if (canHitSound2 != null) uiAudioSource.PlayOneShot(canHitSound2);
            if (canHitSound3 != null) uiAudioSource.PlayOneShot(canHitSound3);
        }

        if (cans != null)
        {
            foreach (var can in cans)
            {
                if (can.textPanelCanvasGroup != null) can.textPanelCanvasGroup.alpha = 0f;
                can.FallDown();
            }
        }

        TumbangPresoSTTManager.Instance.StartSTT(currentSituation);
    }

    public TumbangPresoResponseData GetCurrentSituationData() { return currentSituation; }

    public void UpdateSituationPromptText(string text, Color color)
    {
        if (situationPromptText != null)
        {
            situationPromptText.text = text;
            situationPromptText.color = color;
        }
    }

    public void CompleteSTTAndAdvanceRound()
    {
        DeductTsinelas();
        
        // Show Feedback Dialogue
        isShowingFeedback = true;
        UpdateDialogueText();
        
        Invoke(nameof(StartNextRound), 2f);
    }

    public void CompleteSTTAndFailRound()
    {
        DeductTsinelas();
        
        if (currentTsinelas <= 0)
        {
            ShowLoseScreen();
            return;
        }

        UpdateSituationPromptText(currentSituation.situationText, normalColor);
        
        var controller = FindFirstObjectByType<TumbangPresoSwipeController>();
        if (controller != null) controller.isInputBlocked = false;

        if (cans != null)
        {
            foreach (var can in cans)
            {
                can.ResetCan();
            }
        }
    }

    public void PlayThrowSFX()
    {
        if (uiAudioSource != null && throwSFX != null)
        {
            uiAudioSource.PlayOneShot(throwSFX);
        }
    }
}
