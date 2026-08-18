using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// JSON Data Structures for Memory Game
[System.Serializable]
public class MemoryGamePhraseData
{
    public string category;
    public string phraseId;
    public string englishTerm;
    public string ilokanoTerm;
    public string cebuanoTerm;
    public string englishFeedback;
    public string ilokanoFeedback;
    public string cebuanoFeedback;
    public string recallQuestion; // Added for custom recall questions
}

// Wrapper for Unity's JsonUtility to parse an array of objects
public static class MemoryGameJsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}

public class MemoryGameManager : MonoBehaviour
{
    public static MemoryGameManager Instance { get; private set; }

    public enum GameState
    {
        FlippingCards,
        DragDropVerification,
        STTVerification,
        RecallQuestion,
        GameOver
    }

    [Header("Game State")]
    public GameState currentState = GameState.FlippingCards;

    private int pendingRewardCoins = 0;

    [Header("UI References (Main)")]
    public TextMeshProUGUI pairsFoundText;
    public Image[] heartImages;
    public Sprite heartFullSprite;
    public Sprite heartEmptySprite;

    [Header("UI References (Drag & Drop Phase)")]
    public GameObject verificationGroup; // The new background/dimmer
    public CanvasGroup verificationGroupCanvasGroup; // Used for fading the background dim
    public MemoryCard verificationRevealCard; // The big card that spins in
    public Transform verificationGlowImage; // The glow effect that rotates on success
    public GameObject dragDropPanel;     // The panel holding the drop slot
    public Transform dragOptionsParent; // Where the 8 DraggableWords are
    public GameObject draggableWordPrefab;

    [Header("UI References (Recall Phase)")]
    public GameObject recallGroup; // The parent background dimmer
    public CanvasGroup recallGroupCanvasGroup; // The CanvasGroup on the parent for fading
    public GameObject recallPanel; // The actual question box
    public TextMeshProUGUI recallQuestionText;
    public Button[] recallOptionButtons;
    public RectTransform guideNpcRect;
    public UnityEngine.UI.Image guideNpcImage;
    public Sprite guideNpcIlokano;
    public Sprite guideNpcCebuano;
    public AudioClip npcSlideSfx;
    
    [Header("UI References (STT Phase Hooks)")]
    public RectTransform wordBankPanel;
    public RectTransform choicesGroupRect;
    
    [Header("End Game UI")]
    public GameObject winOrLoseGroup;
    public GameObject winPanel;
    public GameObject losePanel;
    public Image[] winStars;
    public Sprite activeStar;
    public Sprite inactiveStar;
    public TextMeshProUGUI winCoinsText;
    public TextMeshProUGUI loseCoinsText;
    public AudioSource uiAudioSource;
    public AudioClip winSFX;
    public AudioClip loseSFX;
    public AudioClip buttonClickSFX;
    
