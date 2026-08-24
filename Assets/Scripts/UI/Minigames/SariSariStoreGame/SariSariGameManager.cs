using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

[Serializable]
public struct NPCData
{
    public string npcType; // e.g. "GuyTeenager"
    public Sprite idleSprite;
    public Sprite happySprite;
    public Sprite wrongSprite;
}

[Serializable]
public class ChismisRoundData
{
    public string npcType;
    public string englishDialogue;
    public string ilokanoDialogue;
    public string cebuanoDialogue;
    public string situationText;
    public string correctPhraseId;
    public string englishFeedback;
    public string ilokanoFeedback;
    public string cebuanoFeedback;
    public string wrongFeedback; // English
    public string ilokanoWrongFeedback;
    public string cebuanoWrongFeedback;
    public string sttWrongFeedback;
    public string ilokanoSttWrongFeedback;
    public string cebuanoSttWrongFeedback;
    public string npcDescription;
    public string npcKeyword;
    public List<string> acceptablePhraseIds;
    public List<string> distractorWords;        // Ilokano distractor tokens
    public List<string> cebuanoDistractorWords; // Cebuano distractor tokens
}

public class SariSariGameManager : MonoBehaviour
{
    public static SariSariGameManager Instance;

    [Header("UI References")]
    public Transform sentenceBox; // Parent for WordSlots
    public Transform wordBoxGroup; // Parent for WordBlocks
    public TextMeshProUGUI situationTextUI; // Tells the player what to do
    
    [Header("Prefabs")]
    public GameObject wordSlotPrefab;
    public GameObject wordBlockPrefab;

    [Header("NPC References")]
    public SariSariNPC leftTambay;
    public SariSariNPC rightTambay;
    public SariSariNPC tindera;

    [Header("NPC Database")]
    public List<NPCData> npcDatabase;

    [Header("STT Colors")]
    public Color sttNormalColor = Color.white;
    public Color sttWarningTextColor = Color.red;
    public Color sttProcessingColor = Color.cyan;
    public Color sttCorrectColor = Color.green;
    public Color sttWrongColor = Color.red;

    private int pendingRewardCoins = 0;

    [Header("Lives & Round UI")]
    public UnityEngine.UI.Image[] candyImages;
    public Sprite[] activeCandySprites;
    public Sprite[] usedCandySprites;
    public TextMeshProUGUI roundTextUI;

    [Header("Result Popup UI")]
    public UnityEngine.UI.Image resultPopupImage;
    public Sprite correctPopupSprite;
    public Sprite wrongPopupSprite;

    [Header("Win/Lose Panel UI")]
    public GameObject winOrLoseGroup;
    public GameObject winPanel;
    public GameObject losePanel;
    public UnityEngine.UI.Image[] winStars;
    public Sprite activeStar;
    public Sprite inactiveStar;
    public TextMeshProUGUI winCoinsText;
    public TextMeshProUGUI loseCoinsText;
    public AudioClip winSFX;
    public AudioClip loseSFX;

    [Header("How To Play UI")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;
    public AudioClip[] slideInSfxs;
    public AudioClip dropSfx;

    [Header("Debug / Cheat Settings")]
    [Tooltip("(Editor only) Start from this round number (1 = first round)")]
    public int startingRound = 1;
    [Tooltip("(Editor only) Start with this many lives (candies). Max 5.")]
    public int startingLives = 5;

    private List<ChismisRoundData> activeSessionRounds = new List<ChismisRoundData>();
    private int currentRoundIndex = 0;
    private int lives = 5; // The 5 Candies
    public bool isCheckingAnswer = false;
    private bool hasGameStarted = false;

    private List<LuminangPhrase> phraseDictionary;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Hide NPCs initially
        if (leftTambay != null) leftTambay.gameObject.SetActive(false);
        if (rightTambay != null) rightTambay.gameObject.SetActive(false);
        if (tindera != null) tindera.gameObject.SetActive(false);

#if UNITY_EDITOR
        // Apply cheat inspector overrides
        lives = Mathf.Clamp(startingLives, 1, 5);
#endif

        LoadMasterDictionary();
        LoadAndBuildSessionRounds();

#if UNITY_EDITOR
        // Jump to the specified starting round (1-based in inspector, 0-based internally)
        currentRoundIndex = Mathf.Clamp(startingRound - 1, 0, activeSessionRounds.Count - 1);
#endif

        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            howToPlayGroup.SetActive(true);
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        else
        {
            hasGameStarted = true;
            StartRound();
        }
    }

    private void LoadMasterDictionary()
    {
        TextAsset dictAsset = Resources.Load<TextAsset>("LuminangPhrases");
        if (dictAsset != null)
        {
            LuminangPhraseData wrapper = JsonUtility.FromJson<LuminangPhraseData>(dictAsset.text);
            phraseDictionary = wrapper.phrases;
        }
        else
        {
            Debug.LogError("Could not find LuminangPhrases.json in Resources!");
        }
    }

    private void LoadAndBuildSessionRounds()
    {
        // 1. Resolve the JSON filename from the config.
        //    The trigger's categoryFilter may be a short name (e.g. "Identity") OR the full JSON filename.
        //    We map short names to the actual filenames here so both work.
        string rawCategory = string.IsNullOrEmpty(SariSariGameConfig.TargetCategory) ? "IdentityExpressions" : SariSariGameConfig.TargetCategory;
        string category = rawCategory switch
        {
            "Identity"   => "IdentityExpressions",
            "Responses"  => "IdentityExpressions", // fallback - same file has responses
            "Greetings"  => "IdentityExpressions", // fallback - same file has greetings
            "Gratitude"  => "IdentityExpressions", // fallback - same file has gratitude
            _            => rawCategory             // already a full filename
        };

        TextAsset jsonAsset = Resources.Load<TextAsset>(category);
        if (jsonAsset == null)
        {
            Debug.LogError($"[SariSari] Could not find {category}.json in Resources! (rawCategory was '{rawCategory}')");
            return;
        }

        // Parse using a wrapper array since it's a raw JSON array
        ChismisRoundData[] allRounds = JsonHelper.FromJson<ChismisRoundData>("{\"Items\":" + jsonAsset.text + "}");

        // 2. Filter pools by category
        List<ChismisRoundData> identityRounds = allRounds.Where(r => r.correctPhraseId.StartsWith("identity")).ToList();
        List<ChismisRoundData> responsesRounds = allRounds.Where(r => r.correctPhraseId.StartsWith("responses")).ToList();
        List<ChismisRoundData> gratitudeRounds = allRounds.Where(r => r.correctPhraseId.StartsWith("gratitude")).ToList();
        List<ChismisRoundData> greetingsRounds = allRounds.Where(r => r.correctPhraseId.StartsWith("greetings")).ToList();

        // 3. Select 15 rounds
        activeSessionRounds.Clear();
        activeSessionRounds.AddRange(identityRounds);
        Shuffle(responsesRounds);
        activeSessionRounds.AddRange(responsesRounds.Take(3));
        Shuffle(gratitudeRounds);
        activeSessionRounds.AddRange(gratitudeRounds.Take(2));
        Shuffle(greetingsRounds);
        activeSessionRounds.AddRange(greetingsRounds.Take(2));

        // 4. Shuffle final session, ensuring no identical phrases back-to-back
        ShuffleAndSeparate(activeSessionRounds);
    }