    [Header("How To Play UI")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;

    [Header("Game Settings")]
    public bool isIlokano = true; // Temporary language toggle

    [Header("Cards Setup")]
    public Transform gridCardsParent;
    
    [Header("Data Source")]
    [Tooltip("The main JSON file for this level (e.g., 'ActionVerbs'). The first 8 items are the pairs, any items after that are recall questions.")]
    public string jsonFileName = "ActionVerbs";

    [System.Serializable]
    public struct VerbSpriteMapping
    {
        [Tooltip("Type the exact English word from the JSON (e.g., 'eat', 'drink')")]
        public string englishWord; 
        public Sprite image;
    }
    [Header("Assign your 8 Pictures here!")]
    public List<VerbSpriteMapping> verbPictures; 

    [Header("Audio (SFX)")]
    [Tooltip("Leave these blank if you don't have sounds yet")]
    public AudioClip matchCorrectClip;
    public AudioClip matchWrongClip;

    // Internal class to hold the final compiled card data
    private class CardData
    {
        public MemoryGamePhraseData phrase;
        public Sprite image;
    }

    private List<CardData> deck = new List<CardData>();
    private List<MemoryCard> allCards = new List<MemoryCard>();
    [Header("Editor Debug")]
    [Tooltip("Change this in inspector to set starting hearts (1-5)")]
    [Range(1, 5)]
    public int startingHearts = 5;

    private MemoryCard firstSelectedCard = null;
    private MemoryCard secondSelectedCard = null;
    
    private int currentHearts = 5;
    private int pairsFound = 0;
    private int totalPairs = 8;
    private bool isInputLocked = false;
    
    // Recall Sequence Map
    private List<int> recallTriggerRounds = new List<int> { 2, 3, 5, 6, 7 };
    
    // Recall Question Queue
    private Queue<MemoryGamePhraseData> recallQueue = new Queue<MemoryGamePhraseData>();
    private MemoryGamePhraseData currentRecallQuestion;
    private Vector2 originalNpcPos;
    private Vector2 originalWordBankPos;
    private Vector2 originalChoicesPos;
    private bool hasGameStarted = false;

    // STT State Variables
    private bool isRecallSTT = false;
    private MemoryDraggableWord currentDragWordRef; // Reference to put it back if STT fails

    private void Awake()
    {
        // Scene-local singleton: just overwrite instance instead of destroying game object
        // so seamless additive loading (Try Again) doesn't kill the new game object.
        Instance = this;

        // Dynamically override isIlokano if a target language was passed via PlayerPrefs from Magellan
        string passedLang = PlayerPrefs.GetString("MemoryGameLanguage", "");
        if (!string.IsNullOrEmpty(passedLang))
        {
            isIlokano = passedLang.ToLower().Contains("ilokano");
            Debug.Log($"[MemoryGameManager] Language overridden by PlayerPrefs to: {(isIlokano ? "Ilokano" : "Cebuano")}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && hasGameStarted)
        {
            currentHearts = Mathf.Clamp(startingHearts, 1, 5);
            UpdateHeartsUI();
        }
    }
#endif

    private void Start()
    {
        if (guideNpcRect != null) originalNpcPos = guideNpcRect.anchoredPosition;
        if (wordBankPanel != null) originalWordBankPos = wordBankPanel.anchoredPosition;
        if (choicesGroupRect != null) originalChoicesPos = choicesGroupRect.anchoredPosition;

        if (verificationGroup != null) verificationGroup.SetActive(false);
        if (dragDropPanel != null) dragDropPanel.SetActive(false);
        if (recallGroup != null) recallGroup.SetActive(false);
        if (recallPanel != null) recallPanel.SetActive(false);
        
        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            howToPlayGroup.SetActive(true);
            howToPlayGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        else
        {
            StartGame();
        }
    }

    public void CloseHowToPlay()
    {
        if (AudioManager.instance != null && buttonClickSFX != null) AudioManager.instance.PlaySFX(buttonClickSFX);

        if (howToPlayPanel != null) howToPlayPanel.transform.localScale = Vector3.zero; // Instant snap

        if (howToPlayGroup != null) 
        {
            howToPlayGroup.SetActive(false);
        }

        if (!hasGameStarted)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        hasGameStarted = true;
        currentHearts = startingHearts;
        InitializeCards();
        UpdatePairsUI();
        UpdateHeartsUI();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.wKey.wasPressedThisFrame) ShowWinScreen();
            if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame) ShowLoseScreen();
        }
#endif
    }

    private void InitializeCards()
    {
        // 1. Get all cards
        MemoryCard[] cards = gridCardsParent.GetComponentsInChildren<MemoryCard>();
        allCards.AddRange(cards);

        if (allCards.Count != 16)
        {
            Debug.LogWarning("Make sure exactly 16 cards are in the GridCards container!");
            return;
        }

        // 2. Load the JSON dynamically from Resources
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
        if (jsonAsset == null)
        {
            Debug.LogError($"Could not find JSON file named '{jsonFileName}' in any Resources folder!");
            return;
        }

        MemoryGamePhraseData[] actionVerbs = MemoryGameJsonHelper.FromJson<MemoryGamePhraseData>(jsonAsset.text);
        
        if (actionVerbs.Length != 8)
        {
            Debug.LogWarning($"Expected 8 action verbs, found {actionVerbs.Length}");
        }

        // 3. Prepare the Recall Queue (Any words past the first 8)
        PrepareRecallQueue(actionVerbs);

        // 4. Create the deck of 16 cards (8 pairs) using ONLY the first 8
        deck.Clear();
        
        int verbCount = Mathf.Min(8, actionVerbs.Length);
        for (int i = 0; i < verbCount; i++)
        {
            var verb = actionVerbs[i];
            // Find the matching picture based on the english word
            Sprite matchedSprite = null;
            var mapping = verbPictures.FirstOrDefault(v => v.englishWord.ToLower() == verb.englishTerm.ToLower());
            if (mapping.image != null)
            {
                matchedSprite = mapping.image;
            }
            else
            {
                Debug.LogWarning($"Missing picture for verb: {verb.englishTerm}. Add it to Verb Pictures list!");
            }

            CardData card = new CardData { phrase = verb, image = matchedSprite };
            deck.Add(card); // Pair 1
            deck.Add(card); // Pair 2
        }

        // 5. Shuffle the deck
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = UnityEngine.Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        // 6. Assign to UI
        for (int i = 0; i < allCards.Count; i++)
        {
            if (i < deck.Count)
            {
                // We use the JSON 'phraseId' as the pairID to check for matches
                allCards[i].Setup(deck[i].phrase.phraseId, deck[i].image, OnCardSelected);
            }
        }

        SpawnAllDraggableWords(actionVerbs);
    }

    private void PrepareRecallQueue(MemoryGamePhraseData[] allPhrases)
    {
        recallQueue.Clear();

        // If there are more than 8 items in the JSON, the rest are recall questions!
        if (allPhrases.Length > 8)
        {
            List<MemoryGamePhraseData> recallList = new List<MemoryGamePhraseData>();
            for (int i = 8; i < allPhrases.Length; i++)
            {
                recallList.Add(allPhrases[i]);
            }

            // Shuffle them
            for (int i = 0; i < recallList.Count; i++)
            {
                var temp = recallList[i];
                int randomIndex = Random.Range(i, recallList.Count);
                recallList[i] = recallList[randomIndex];
                recallList[randomIndex] = temp;
            }

            // Enqueue exactly 5 random recall questions
            int questionsToTake = Mathf.Min(5, recallList.Count);
            for (int i = 0; i < questionsToTake; i++)
            {
                recallQueue.Enqueue(recallList[i]);
            }
        }
    }

    private void SpawnAllDraggableWords(MemoryGamePhraseData[] verbs)
    {
        // Clear existing just in case
        foreach (Transform child in dragOptionsParent)
        {
            Destroy(child.gameObject);
        }

        // Shuffle the verbs before spawning so the word bank is random
        List<MemoryGamePhraseData> shuffledVerbs = new List<MemoryGamePhraseData>();
        
        int verbCount = Mathf.Min(8, verbs.Length);
        for (int i = 0; i < verbCount; i++)
        {
            shuffledVerbs.Add(verbs[i]);
        }
        
        for (int i = 0; i < shuffledVerbs.Count; i++)
        {
            MemoryGamePhraseData temp = shuffledVerbs[i];
            int randomIndex = Random.Range(i, shuffledVerbs.Count);
            shuffledVerbs[i] = shuffledVerbs[randomIndex];
            shuffledVerbs[randomIndex] = temp;
        }

        for (int i = 0; i < shuffledVerbs.Count; i++)
        {
            MemoryGamePhraseData verb = shuffledVerbs[i];
            GameObject wordObj = Instantiate(draggableWordPrefab, dragOptionsParent);
            MemoryDraggableWord dragScript = wordObj.GetComponent<MemoryDraggableWord>();
            string termToDisplay = isIlokano ? verb.ilokanoTerm : verb.cebuanoTerm;
            dragScript.Setup(verb.phraseId, termToDisplay);
        }
    }

    private void OnCardSelected(MemoryCard selectedCard)
    {
        if (currentState != GameState.FlippingCards) return;
        if (isInputLocked) return;

        selectedCard.FlipFaceUp();

        if (firstSelectedCard == null)
        {
            firstSelectedCard = selectedCard;
        }
        else if (secondSelectedCard == null && selectedCard != firstSelectedCard)
        {
            secondSelectedCard = selectedCard;
            StartCoroutine(CheckMatchCoroutine());
        }
    }