    private void ShuffleAndSeparate(List<ChismisRoundData> list)
    {
        Shuffle(list); // First, randomly shuffle everything
        List<ChismisRoundData> result = new List<ChismisRoundData>();
        
        while (list.Count > 0)
        {
            int indexToPick = 0;
            if (result.Count > 0)
            {
                // Try to find a round that doesn't match the last added one
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].correctPhraseId != result[result.Count - 1].correctPhraseId)
                    {
                        indexToPick = i;
                        break;
                    }
                }
            }
            
            result.Add(list[indexToPick]);
            list.RemoveAt(indexToPick);
        }
        
        // Apply separated order back to list
        list.Clear();
        list.AddRange(result);
    }

    private void UpdateLivesUI()
    {
        if (candyImages == null || candyImages.Length != 5) return;
        
        for (int i = 0; i < candyImages.Length; i++)
        {
            if (i < lives)
            {
                // Active Candy
                if (activeCandySprites != null && i < activeCandySprites.Length && activeCandySprites[i] != null)
                {
                    candyImages[i].sprite = activeCandySprites[i];
                }
            }
            else
            {
                // Used Candy
                if (usedCandySprites != null && i < usedCandySprites.Length && usedCandySprites[i] != null)
                {
                    candyImages[i].sprite = usedCandySprites[i];
                }
            }
        }
    }

    private void StartRound()
    {
        isCheckingAnswer = false; // Reset lock flag
        
        if (tindera != null) tindera.SetIdle();
        if (leftTambay != null) leftTambay.SetIdle();
        
        if (currentRoundIndex < 0 || currentRoundIndex >= activeSessionRounds.Count)
        {
            if (currentRoundIndex < 0)
                Debug.LogError($"[SariSari] currentRoundIndex is {currentRoundIndex} (negative!). Rounds loaded: {activeSessionRounds.Count}");
            else
                Debug.Log("Game Won! Session complete.");
            ShowWinScreen();
            return;
        }

        ChismisRoundData currentRound = activeSessionRounds[currentRoundIndex];
        
        // Setup UI
        if (situationTextUI != null) situationTextUI.text = currentRound.situationText;
        if (roundTextUI != null) roundTextUI.text = $"Round: {currentRoundIndex + 1}/{activeSessionRounds.Count}";
        UpdateLivesUI();

        // NPC Spawning
        if (leftTambay != null) leftTambay.gameObject.SetActive(false);
        if (rightTambay != null) rightTambay.gameObject.SetActive(false);
        if (tindera != null) tindera.gameObject.SetActive(false);

        NPCData npcData = npcDatabase.FirstOrDefault(n => n.npcType == currentRound.npcType);
        SariSariNPC activeNPC = (currentRound.npcType == "Tindera") ? tindera : GetActiveCustomer();

        if (npcData.npcType != null && activeNPC != null)
        {
            activeNPC.SetSprites(npcData.idleSprite, npcData.happySprite, npcData.wrongSprite);
            
            // Tindera is ALWAYS visually present!
            if (tindera != null)
            {
                tindera.gameObject.SetActive(true);
                tindera.HideDialogue(); // Hide her bubble by default
            }
            
            // Activate the customer and play slide-in animation
            if (activeNPC != tindera)
            {
                activeNPC.gameObject.SetActive(true);
                // Slide in from the left if it's leftTambay, right if it's rightTambay
                float startOffset = (activeNPC == leftTambay) ? -800f : 800f;
                activeNPC.PlaySlideInAnimation(startOffset, 0.5f);
                
                // Play random slide-in SFX
                if (sfxSource != null && slideInSfxs != null && slideInSfxs.Length > 0)
                {
                    AudioClip randomSlideSfx = slideInSfxs[UnityEngine.Random.Range(0, slideInSfxs.Length)];
                    if (randomSlideSfx != null) sfxSource.PlayOneShot(randomSlideSfx);
                }
            }
            else
            {
                // If Tindera is the only active NPC for this round, she only pops in on Round 1
                if (currentRoundIndex == 0 && tindera != null) tindera.PlayPopInAnimation(0.4f);
            }
            
            // Define silent NPCs that need Tindera hints
            bool isSilentNPC = currentRound.npcType == "GirlTurnedAway" || currentRound.npcType == "Guy40s";
            
            string targetLanguage = SariSariGameConfig.TargetLanguage.ToLower();
            string nativeDialogue = targetLanguage == "cebuano" ? currentRound.cebuanoDialogue : currentRound.ilokanoDialogue;
            if (string.IsNullOrEmpty(nativeDialogue) || nativeDialogue == "...") nativeDialogue = currentRound.englishDialogue;

            if (isSilentNPC)
            {
                activeNPC.HideDialogue();
                if (tindera != null)
                {
                    tindera.ShowDialogue(nativeDialogue, currentRound.englishDialogue); // Tindera speaks for them!
                    if (currentRoundIndex == 0) tindera.PlayPopInAnimation(0.4f);
                }
            }
            else
            {
                // Normal dialogue logic for whoever is active
                if (nativeDialogue != "...")
                    activeNPC.ShowDialogue(nativeDialogue, currentRound.englishDialogue);
                else
                    activeNPC.HideDialogue();
            }
        }

        // Spawn Word Blocks!
        SpawnWordBlocks(currentRound);
    }

    private void SpawnWordBlocks(ChismisRoundData round)
    {
        // 1. Clean up old blocks and slots
        foreach (Transform child in sentenceBox) Destroy(child.gameObject);
        foreach (Transform child in wordBoxGroup) Destroy(child.gameObject);

        // 2. Look up the translation
        LuminangPhrase targetPhrase = phraseDictionary.FirstOrDefault(p => p.id == round.correctPhraseId);
        if (targetPhrase == null) return;

        // 3. Determine the correct words based on the ACTIVE target language
        string targetLanguage = SariSariGameConfig.TargetLanguage.ToLower();
        bool isCebuano = targetLanguage == "cebuano";

        List<string> correctWords = new List<string>();
        if (targetPhrase.type == "template")
        {
            // For templates, ALWAYS split the full target string (e.g. "taga {place} ko")
            // This ensures the {place}/{name} token is present so an input block slot is created.
            // DO NOT use requiredTokens here — those are for validation only, not block spawning.
            string templateTarget = isCebuano ? targetPhrase.cebuano_target : targetPhrase.ilokano_target;
            if (!string.IsNullOrEmpty(templateTarget))
            {
                correctWords.AddRange(templateTarget.Split(' '));
            }
            else
            {
                // Last resort fallback: use the base phrase
                string nativePhrase = isCebuano ? targetPhrase.cebuano : targetPhrase.ilokano;
                correctWords.AddRange(nativePhrase.Split(' '));
            }
        }
        else
        {
            // Fixed phrase: split the native translation into individual word tokens
            string nativePhrase = isCebuano ? targetPhrase.cebuano : targetPhrase.ilokano;
            if (string.IsNullOrEmpty(nativePhrase)) nativePhrase = targetPhrase.ilokano;
            correctWords.AddRange(nativePhrase.Split(' '));
        }

        // 4. Spawn an empty slot in the SentenceBox for every correct word
        for (int i = 0; i < correctWords.Count; i++)
        {
            Instantiate(wordSlotPrefab, sentenceBox);
        }

        // 5. Pick distractors from the language-appropriate list
        List<string> distractors = isCebuano
            ? (round.cebuanoDistractorWords != null && round.cebuanoDistractorWords.Count > 0
                ? round.cebuanoDistractorWords
                : round.distractorWords)   // fallback to Ilokano if Cebuano not set
            : round.distractorWords;

        // 6. Gather all correct words + distractor words, capped at 6 total
        //    NOTE: {variable} tokens like {place} MUST be included — SariSariWordBlock renders them
        //    as draggable "(Type place..)" input blocks that become editable when dropped into a slot.
        List<string> allWords = new List<string>(correctWords);
        if (distractors != null)
        {
            foreach (string distractor in distractors)
            {
                if (allWords.Count >= 6) break;
                allWords.Add(distractor);
            }
        }
        Shuffle(allWords);

        // 7. Spawn the draggable WordBlocks into the WordBank
        foreach (string word in allWords)
        {
            GameObject blockObj = Instantiate(wordBlockPrefab, wordBoxGroup);
            SariSariWordBlock block = blockObj.GetComponent<SariSariWordBlock>();
            if (block != null)
            {
                block.SetWord(word);
            }
        }
    }

    // Optional visual update method, validation is moved to SubmitSentence
    public void CheckSentenceState()
    {
        // Could highlight submit button here if all slots filled
    }

    public void SubmitSentence()
    {
        if (currentRoundIndex >= activeSessionRounds.Count) return; // Safety check if game is already won!
        if (isCheckingAnswer) return; // Prevent spam clicking

        // 1. Check if all slots are filled
        List<SariSariWordBlock> playerBlocks = new List<SariSariWordBlock>();
        foreach (Transform slotObj in sentenceBox)
        {
            SariSariWordSlot slot = slotObj.GetComponent<SariSariWordSlot>();
            if (slot == null || slot.CurrentBlock == null)
            {
                // If any slot is empty, we don't check yet! Play wrong face to hint they need to fill it
                SariSariNPC activeNPC_empty = (activeSessionRounds[currentRoundIndex].npcType == "Tindera") ? tindera : leftTambay;
                activeNPC_empty.SetWrong();
                Invoke("ResetNPCIdle", 1.5f);
                return; 
            }
            playerBlocks.Add(slot.CurrentBlock);
        }

        isCheckingAnswer = true; // Lock further submission

        // 2. If we reach here, all slots are filled! Let's check the sentence.
        ChismisRoundData currentRound = activeSessionRounds[currentRoundIndex];
        LuminangPhrase targetPhrase = phraseDictionary.FirstOrDefault(p => p.id == currentRound.correctPhraseId);
        if (targetPhrase == null) 
        {
            isCheckingAnswer = false;
            return;
        }

        List<string> correctWords = new List<string>();
        string targetLanguage = SariSariGameConfig.TargetLanguage.ToLower();
        
        if (targetLanguage == "cebuano")
        {
            // For templates, split the FULL cebuano_target (includes {place}/{name} tokens)
            // This must match exactly how SpawnWordBlocks lays out the slots.
            if (targetPhrase.type == "template" && !string.IsNullOrEmpty(targetPhrase.cebuano_target))
            {
                correctWords.AddRange(targetPhrase.cebuano_target.Split(' '));
            }
            else if (targetPhrase.cebuano_required_tokens != null && targetPhrase.cebuano_required_tokens.Count > 0)
            {
                correctWords.AddRange(targetPhrase.cebuano_required_tokens);
            }
            else
            {
                correctWords.AddRange(targetPhrase.cebuano.Split(' '));
            }
        }
        else
        {
            // Ilokano target logic
            if (targetPhrase.type == "template" && !string.IsNullOrEmpty(targetPhrase.ilokano_target))
            {
                correctWords.AddRange(targetPhrase.ilokano_target.Split(' '));
            }
            else if (targetPhrase.ilokano_required_tokens != null && targetPhrase.ilokano_required_tokens.Count > 0)
            {
                correctWords.AddRange(targetPhrase.ilokano_required_tokens);
            }
            else
            {
                correctWords.AddRange(targetPhrase.ilokano.Split(' '));
            }
        }

        // 3. Compare them
        bool isCorrect = true;
        if (playerBlocks.Count != correctWords.Count)
        {
            isCorrect = false;
        }
        else
        {
            for (int i = 0; i < correctWords.Count; i++)
            {
                // If it's a template variable, they MUST have placed the input block here
                if (correctWords[i].StartsWith("{") && correctWords[i].EndsWith("}"))
                {
                    if (!playerBlocks[i].isInputBlock)
                    {
                        isCorrect = false;
                        break;
                    }

                    string typedWord = playerBlocks[i].CurrentWord;
                    // As long as they typed *something* and it's not the default placeholder
                    if (string.IsNullOrWhiteSpace(typedWord) || typedWord.StartsWith("<") || typedWord.StartsWith("(Type"))
                    {
                        isCorrect = false;
                        break;
                    }
                }
                else 
                {
                    // Must be a normal block, and must match exactly
                    if (playerBlocks[i].isInputBlock || playerBlocks[i].CurrentWord != correctWords[i])
                    {
                        isCorrect = false;
                        break;
                    }
                }
            }
        }

        // 4. Handle Result
        SariSariNPC activeNPC = (currentRound.npcType == "Tindera") ? tindera : GetActiveCustomer();
        bool isSilentNPC = currentRound.npcType == "GirlTurnedAway" || currentRound.npcType == "Guy40s";

        if (isCorrect)
        {
            Debug.Log("Sentence is Correct! Transitioning to STT Phase.");
            if (SariSariSTTManager.Instance != null)
            {
                bool isTemplate = targetPhrase != null && targetPhrase.type == "template";
                string templateSentence = targetLanguage == "cebuano" ? targetPhrase.cebuano : targetPhrase.ilokano;
                
                string targetSentence = templateSentence;
                if (isTemplate)
                {
                    List<string> finalWords = new List<string>();
                    foreach (var b in playerBlocks) finalWords.Add(b.CurrentWord);
                    targetSentence = string.Join(" ", finalWords);
                }
                
                SariSariSTTManager.Instance.StartSTT(currentRound, targetSentence, isTemplate);
            }
            else
            {
                // Fallback if STT manager is missing
                OnSTTSuccess();
            }
        }
        else
        {
            Debug.Log("Sentence is Wrong! Try again.");
            activeNPC.SetWrong();
            if (sfxSource != null && wrongSfx != null) sfxSource.PlayOneShot(wrongSfx);
            ShowResultPopup(false);
            
            // Show Feedback Dialogue
            if (isSilentNPC) tindera.HideDialogue();
            
            List<string> finalWordsWrong = new List<string>();
            foreach (var b in playerBlocks) finalWordsWrong.Add(b.CurrentWord);
            string playerString = string.Join(" ", finalWordsWrong);
            
            // Get native wrong feedback
            string wrongFeedbackNativeTemplate = targetLanguage == "cebuano" ? currentRound.cebuanoWrongFeedback : currentRound.ilokanoWrongFeedback;
            if (string.IsNullOrEmpty(wrongFeedbackNativeTemplate)) wrongFeedbackNativeTemplate = currentRound.wrongFeedback;
            
            string wrongTextNative = string.Format(wrongFeedbackNativeTemplate, playerString);
            string wrongTextEnglish = string.Format(currentRound.wrongFeedback, playerString);
            activeNPC.ShowDialogue(wrongTextNative, wrongTextEnglish);
            
            lives--;
            UpdateLivesUI();

            if (lives <= 0)
            {
                Invoke("ShowLoseScreen", 2.0f);
                return;
            }
            else
            {
                // For now, just show wrong face for 2.0s then unlock (Wait a bit longer to read feedback)
                Invoke("ResetNPCIdle", 2.0f);
            }
        }
    }

    public void OnSTTSuccess()
    {
        ChismisRoundData currentRound = activeSessionRounds[currentRoundIndex];
        SariSariNPC activeNPC = (currentRound.npcType == "Tindera") ? tindera : GetActiveCustomer();
        bool isSilentNPC = currentRound.npcType == "GirlTurnedAway" || currentRound.npcType == "Guy40s";

        activeNPC.SetHappy();
        if (sfxSource != null && correctSfx != null) sfxSource.PlayOneShot(correctSfx);
        ShowResultPopup(true);
        
        if (isSilentNPC) tindera.HideDialogue();
        string targetLanguage = SariSariGameConfig.TargetLanguage.ToLower();
        string correctFeedbackNative = targetLanguage == "cebuano" ? currentRound.cebuanoFeedback : currentRound.ilokanoFeedback;
        if (string.IsNullOrEmpty(correctFeedbackNative)) correctFeedbackNative = currentRound.englishFeedback;
        activeNPC.ShowDialogue(correctFeedbackNative, currentRound.englishFeedback);
        
        Invoke("NextRound", 2.0f);
    }

    public void OnSTTFailure(string targetSentence)
    {
        ChismisRoundData currentRound = activeSessionRounds[currentRoundIndex];
        SariSariNPC activeNPC = (currentRound.npcType == "Tindera") ? tindera : GetActiveCustomer();
        bool isSilentNPC = currentRound.npcType == "GirlTurnedAway" || currentRound.npcType == "Guy40s";

        activeNPC.SetWrong();
        if (sfxSource != null && wrongSfx != null) sfxSource.PlayOneShot(wrongSfx);
        ShowResultPopup(false);
        
        if (isSilentNPC) tindera.HideDialogue();
        
        string targetLanguage = SariSariGameConfig.TargetLanguage.ToLower();
        string sttWrongNativeTemplate = targetLanguage == "cebuano" ? currentRound.cebuanoSttWrongFeedback : currentRound.ilokanoSttWrongFeedback;
        
        // Fallback to English if native isn't provided
        if (string.IsNullOrEmpty(sttWrongNativeTemplate)) sttWrongNativeTemplate = currentRound.sttWrongFeedback;
        
        // Final fallback if absolutely nothing is provided
        if (string.IsNullOrEmpty(sttWrongNativeTemplate)) 
        {
            if (targetLanguage == "cebuano") sttWrongNativeTemplate = "Nakita nako nga gisulayan nimo pagsulti og '{0}'. Mas maayo pa nimo!";
            else if (targetLanguage == "ilokano") sttWrongNativeTemplate = "Makita k a padasem a sawen ti '{0}'. Maka-aramid ka pay ti nasaysayaat!";
            else sttWrongNativeTemplate = "I can see you're trying to say '{0}'. You can do better!";
        }
        
        string engFallback = string.IsNullOrEmpty(currentRound.sttWrongFeedback) ? "I can see you're trying to say '{0}'. You can do better!" : currentRound.sttWrongFeedback;

        string wrongTextNative = string.Format(sttWrongNativeTemplate, targetSentence);
        string wrongTextEnglish = string.Format(engFallback, targetSentence);
        
        activeNPC.ShowDialogue(wrongTextNative, wrongTextEnglish);
        
        // Note: No candy deducted on STT failure, do not advance round, just unlock
        Invoke("ResetNPCIdle", 2.0f);
    }

    private void ShowResultPopup(bool isCorrect)
    {
        if (resultPopupImage == null) return;
        resultPopupImage.sprite = isCorrect ? correctPopupSprite : wrongPopupSprite;
        resultPopupImage.gameObject.SetActive(true);
        StartCoroutine(PopInThenOut(resultPopupImage.transform));
    }

    private IEnumerator PopInThenOut(Transform target)
    {
        target.localScale = Vector3.zero;
        float duration = 0.3f;
        float elapsed = 0f;
        
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        // Pop in
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float t2 = t - 1f;
            float ease = 1f + c3 * (t2 * t2 * t2) + c1 * (t2 * t2);
            target.localScale = Vector3.one * ease;
            yield return null;
        }
        target.localScale = Vector3.one;
        
        // Wait
        yield return new WaitForSeconds(1.0f);
        
        // Pop out
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.2f);
            target.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        target.localScale = Vector3.zero;
        target.gameObject.SetActive(false);
    }

    private void ResetNPCIdle()
    {
        isCheckingAnswer = false; // Unlock submission
        if (currentRoundIndex >= activeSessionRounds.Count) return;
        
        ChismisRoundData currentRound = activeSessionRounds[currentRoundIndex];
        SariSariNPC activeNPC = (currentRound.npcType == "Tindera") ? tindera : leftTambay;
        activeNPC.SetIdle();
        
        // Restore original prompt dialogue
        bool isSilentNPC = currentRound.npcType == "GirlTurnedAway" || currentRound.npcType == "Guy40s";
        string targetLanguage = SariSariGameConfig.TargetLanguage.ToLower();
        string nativeDialogue = targetLanguage == "cebuano" ? currentRound.cebuanoDialogue : currentRound.ilokanoDialogue;
        if (string.IsNullOrEmpty(nativeDialogue) || nativeDialogue == "...") nativeDialogue = currentRound.englishDialogue;

        if (isSilentNPC)
        {
            if (activeNPC != null) activeNPC.HideDialogue();
            if (tindera != null) tindera.ShowDialogue(nativeDialogue, currentRound.englishDialogue);
        }
        else
        {
            if (activeNPC != null)
            {
                if (nativeDialogue != "...")
                    activeNPC.ShowDialogue(nativeDialogue, currentRound.englishDialogue);
                else
                    activeNPC.HideDialogue();
            }
        }
    }

    private void NextRound()
    {
        currentRoundIndex++;
        StartRound();
    }

    // Standard Fisher-Yates shuffle
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int rnd = UnityEngine.Random.Range(0, n + 1);
            T temp = list[n];
            list[n] = list[rnd];
            list[rnd] = temp;
        }
    }

    public void PlayDropSfx()
    {
        if (sfxSource != null && dropSfx != null)
        {
            sfxSource.PlayOneShot(dropSfx);
        }
    }

    private void ShowWinScreen()
    {
        if (sfxSource != null && winSFX != null) sfxSource.PlayOneShot(winSFX);
        
        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            winPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        if (losePanel != null) losePanel.SetActive(false);

        int stars = 0;
        int coinsEarned = 0;

        if (lives >= 5) { stars = 5; coinsEarned = 50; }
        else if (lives == 4) { stars = 4; coinsEarned = 40; }
        else if (lives == 3) { stars = 3; coinsEarned = 30; }
        else if (lives == 2) { stars = 2; coinsEarned = 20; }
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

        PlayerPrefs.SetInt("SariSariMinigameWon", 1);
        PlayerPrefs.SetInt("MinigameWon", 1);
        PlayerPrefs.Save();
    }

    private void ShowLoseScreen()
    {
        if (sfxSource != null && loseSFX != null) sfxSource.PlayOneShot(loseSFX);

        if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) 
        {
            losePanel.SetActive(true);
            losePanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        
        if (loseCoinsText != null) loseCoinsText.text = "+2";

        pendingRewardCoins = 2;

        PlayerPrefs.SetInt("SariSariMinigameWon", 0);
        PlayerPrefs.SetInt("MinigameWon", 0);
        PlayerPrefs.Save();
    }

    private SariSariNPC GetActiveCustomer()
    {
        // Alternate between left and right customers based on the round index!
        return (currentRoundIndex % 2 == 0) ? leftTambay : rightTambay;
    }

    public void RestartGame()
    {
        if (winOrLoseGroup != null && winOrLoseGroup.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        // Use MinigameReloader so we don't accidentally unload the main game (Magellan) background scene
        MinigameReloader.ReloadActiveMinigame();
    }

    public void QuitToMenu()
    {
        if (winOrLoseGroup != null && winOrLoseGroup.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        // Load whatever scene they came from (e.g. Map or Menu)
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene"); PlayerPrefs.SetString("PreviousScene", prevScene); SceneLoader.ResetLoadingFlag(); SceneLoader.targetSceneForLoading = prevScene; SceneLoader.keepBackgroundPersistent = false; UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    public void CloseHowToPlay()
    {
        if (sfxSource != null && correctSfx != null) // Fallback for a button click sound
        {
            // You can optionally add a generic UI click sound here later if desired
        }

        if (howToPlayGroup != null) 
        {
            howToPlayGroup.SetActive(false);
        }

        if (!hasGameStarted)
        {
            hasGameStarted = true;
            StartRound();
        }
        else
        {
            // If opened mid-game, simply resume (unpause if you have pause logic)
        }
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && hasGameStarted)
        {
            lives = Mathf.Clamp(startingLives, 1, 5);
            if (activeSessionRounds != null && activeSessionRounds.Count > 0)
            {
                currentRoundIndex = Mathf.Clamp(startingRound - 1, 0, activeSessionRounds.Count - 1);
            }
            
            UpdateLivesUI();
            if (roundTextUI != null && activeSessionRounds != null) 
            {
                roundTextUI.text = $"Round: {currentRoundIndex + 1}/{activeSessionRounds.Count}";
            }
        }
    }

    private void Update()
    {
        try 
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.pKey != null && keyboard.pKey.wasPressedThisFrame)
                {
                    Debug.Log("<color=magenta>CHEAT: Forced Pass STT (P)</color>");
                    OnSTTSuccess();
                }
                else if (keyboard.wKey != null && keyboard.wKey.wasPressedThisFrame)
                {
                    Debug.Log("<color=magenta>CHEAT: Forced Win (W)</color>");
                    ShowWinScreen();
                }
                else if (keyboard.lKey != null && keyboard.lKey.wasPressedThisFrame)
                {
                    Debug.Log("<color=magenta>CHEAT: Forced Lose (L)</color>");
                    ShowLoseScreen();
                }
            }
        }
        catch (System.Exception)
        {
            // Silently swallow NullReferenceExceptions thrown by Unity's Simulator virtual keyboard
        }
    }
#endif
}

// Helper class for parsing raw JSON arrays in Unity
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}