    private IEnumerator CheckMatchCoroutine()
    {
        isInputLocked = true; 

        bool isMatch = firstSelectedCard.pairID == secondSelectedCard.pairID;

        // Immediately show Red or Green feedback
        firstSelectedCard.SetFeedback(isMatch);
        secondSelectedCard.SetFeedback(isMatch);

        // Wait a second so they can see the result
        yield return new WaitForSeconds(1.0f);

        if (isMatch)
        {
            if (AudioManager.instance != null && matchCorrectClip != null) 
                AudioManager.instance.PlaySFX(matchCorrectClip);

            firstSelectedCard.SetMatched();
            secondSelectedCard.SetMatched();
            
            pairsFound++;
            UpdatePairsUI();
            
            // Wait slightly before showing the popup so they see the green cards
            yield return new WaitForSeconds(0.5f);
            
            StartDragDropVerification();
        }
        else
        {
            if (AudioManager.instance != null && matchWrongClip != null) 
                AudioManager.instance.PlaySFX(matchWrongClip);

            // Clear the red color before flipping them back down
            firstSelectedCard.ClearFeedback();
            secondSelectedCard.ClearFeedback();

            firstSelectedCard.FlipFaceDown();
            secondSelectedCard.FlipFaceDown();
            firstSelectedCard = null;
            secondSelectedCard = null;
            isInputLocked = false;
        }
    }

    private void StartDragDropVerification()
    {
        currentState = GameState.DragDropVerification;
        
        if (verificationGroup != null) 
        {
            verificationGroup.SetActive(true);
            if (verificationGroupCanvasGroup != null)
                verificationGroupCanvasGroup.alpha = 0f;
        }
        if (verificationGlowImage != null) verificationGlowImage.gameObject.SetActive(false);
        if (dragDropPanel != null) dragDropPanel.SetActive(false); // Hide slot during animation

        CardData matchedData = deck.Find(c => c.phrase.phraseId == firstSelectedCard.pairID);
        
        // Setup the reveal card using the actual MemoryCard script
        if (verificationRevealCard != null && matchedData != null)
        {
            // Store the scale before Setup resets it to 1,1,1!
            Vector3 originalScale = verificationRevealCard.transform.localScale;

            // Set it up as if it's a new card
            verificationRevealCard.Setup(matchedData.phrase.phraseId, matchedData.image, null);
            // Instantly show the picture without playing the card flip animation (which conflicts with the scale in animation)
            verificationRevealCard.ForceFaceUp(); 
            
            StartCoroutine(AnimateRevealCard(matchedData, originalScale));
        }

        firstSelectedCard = null;
        secondSelectedCard = null;
        isInputLocked = false;
    }

    private IEnumerator AnimateRevealCard(CardData matchedData, Vector3 targetScale)
    {
        RectTransform cardRect = verificationRevealCard.GetComponent<RectTransform>();
        
        // Start state: Small and rotated
        cardRect.localScale = Vector3.zero;
        cardRect.localRotation = Quaternion.Euler(0, 0, 180);

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease out elastic effect (or just smooth lerp)
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (verificationGroupCanvasGroup != null)
                verificationGroupCanvasGroup.alpha = smoothT;

            cardRect.localScale = Vector3.Lerp(Vector3.zero, targetScale, smoothT);
            cardRect.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 180), Quaternion.identity, smoothT);
            
            yield return null;
        }

        // Snap to final values
        if (verificationGroupCanvasGroup != null)
            verificationGroupCanvasGroup.alpha = 1f;
        cardRect.localScale = targetScale;
        cardRect.localRotation = Quaternion.identity;

        // Now show the Drop Slot
        if (dragDropPanel != null) dragDropPanel.SetActive(true);

        MemoryDropSlot dropSlot = dragDropPanel.GetComponentInChildren<MemoryDropSlot>();
        if (dropSlot != null)
        {
            dropSlot.ShowVisuals(); // Make sure the slot is visible!
            dropSlot.SetTarget(matchedData.phrase.phraseId);
        }
    }

    public void OnDragDropFail(MemoryDraggableWord wrongWord)
    {
        StartCoroutine(HandleDragDropFail(wrongWord));
    }

    private IEnumerator HandleDragDropFail(MemoryDraggableWord wrongWord)
    {
        Debug.Log("Wrong translation dropped!");
        if (AudioManager.instance != null && matchWrongClip != null) 
            AudioManager.instance.PlaySFX(matchWrongClip);
            
        LoseHeart();
        if (verificationRevealCard != null) verificationRevealCard.SetFeedback(false); // Glow red!
        
        // Wait 1 second before kicking the word back
        yield return new WaitForSeconds(1f);

        if (verificationRevealCard != null) verificationRevealCard.ClearFeedback(); // Remove red glow

        wrongWord.ResetPosition();

        MemoryDropSlot dropSlot = dragDropPanel.GetComponentInChildren<MemoryDropSlot>();
        if (dropSlot != null) dropSlot.ShowVisuals(); // Show the empty slot again
    }

    public void OnDragDropSuccess(MemoryDraggableWord correctWord)
    {
        StartCoroutine(HandleDragDropSuccess(correctWord));
    }

    private IEnumerator HandleDragDropSuccess(MemoryDraggableWord correctWord)
    {
        Debug.Log("Correct translation dropped!");
        if (AudioManager.instance != null && matchCorrectClip != null) 
            AudioManager.instance.PlaySFX(matchCorrectClip);

        // Turn on glow and rotate it for 2 seconds
        if (verificationGlowImage != null)
        {
            verificationGlowImage.gameObject.SetActive(true);
            float elapsed = 0f;
            float duration = 2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                verificationGlowImage.Rotate(0, 0, -100f * Time.deltaTime); // Spin slowly clockwise
                yield return null;
            }
            verificationGlowImage.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(2f); // Fallback if no glow image assigned
        }

        // We do NOT destroy the word here yet, because if they fail STT, it must go back!
        currentDragWordRef = correctWord;
        
        // Trigger STT Phase instead of checking next round immediately
        TriggerSTTPhase(isRecall: false);
    }

    public void TriggerRecallQuestion()
    {
        if (recallQueue.Count == 0) return; // No more questions
        
        currentState = GameState.RecallQuestion;
        if (recallGroup != null) recallGroup.SetActive(true);
        if (recallPanel != null) recallPanel.SetActive(true);
        
        if (recallGroupCanvasGroup != null)
        {
            recallGroupCanvasGroup.alpha = 0f;
            StartCoroutine(FadeRecallPanel(1f, 0.3f)); // Fade in over 0.3 seconds
        }

        if (guideNpcImage != null)
        {
            guideNpcImage.sprite = isIlokano ? guideNpcIlokano : guideNpcCebuano;
        }

        if (guideNpcRect != null)
        {
            StartCoroutine(SlideInNPC(0.5f));
        }

        currentRecallQuestion = recallQueue.Dequeue();

        if (recallQuestionText != null)
        {
            if (!string.IsNullOrEmpty(currentRecallQuestion.recallQuestion))
                recallQuestionText.text = currentRecallQuestion.recallQuestion;
            else
                recallQuestionText.text = $"Translate: {currentRecallQuestion.englishTerm.ToUpper()}";
        }

        // Prepare options: 1 correct, 3 random wrong from the rest of the array
        List<MemoryGamePhraseData> options = new List<MemoryGamePhraseData> { currentRecallQuestion };

        // Load the full array again just for options
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
        MemoryGamePhraseData[] allPhrases = MemoryGameJsonHelper.FromJson<MemoryGamePhraseData>(jsonAsset.text);
        List<MemoryGamePhraseData> wrongOptionsPool = new List<MemoryGamePhraseData>(allPhrases);

        wrongOptionsPool.RemoveAll(p => p.phraseId == currentRecallQuestion.phraseId);

        // Filter by the same subcategory using the phraseId prefix (e.g. "count_038" -> "count")
        string questionPrefix = currentRecallQuestion.phraseId.Split('_')[0];
        List<MemoryGamePhraseData> sameCategoryOptions = wrongOptionsPool.FindAll(p => p.phraseId.StartsWith(questionPrefix + "_"));
        
        // Only use the filtered list if there are at least 3 options available in that category
        if (sameCategoryOptions.Count >= 3)
        {
            wrongOptionsPool = sameCategoryOptions;
        }

        // Shuffle and pick 3 wrong
        for (int i = 0; i < wrongOptionsPool.Count; i++)
        {
            var temp = wrongOptionsPool[i];
            int randomIndex = Random.Range(i, wrongOptionsPool.Count);
            wrongOptionsPool[i] = wrongOptionsPool[randomIndex];
            wrongOptionsPool[randomIndex] = temp;
        }

        // Pick enough wrong options to fill the remaining buttons
        int distractorsNeeded = recallOptionButtons.Length - 1;
        for (int i = 0; i < distractorsNeeded && i < wrongOptionsPool.Count; i++)
        {
            options.Add(wrongOptionsPool[i]);
        }

        // Shuffle the 4 options
        for (int i = 0; i < options.Count; i++)
        {
            var temp = options[i];
            int randomIndex = Random.Range(i, options.Count);
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }

        // Assign to buttons
        for (int i = 0; i < recallOptionButtons.Length; i++)
        {
            if (i < options.Count)
            {
                recallOptionButtons[i].gameObject.SetActive(true);
                TextMeshProUGUI btnText = recallOptionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = options[i].ilokanoTerm; // Assuming Ilokano

                // Remove old listeners and add new one
                recallOptionButtons[i].onClick.RemoveAllListeners();
                bool isCorrect = (options[i].phraseId == currentRecallQuestion.phraseId);
                recallOptionButtons[i].onClick.AddListener(() => OnRecallOptionSelected(isCorrect));
            }
            else
            {
                recallOptionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnRecallOptionSelected(bool isCorrect)
    {
        if (isCorrect)
        {
            if (AudioManager.instance != null && matchCorrectClip != null) 
                AudioManager.instance.PlaySFX(matchCorrectClip);
                
            TriggerSTTPhase(isRecall: true);
        }
        else
        {
            if (AudioManager.instance != null && matchWrongClip != null) 
                AudioManager.instance.PlaySFX(matchWrongClip);
            
            LoseHeart();
            // Optional: visual feedback for wrong choice
        }
    }

    private void TriggerSTTPhase(bool isRecall)
    {
        currentState = GameState.STTVerification;
        isRecallSTT = isRecall;
        
        if (isRecall)
        {
            if (choicesGroupRect != null) StartCoroutine(SlidePanelY(choicesGroupRect, originalChoicesPos.y - 1000f, 0.5f));
        }
        else
        {
            if (wordBankPanel != null) StartCoroutine(SlidePanelY(wordBankPanel, originalWordBankPos.y - 1000f, 0.5f));
        }

        string answerWord = "";
        if (isRecall)
            answerWord = isIlokano ? currentRecallQuestion.ilokanoTerm : currentRecallQuestion.cebuanoTerm;
        else if (currentDragWordRef != null)
            answerWord = currentDragWordRef.wordText.text;

        if (MemoryGameSTTManager.Instance != null)
            MemoryGameSTTManager.Instance.StartSTT(answerWord);
        else
            Debug.LogError("MemoryGameSTTManager Instance is null! Please add the STTManager to the scene.");
    }

    public void OnSTTSuccess()
    {
        if (isRecallSTT)
        {
            // Reset position for the next time it's used
            if (choicesGroupRect != null) choicesGroupRect.anchoredPosition = originalChoicesPos;
            
            if (recallGroup != null) recallGroup.SetActive(false);
            if (recallPanel != null) recallPanel.SetActive(false);
            currentState = GameState.FlippingCards;
        }
        else
        {
            // Reset position for the next time it's used
            if (wordBankPanel != null) wordBankPanel.anchoredPosition = originalWordBankPos;
            
            if (currentDragWordRef != null) Destroy(currentDragWordRef.gameObject);
            if (verificationGroup != null) verificationGroup.SetActive(false);
            if (dragDropPanel != null) dragDropPanel.SetActive(false);
            
            if (recallTriggerRounds.Contains(pairsFound))
            {
                TriggerRecallQuestion();
            }
            else if (pairsFound >= totalPairs)
            {
                ShowWinScreen();
            }
            else
            {
                currentState = GameState.FlippingCards;
            }
        }
    }

    public void OnSTTFailure()
    {
        if (isRecallSTT)
        {
            if (choicesGroupRect != null) StartCoroutine(SlidePanelY(choicesGroupRect, originalChoicesPos.y, 0.5f));
            
            if (recallQueue.Count == 0)
            {
                TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFileName);
                MemoryGamePhraseData[] allPhrases = MemoryGameJsonHelper.FromJson<MemoryGamePhraseData>(jsonAsset.text);
                PrepareRecallQueue(allPhrases);
            }
            TriggerRecallQuestion();
        }
        else
        {
            if (wordBankPanel != null) StartCoroutine(SlidePanelY(wordBankPanel, originalWordBankPos.y, 0.5f));
            if (verificationGlowImage != null) verificationGlowImage.gameObject.SetActive(false);
            if (currentDragWordRef != null) currentDragWordRef.ResetPosition();
            
            MemoryDropSlot dropSlot = dragDropPanel.GetComponentInChildren<MemoryDropSlot>();
            if (dropSlot != null) dropSlot.ShowVisuals();
            
            currentState = GameState.DragDropVerification;
        }
    }

    private IEnumerator FadeRecallPanel(float targetAlpha, float duration, System.Action onComplete = null)
    {
        float startAlpha = recallGroupCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            recallGroupCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        recallGroupCanvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    private IEnumerator SlideInNPC(float duration)
    {
        if (AudioManager.instance != null && npcSlideSfx != null)
            AudioManager.instance.PlaySFX(npcSlideSfx);
            
        // Slide in from 1000 pixels to the right
        Vector2 startPos = originalNpcPos + new Vector2(1000f, 0f); 
        guideNpcRect.anchoredPosition = startPos;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth ease out
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 
            
            guideNpcRect.anchoredPosition = Vector2.Lerp(startPos, originalNpcPos, smoothT);
            yield return null;
        }
        
        guideNpcRect.anchoredPosition = originalNpcPos;
    }

    private void UpdatePairsUI()
    {
        if (pairsFoundText != null)
        {
            pairsFoundText.text = $"{pairsFound:D2}/08";
        }
    }

    public void LoseHeart()
    {
        if (currentHearts > 0)
        {
            currentHearts--;
            UpdateHeartsUI();
            if (currentHearts <= 0) ShowLoseScreen();
        }
    }

    private void UpdateHeartsUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].sprite = (i < currentHearts) ? heartFullSprite : heartEmptySprite;
        }
    }

    private void ShowWinScreen()
    {
        if (AudioManager.instance != null && winSFX != null) AudioManager.instance.PlaySFX(winSFX);
        
        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            winPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        if (losePanel != null) losePanel.SetActive(false);

        int stars = 0;
        int coinsEarned = 0;

        if (currentHearts >= 5) { stars = 5; coinsEarned = 50; }
        else if (currentHearts == 4) { stars = 4; coinsEarned = 40; }
        else if (currentHearts == 3) { stars = 3; coinsEarned = 30; }
        else if (currentHearts == 2) { stars = 2; coinsEarned = 20; }
        else { stars = 1; coinsEarned = 10; }

        if (winStars != null)
        {
            for (int i = 0; i < winStars.Length; i++)
            {
                if (winStars[i] != null)
                    winStars[i].sprite = (i < stars) ? activeStar : inactiveStar;
            }
        }

        if (winCoinsText != null) winCoinsText.text = $"+{coinsEarned}";

        pendingRewardCoins = coinsEarned;

        PlayerPrefs.SetInt("MemoryGameMinigameWon", 1);
        PlayerPrefs.Save();
    }

    private void ShowLoseScreen()
    {
        if (AudioManager.instance != null && loseSFX != null) AudioManager.instance.PlaySFX(loseSFX);

        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) 
        {
            losePanel.SetActive(true);
            losePanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        
        if (loseCoinsText != null) loseCoinsText.text = "+2";

        pendingRewardCoins = 2;

        PlayerPrefs.SetInt("MemoryGameMinigameWon", 0);
        PlayerPrefs.Save();
    }

    public void OnContinueClicked()
    {
        if (winOrLoseGroup != null && winOrLoseGroup.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        if (AudioManager.instance != null && buttonClickSFX != null) AudioManager.instance.PlaySFX(buttonClickSFX);
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene"); PlayerPrefs.SetString("PreviousScene", prevScene); SceneLoader.ResetLoadingFlag(); SceneLoader.targetSceneForLoading = prevScene; SceneLoader.keepBackgroundPersistent = false; UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    public void OnTryAgainClicked()
    {
        if (winOrLoseGroup != null && winOrLoseGroup.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        if (AudioManager.instance != null && buttonClickSFX != null) AudioManager.instance.PlaySFX(buttonClickSFX);
        MinigameReloader.ReloadActiveMinigame();
    }

    public void OnQuitClicked()
    {
        if (winOrLoseGroup != null && winOrLoseGroup.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        if (AudioManager.instance != null && buttonClickSFX != null) AudioManager.instance.PlaySFX(buttonClickSFX);
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene"); PlayerPrefs.SetString("PreviousScene", prevScene); SceneLoader.ResetLoadingFlag(); SceneLoader.targetSceneForLoading = prevScene; SceneLoader.keepBackgroundPersistent = false; UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    private IEnumerator SlidePanelY(RectTransform panel, float targetY, float duration)
    {
        if (panel == null) yield break;
        
        Vector2 startPos = panel.anchoredPosition;
        Vector2 targetPos = new Vector2(startPos.x, targetY);
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); // Smooth ease out
            
            panel.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }
        
        panel.anchoredPosition = targetPos;
    }
}
