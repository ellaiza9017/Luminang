using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;

namespace Luminang.UI.Minigames
{
    public class TCGGameManager : MonoBehaviour
    {
        public static TCGGameManager Instance { get; private set; }

        [Header("Card Slots & Transforms")]
        public RectTransform[] playerCardSlots;      // Hand slots (Card_1 to Card_4)
        public RectTransform[] enemyCardSlots;       // Top enemy slots (EnemyCard1 to EnemyCard4)
        public RectTransform deckTransform;          // Card_Deck position (stacked source)
        public RectTransform enemyPlayedSlot;        // EnemyPlayedCard position
        public RectTransform playerPlayedSlot;       // PlayerPlayedCard position

        [Header("Enemy Played Card UI")]
        public Image enemyPlayedInnerImage;          // Inner picture on EnemyPlayedCard
        public TextMeshProUGUI enemyPlayedSituationText; // Situation text on EnemyPlayedCard

        [Header("Card Sprites")]
        public Sprite cardBackSprite;
        public Sprite cardFrontSprite;

        [Header("Preview / View Panels")]
        public GameObject blackOverlay;
        public GameObject viewCardRoot;              // ViewCard parent

        [Header("Enemy View UI")]
        public GameObject playerViewEnemyCardPanel;  // PlayerViewEnemyCard GameObject
        public TextMeshProUGUI enemyViewSituationText;
        public Image enemyViewImage;

        [Header("Player View UI")]
        public GameObject playerViewCardPanel;       // PlayerViewCard GameObject
        public TextMeshProUGUI playerViewWordText;
        public RectTransform playerViewUnderline;
        public Button chooseCardButton;
        public float underlinePadding = 10f;

        [Header("HUD References")]
        public TextMeshProUGUI roundNumberText;      // RoundNumber
        public TextMeshProUGUI roundTotalText;       // RoundTotal
        public TextMeshProUGUI cardsLeftNumberText;  // CardsLeft Number text

        [Header("Correct / Wrong UI")]
        public GameObject correctOrWrongRoot;
        public GameObject correctBanner;             // Correct GameObject
        public GameObject wrongBanner;               // Wrong GameObject
        public float bannerDuration = 1.5f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip buttonClickSFX;
        public AudioClip correctSFX;
        public AudioClip wrongSFX;
        public AudioClip winSFX;
        public AudioClip loseSFX;
        public AudioClip cardSlideSFX;

        [Header("Win / Lose UI Panels")]
        public GameObject winOrLoseGroup;
        public GameObject winPanel;
        public GameObject losePanel;
        public TextMeshProUGUI winCoinsText;
        public TextMeshProUGUI loseCoinsText;
        public Image[] winStars;
        public Sprite activeStarSprite;
        public Sprite inactiveStarSprite;

        [Header("How To Play UI")]
        public GameObject howToPlayGroup;
        public GameObject howToPlayPanel;
        public Button howToPlayCloseButton;

        [Header("Game Mode Config")]
        public int totalRounds = 20;
        public int initialCardsLeft = 25;

        private List<TCGRoundData> roundPool = new List<TCGRoundData>();
        private int currentRoundIndex = 0;           // 0-indexed internally
        private int cardsLeft;
        
        private TCGRoundData currentRoundData;
        private Sprite currentEnemySituationSprite;
        private List<PhraseEntry> roundOptions = new List<PhraseEntry>(); // 4 choices
        private int correctOptionIndex = -1;

        private int currentlySelectedSlotIndex = -1;
        private int lastEnemyCardSlotIndex = -1;
        private bool isInteractionLocked = true;

        [Header("Debug & Layout Adjustments")]
        [Tooltip("If enabled, rotates EnemyPlayedCard 180 degrees so it is oriented upside-down.")]
        public bool rotateEnemyPlayedCardUpsideDown = false;

        private Vector2[] playerCardOriginalAnchoredPositions;
        private Vector3[] playerCardOriginalScales;
        private Vector2 enemyPlayedSlotOriginalAnchoredPos;
        private Vector3 enemyPlayedSlotOriginalScale;
        private Vector2 playerPlayedSlotOriginalAnchoredPos;
        private Vector3 playerPlayedSlotOriginalScale;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private IEnumerator Start()
        {
            Canvas.ForceUpdateCanvases();
            
            // Give Unity's CanvasScaler and LayoutGroups one full frame to completely finalize 
            // the screen resolution and safe area layout before we cache their world positions!
            yield return new WaitForEndOfFrame();

            // Disable main game joysticks and touchpads in background scenes (e.g. Magellan's_Cross)
            SceneMinigameTrigger.DisableMainGameControls();

            // Make the deck render behind the cards so they fly off the top
            if (deckTransform != null)
            {
                deckTransform.SetAsFirstSibling();
            }

            // Cache starting anchored positions and scales of card slots
            playerCardOriginalAnchoredPositions = new Vector2[playerCardSlots.Length];
            playerCardOriginalScales = new Vector3[playerCardSlots.Length];
            for (int i = 0; i < playerCardSlots.Length; i++)
            {
                playerCardOriginalAnchoredPositions[i] = playerCardSlots[i].anchoredPosition;
                playerCardOriginalScales[i] = playerCardSlots[i].localScale;
                // Move them to the deck in world space to start hidden
                playerCardSlots[i].position = deckTransform.position;
                playerCardSlots[i].gameObject.SetActive(false);
            }

            enemyPlayedSlotOriginalAnchoredPos = enemyPlayedSlot.anchoredPosition;
            enemyPlayedSlotOriginalScale = enemyPlayedSlot.localScale;
            enemyPlayedSlot.position = deckTransform.position;
            SetupEnemyPlayedCardVisuals(false);
            enemyPlayedSlot.gameObject.SetActive(false);

            playerPlayedSlotOriginalAnchoredPos = playerPlayedSlot.anchoredPosition;
            playerPlayedSlotOriginalScale = playerPlayedSlot.localScale;
            playerPlayedSlot.position = deckTransform.position;
            playerPlayedSlot.gameObject.SetActive(false);

            // Auto-find enemyCardSlots if not set in inspector
            if (enemyCardSlots == null || enemyCardSlots.Length == 0)
            {
                Transform enemyGroup = transform.Find("TCG_Cards/Enemy_Cards") ?? transform.parent?.Find("TCG_Cards/Enemy_Cards");
                if (enemyGroup != null)
                {
                    List<RectTransform> slots = new List<RectTransform>();
                    for (int c = 0; c < enemyGroup.childCount; c++)
                    {
                        RectTransform rt = enemyGroup.GetChild(c) as RectTransform;
                        if (rt != null) slots.Add(rt);
                    }
                    enemyCardSlots = slots.ToArray();
                }
            }

            // Hide overlay and view elements
            if (blackOverlay != null) blackOverlay.SetActive(false);
            if (viewCardRoot != null) viewCardRoot.SetActive(false);
            if (playerViewEnemyCardPanel != null) playerViewEnemyCardPanel.SetActive(false);
            if (playerViewCardPanel != null) playerViewCardPanel.SetActive(false);
            if (correctBanner != null) correctBanner.SetActive(false);
            if (wrongBanner != null) wrongBanner.SetActive(false);
            if (winOrLoseGroup != null) winOrLoseGroup.SetActive(false);

            // Wire up slot buttons and overlay clicks
            for (int i = 0; i < playerCardSlots.Length; i++)
            {
                int index = i;
                Button btn = playerCardSlots[i].GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnPlayerCardClicked(index));
                }
            }

            Button enemyPlayedBtn = enemyPlayedSlot.GetComponent<Button>();
            if (enemyPlayedBtn != null)
            {
                enemyPlayedBtn.onClick.RemoveAllListeners();
                enemyPlayedBtn.onClick.AddListener(OnEnemyPlayedCardClicked);
            }

            if (blackOverlay != null)
            {
                Button overlayBtn = blackOverlay.GetComponent<Button>();
                if (overlayBtn != null)
                {
                    overlayBtn.onClick.RemoveAllListeners();
                    overlayBtn.onClick.AddListener(OnCloseCardView);
                }
            }

            if (chooseCardButton != null)
            {
                chooseCardButton.onClick.RemoveAllListeners();
                chooseCardButton.onClick.AddListener(OnChooseCardPressed);
            }

            // Wire up howToPlay close buttons dynamically
            if (howToPlayCloseButton != null)
            {
                howToPlayCloseButton.onClick.RemoveAllListeners();
                howToPlayCloseButton.onClick.AddListener(CloseHowToPlay);
            }

            if (howToPlayPanel != null)
            {
                Button[] htpBtns = howToPlayPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in htpBtns)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(CloseHowToPlay);
                }
            }

            if (howToPlayGroup != null)
            {
                Button groupBtn = howToPlayGroup.GetComponent<Button>();
                if (groupBtn != null)
                {
                    groupBtn.onClick.RemoveAllListeners();
                    groupBtn.onClick.AddListener(CloseHowToPlay);
                }
            }

            // Start screen flow
            if (howToPlayGroup != null && howToPlayPanel != null)
            {
                howToPlayGroup.SetActive(true);
                StartCoroutine(TCGCardAnimator.Instance.PopIn(howToPlayPanel.transform));
            }
            else
            {
                StartGame();
            }
        }

        private void Update()
        {
            // Allow closing HowToPlay with Space, Escape, or Enter if active
            if (howToPlayGroup != null && howToPlayGroup.activeSelf)
            {
                #if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                {
                    CloseHowToPlay();
                    return;
                }
                #endif

                #if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
                {
                    CloseHowToPlay();
                    return;
                }
                #endif
            }

            // Debug Keys (development build and editor only)
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            #if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame) DebugWinGame();
                if (keyboard.eKey.wasPressedThisFrame) DebugLoseGame();
            }
            #endif

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Q)) DebugWinGame();
            if (Input.GetKeyDown(KeyCode.E)) DebugLoseGame();
            #endif
            #endif
        }

        public void DebugWinGame()
        {
            ShowWinScreen();
        }

        public void DebugLoseGame()
        {
            ShowLoseScreen();
        }

        public void CloseHowToPlay()
        {
            Debug.Log("<color=yellow>[TCGGameManager] CloseHowToPlay() clicked!</color>");
            PlaySFX(buttonClickSFX);

            if (howToPlayPanel != null && TCGCardAnimator.Instance != null)
            {
                StartCoroutine(TCGCardAnimator.Instance.PopOut(howToPlayPanel.transform, 0.2f, () =>
                {
                    if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
                    StartGame();
                }));
            }
            else
            {
                Debug.LogWarning($"[TCGGameManager] howToPlayPanel is {(howToPlayPanel == null ? "NULL" : "assigned")}, howToPlayGroup is {(howToPlayGroup == null ? "NULL" : "assigned")}, Animator Instance is {(TCGCardAnimator.Instance == null ? "NULL" : "assigned")}");
                if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
                if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
                StartGame();
            }
        }

        private void StartGame()
        {
            cardsLeft = initialCardsLeft;
            currentRoundIndex = 0;

            UpdateHUD();
            BuildRoundPool();
            SetupNextRound();
        }

        private void BuildRoundPool()
        {
            roundPool.Clear();

            // Sync Target Language from PlayerPrefs / FishingGameConfig
            if (PlayerPrefs.HasKey("TargetLanguage"))
            {
                string savedLang = PlayerPrefs.GetString("TargetLanguage", "");
                if (!string.IsNullOrEmpty(savedLang)) FishingGameConfig.TargetLanguage = savedLang;
            }

            // Load cards metadata
            TextAsset databaseAsset = Resources.Load<TextAsset>("Minigames/TCG/TCGEnemyCards");
            if (databaseAsset == null)
            {
                Debug.LogError("[TCGGameManager] TCGEnemyCards.json database is missing from Resources/Minigames/TCG/!");
                return;
            }

            TCGEnemyCardDatabase db = JsonUtility.FromJson<TCGEnemyCardDatabase>(databaseAsset.text);
            if (db == null || db.cards == null || db.cards.Count == 0)
            {
                Debug.LogError("[TCGGameManager] TCGEnemyCards database failed to parse or is empty!");
                return;
            }

            // Ensure DatasetManager is initialized
            if (DatasetManager.Instance == null)
            {
                gameObject.AddComponent<DatasetManager>();
            }

            // The 5 official TCG categories: Requests, Greetings, Gratitude, Identity, Responses
            HashSet<string> validTCGCategories = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "Requests", "Greetings", "Gratitude", "Identity", "Responses"
            };

            // Filter to only cards belonging to the 5 official TCG categories
            List<TCGEnemyCardEntry> validCards = db.cards.Where(c => validTCGCategories.Contains(c.category)).ToList();

            // Separate Requests (10 cards) from the other 4 categories
            List<TCGEnemyCardEntry> requestsList = validCards.Where(c => c.category.Equals("Requests", System.StringComparison.OrdinalIgnoreCase)).ToList();
            List<TCGEnemyCardEntry> otherCategoriesList = validCards.Where(c => !c.category.Equals("Requests", System.StringComparison.OrdinalIgnoreCase)).ToList();

            // Shuffle sublists
            ShuffleList(requestsList);
            ShuffleList(otherCategoriesList);

            List<TCGEnemyCardEntry> selectedEntries = new List<TCGEnemyCardEntry>();

            // Always add all 10 Requests situations
            selectedEntries.AddRange(requestsList);

            // Pull 10 situations randomly from the remaining 4 categories (Greetings, Gratitude, Identity, Responses)
            for (int i = 0; i < Mathf.Min(10, otherCategoriesList.Count); i++)
            {
                selectedEntries.Add(otherCategoriesList[i]);
            }

            totalRounds = selectedEntries.Count; // 20 rounds
            initialCardsLeft = totalRounds + 5;   // 25 cards total (5 mistakes allowed)

            // Map each selected entry to its corresponding PhraseEntry
            foreach (var cardEntry in selectedEntries)
            {
                PhraseEntry phrase = DatasetManager.Instance.GetPhraseById(cardEntry.phraseId);
                if (phrase != null)
                {
                    roundPool.Add(new TCGRoundData
                    {
                        enemyCard = cardEntry,
                        phrase = phrase
                    });
                }
                else
                {
                    Debug.LogWarning($"[TCGGameManager] Phrase {cardEntry.phraseId} not found in LuminangPhrases dataset!");
                }
            }

            // Final shuffle of the 20 rounds so Requests and other categories are mixed randomly throughout the match
            ShuffleList(roundPool);
        }

        private void SetupNextRound()
        {
            if (currentRoundIndex >= roundPool.Count)
            {
                ShowWinScreen();
                return;
            }

            isInteractionLocked = true;
            currentRoundData = roundPool[currentRoundIndex];

            // Reset slots and Played zone
            for (int i = 0; i < playerCardSlots.Length; i++)
            {
                playerCardSlots[i].localScale = playerCardOriginalScales[i];
                playerCardSlots[i].position = deckTransform.position;
                playerCardSlots[i].gameObject.SetActive(false);
                Image img = playerCardSlots[i].GetComponent<Image>();
                if (img != null) img.sprite = cardBackSprite;

                // Disable the word text component initially so card back shows cleanly
                TextMeshProUGUI tmp = playerCardSlots[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) tmp.gameObject.SetActive(false);
            }

            enemyPlayedSlot.localScale = enemyPlayedSlotOriginalScale;
            enemyPlayedSlot.position = deckTransform.position;
            enemyPlayedSlot.gameObject.SetActive(false);

            playerPlayedSlot.localScale = playerPlayedSlotOriginalScale;
            playerPlayedSlot.position = deckTransform.position;
            playerPlayedSlot.gameObject.SetActive(false);

            // Select correct word and 3 random decoy words
            roundOptions.Clear();
            roundOptions.Add(currentRoundData.phrase);

            List<PhraseEntry> allPhrases = DatasetManager.Instance.GetAllPhrases();
            List<PhraseEntry> decoyPool = allPhrases
                .Where(p => p.id != currentRoundData.phrase.id)
                .ToList();

            // Prefer decoys in the same category to increase challenge
            List<PhraseEntry> sameCategoryDecoys = decoyPool.Where(p => p.category == currentRoundData.phrase.category).ToList();
            ShuffleList(sameCategoryDecoys);

            for (int i = 0; i < Mathf.Min(3, sameCategoryDecoys.Count); i++)
            {
                roundOptions.Add(sameCategoryDecoys[i]);
            }

            // Fill remaining if category didn't have enough decoys
            if (roundOptions.Count < 4)
            {
                List<PhraseEntry> crossCategoryDecoys = decoyPool.Where(p => p.category != currentRoundData.phrase.category).ToList();
                ShuffleList(crossCategoryDecoys);
                while (roundOptions.Count < 4 && crossCategoryDecoys.Count > 0)
                {
                    roundOptions.Add(crossCategoryDecoys[0]);
                    crossCategoryDecoys.RemoveAt(0);
                }
            }

            // Shuffle the 4 choices
            var shuffledIndices = Enumerable.Range(0, roundOptions.Count).OrderBy(x => UnityEngine.Random.value).ToList();
            List<PhraseEntry> shuffledOptions = new List<PhraseEntry>();
            for (int i = 0; i < shuffledIndices.Count; i++)
            {
                shuffledOptions.Add(roundOptions[shuffledIndices[i]]);
                if (shuffledOptions[i].id == currentRoundData.phrase.id)
                {
                    correctOptionIndex = i;
                }
            }
            roundOptions = shuffledOptions;

            // Load situation image for the enemy card
            StartCoroutine(LoadCardSprite(currentRoundData.enemyCard.spritePath, (sprite) =>
            {
                currentEnemySituationSprite = sprite;
                Image enemyImg = enemyPlayedSlot.GetComponent<Image>();
                if (enemyImg != null && sprite != null)
                {
                    enemyImg.sprite = sprite;
                }
                
                // Start dealing animation
                StartCoroutine(DealRoundSequence());
            }));
        }

        private IEnumerator DealRoundSequence()
        {
            UpdateHUD();

            // Step 1: Deal Player's 4 Cards sequentially with a smooth, slower arc throw
            for (int i = 0; i < playerCardSlots.Length; i++)
            {
                playerCardSlots[i].gameObject.SetActive(true);
                playerCardSlots[i].position = deckTransform.position; // Ensure it starts exactly on the deck
                
                // Setup the word/phrase on the card with auto-sizing and dynamic underline
                TextMeshProUGUI tmp = playerCardSlots[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    FormatCardTextAndUnderline(tmp, roundOptions[i].GetPhrase(FishingGameConfig.TargetLanguage), 14f, 32f);
                    tmp.gameObject.SetActive(false);
                }

                // Trigger Throw & Flip with smooth 0.75s duration and upward arc
                int slotIndex = i;
                
                playerCardSlots[slotIndex].anchoredPosition = playerCardOriginalAnchoredPositions[slotIndex];
                Vector3 targetWorldPos = playerCardSlots[slotIndex].position;
                playerCardSlots[slotIndex].position = deckTransform.position;

                // Play card slide SFX as each card is thrown to its hand slot
                PlaySFX(cardSlideSFX);

                StartCoroutine(TCGCardAnimator.Instance.ThrowAndFlipCard(
                    playerCardSlots[slotIndex],
                    deckTransform.position,
                    targetWorldPos,
                    0.75f,
                    cardFrontSprite,
                    playerCardOriginalScales[slotIndex],
                    35f,
                    () => {
                        // Activate text child once flipped face up
                        TextMeshProUGUI cardText = playerCardSlots[slotIndex].GetComponentInChildren<TextMeshProUGUI>(true);
                        if (cardText != null) cardText.gameObject.SetActive(true);
                    }
                ));

                // Stagger delay between cards for sequential dealing
                yield return new WaitForSeconds(0.18f);
            }

            // Wait for player cards to finish settling
            yield return new WaitForSeconds(0.6f);

            // Step 2: Deal Enemy Card from a random top EnemyCard (1 to 4), different every round!
            Vector3 enemySourcePos = GetRandomEnemyCardSourceWorldPos();
            enemyPlayedSlot.gameObject.SetActive(true);
            
            enemyPlayedSlot.anchoredPosition = enemyPlayedSlotOriginalAnchoredPos;
            Vector3 targetEnemyWorldPos = enemyPlayedSlot.position;
            enemyPlayedSlot.position = enemySourcePos;

            SetupEnemyPlayedCardVisuals(false); // Start face down

            Vector3 enemyRotation = rotateEnemyPlayedCardUpsideDown ? new Vector3(0, 0, 180f) : Vector3.zero;

            // Play card slide SFX for enemy card throw
            PlaySFX(cardSlideSFX);

            yield return TCGCardAnimator.Instance.ThrowAndFlipCard(
                enemyPlayedSlot,
                enemySourcePos,
                targetEnemyWorldPos,
                0.85f,
                cardFrontSprite,
                enemyPlayedSlotOriginalScale,
                35f,
                () => {
                    // Reveal inner situation image and text on flip
                    SetupEnemyPlayedCardVisuals(true);
                },
                enemyRotation
            );

            // Unlock interactions
            isInteractionLocked = false;
        }

        private Vector3 GetRandomEnemyCardSourceWorldPos()
        {
            if (enemyCardSlots != null && enemyCardSlots.Length > 0)
            {
                int slotIndex = UnityEngine.Random.Range(0, enemyCardSlots.Length);
                if (enemyCardSlots.Length > 1 && slotIndex == lastEnemyCardSlotIndex)
                {
                    slotIndex = (slotIndex + 1) % enemyCardSlots.Length;
                }
                lastEnemyCardSlotIndex = slotIndex;
                return enemyCardSlots[slotIndex].position;
            }
            return deckTransform.position;
        }

        private void SetupEnemyPlayedCardVisuals(bool faceUp)
        {
            if (enemyPlayedSlot == null) return;

            // Main card frame (front when face up, back when face down)
            Image mainCardImg = enemyPlayedSlot.GetComponent<Image>();
            if (mainCardImg != null)
            {
                mainCardImg.sprite = faceUp ? cardFrontSprite : cardBackSprite;
            }

            // Update all inner images (to catch the SituationImage wherever it is nested)
            Image[] allImgs = enemyPlayedSlot.GetComponentsInChildren<Image>(true);
            foreach (var img in allImgs)
            {
                if (img.gameObject != enemyPlayedSlot.gameObject && 
                   (img.gameObject.name.Contains("Situation") || img.gameObject.name.Contains("Image") || enemyPlayedInnerImage == img))
                {
                    if (currentEnemySituationSprite != null)
                    {
                        img.sprite = currentEnemySituationSprite;
                        img.color = Color.white;
                    }
                    img.gameObject.SetActive(faceUp);
                }
            }

            string textToDisplay = currentRoundData != null ? currentRoundData.enemyCard.situationText : "";

            // Update all child TMP texts with auto-sizing so long phrases shrink to fit
            TextMeshProUGUI[] allTMPs = enemyPlayedSlot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var sitTMP in allTMPs)
            {
                sitTMP.enableAutoSizing = true;
                sitTMP.fontSizeMin = 8f;
                sitTMP.fontSizeMax = 18f;
                sitTMP.textWrappingMode = TextWrappingModes.Normal;
                sitTMP.overflowMode = TextOverflowModes.Ellipsis;
                sitTMP.text = textToDisplay;
                sitTMP.gameObject.SetActive(faceUp);
            }

            // Update all legacy child texts
            UnityEngine.UI.Text[] allLegacyTexts = enemyPlayedSlot.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var legacyText in allLegacyTexts)
            {
                legacyText.text = textToDisplay;
                legacyText.gameObject.SetActive(faceUp);
            }
        }

        public void OnPlayerCardClicked(int slotIndex)
        {
            if (isInteractionLocked) return;
            PlaySFX(buttonClickSFX);

            currentlySelectedSlotIndex = slotIndex;

            // Setup preview
            if (playerViewWordText != null)
            {
                FormatCardTextAndUnderline(playerViewWordText, roundOptions[slotIndex].GetPhrase(FishingGameConfig.TargetLanguage), 20f, 48f);
            }

            // Open panels
            if (blackOverlay != null) blackOverlay.SetActive(true);
            if (viewCardRoot != null) viewCardRoot.SetActive(true);
            if (playerViewCardPanel != null) playerViewCardPanel.SetActive(true);
            if (playerViewEnemyCardPanel != null) playerViewEnemyCardPanel.SetActive(false);

            // Position underline based on word wrap height
            StartCoroutine(UpdateUnderlinePosition());
        }

        private void FormatCardTextAndUnderline(TextMeshProUGUI tmp, string text, float minFont = 14f, float maxFont = 32f)
        {
            if (tmp == null) return;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minFont;
            tmp.fontSizeMax = maxFont;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.text = text;
            tmp.ForceMeshUpdate();

            // Find child underline
            Transform underline = tmp.transform.Find("Underline") ?? tmp.transform.Find("Image");
            if (underline == null)
            {
                for (int i = 0; i < tmp.transform.childCount; i++)
                {
                    Transform child = tmp.transform.GetChild(i);
                    if (child.GetComponent<Image>() != null)
                    {
                        underline = child;
                        break;
                    }
                }
            }

            if (underline != null)
            {
                RectTransform uRect = underline as RectTransform;
                if (uRect != null)
                {
                    float textHeight = tmp.preferredHeight;
                    uRect.anchoredPosition = new Vector2(uRect.anchoredPosition.x, -(textHeight * 0.5f + 14f));
                }
            }
        }

        private IEnumerator UpdateUnderlinePosition()
        {
            yield return new WaitForEndOfFrame();
            if (playerViewWordText != null && playerViewUnderline != null)
            {
                // Force layout update to read correct preferredHeight
                playerViewWordText.ForceMeshUpdate();
                float textHeight = playerViewWordText.preferredHeight;
                playerViewUnderline.anchoredPosition = new Vector2(0f, -(textHeight * 0.5f + underlinePadding));
            }
        }

        public void OnEnemyPlayedCardClicked()
        {
            if (isInteractionLocked) return;
            PlaySFX(buttonClickSFX);

            // Setup enemy card preview (PlayerViewEnemyCard) with auto-sizing
            if (enemyViewSituationText != null)
            {
                enemyViewSituationText.enableAutoSizing = true;
                enemyViewSituationText.fontSizeMin = 8f;
                enemyViewSituationText.fontSizeMax = 24f;
                enemyViewSituationText.textWrappingMode = TextWrappingModes.Normal;
                enemyViewSituationText.overflowMode = TextOverflowModes.Ellipsis;
                enemyViewSituationText.text = currentRoundData.enemyCard.situationText;
            }
            if (enemyViewImage != null)
            {
                enemyViewImage.sprite = currentEnemySituationSprite != null ? currentEnemySituationSprite : enemyPlayedSlot.GetComponent<Image>().sprite;
            }

            // Open panels
            if (blackOverlay != null) blackOverlay.SetActive(true);
            if (viewCardRoot != null) viewCardRoot.SetActive(true);
            if (playerViewEnemyCardPanel != null) playerViewEnemyCardPanel.SetActive(true);
            if (playerViewCardPanel != null) playerViewCardPanel.SetActive(false);
        }

        public void OnCloseCardView()
        {
            PlaySFX(buttonClickSFX);
            ClosePreviewPanels();
        }

        private void ClosePreviewPanels()
        {
            if (playerViewCardPanel != null) playerViewCardPanel.SetActive(false);
            if (playerViewEnemyCardPanel != null) playerViewEnemyCardPanel.SetActive(false);
            if (viewCardRoot != null) viewCardRoot.SetActive(false);
            if (blackOverlay != null) blackOverlay.SetActive(false);
        }

        public void OnChooseCardPressed()
        {
            if (isInteractionLocked || currentlySelectedSlotIndex == -1) return;
            isInteractionLocked = true;

            PlaySFX(buttonClickSFX);
            ClosePreviewPanels();

            // Disable original slot Card visual
            RectTransform cardSlot = playerCardSlots[currentlySelectedSlotIndex];
            cardSlot.gameObject.SetActive(false);

            // Setup Played card position and word
            playerPlayedSlot.gameObject.SetActive(true);
            Image playedImg = playerPlayedSlot.GetComponent<Image>();
            if (playedImg != null) playedImg.sprite = cardFrontSprite;

            TextMeshProUGUI playedTmp = playerPlayedSlot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (playedTmp != null)
            {
                FormatCardTextAndUnderline(playedTmp, roundOptions[currentlySelectedSlotIndex].GetPhrase(FishingGameConfig.TargetLanguage), 16f, 38f);
                playedTmp.gameObject.SetActive(true);
            }

            // Animate card to Played position
            playerPlayedSlot.anchoredPosition = playerPlayedSlotOriginalAnchoredPos;
            Vector3 targetPlayedWorldPos = playerPlayedSlot.position;
            playerPlayedSlot.position = cardSlot.position;

            // Play card slide SFX at the start of the throw
            PlaySFX(cardSlideSFX);

            StartCoroutine(TCGCardAnimator.Instance.MoveCard(
                playerPlayedSlot,
                cardSlot.position,
                targetPlayedWorldPos,
                0.5f,
                () => StartCoroutine(EvaluateWithDelay(1.5f))
            ));
        }

        private IEnumerator EvaluateWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EvaluateSelectedCard();
        }

        private void EvaluateSelectedCard()
        {
            cardsLeft--;
            UpdateHUD();

            bool isCorrect = (currentlySelectedSlotIndex == correctOptionIndex);

            if (isCorrect)
            {
                StartCoroutine(CorrectAnswerSequence());
            }
            else
            {
                StartCoroutine(WrongAnswerSequence());
            }
        }

        private IEnumerator CorrectAnswerSequence()
        {
            PlaySFX(correctSFX);

            // Activate parent group if disabled
            if (correctBanner != null && correctBanner.transform.parent != null)
            {
                correctBanner.transform.parent.gameObject.SetActive(true);
            }

            // Show Correct Banner instantly
            if (correctBanner != null) correctBanner.SetActive(true);

            // Glow Green
            yield return TCGCardAnimator.Instance.GlowOutline(playerPlayedSlot, Color.green, 1.0f);

            yield return new WaitForSeconds(bannerDuration - 1.0f > 0 ? bannerDuration - 1.0f : 0);
            
            if (correctBanner != null) 
            {
                correctBanner.SetActive(false);
                if (correctBanner.transform.parent != null)
                {
                    correctBanner.transform.parent.gameObject.SetActive(false);
                }
            }

            // Start STT mode
            if (TCGSTTManager.Instance != null)
            {
                TCGSTTManager.Instance.StartSTT(
                    currentRoundData.phrase,
                    OnSTTSuccess,
                    OnSTTFail
                );
            }
            else
            {
                Debug.LogWarning("[TCGGameManager] TCGSTTManager instance not found! Skipping STT step.");
                OnSTTSuccess();
            }
        }

        private IEnumerator WrongAnswerSequence()
        {
            PlaySFX(wrongSFX);

            // Activate parent group if disabled
            if (wrongBanner != null && wrongBanner.transform.parent != null)
            {
                wrongBanner.transform.parent.gameObject.SetActive(true);
            }

            // Show Wrong Banner instantly
            if (wrongBanner != null) wrongBanner.SetActive(true);

            // Screen vibrate/shake target (optional - mobile)
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif

            // Shake Played Card & Glow Red
            StartCoroutine(TCGCardAnimator.Instance.GlowOutline(playerPlayedSlot, Color.red, 1.0f));
            yield return TCGCardAnimator.Instance.ShakeCard(playerPlayedSlot, 0.4f, 0.8f); // Reduced intensity to 0.8f

            yield return new WaitForSeconds(bannerDuration - 0.4f > 0 ? bannerDuration - 0.4f : 0);
            
            if (wrongBanner != null) 
            {
                wrongBanner.SetActive(false);
                if (wrongBanner.transform.parent != null)
                {
                    wrongBanner.transform.parent.gameObject.SetActive(false);
                }
            }

            if (cardsLeft <= 0)
            {
                ShowLoseScreen();
                yield break;
            }

            // Reshuffle/replace the enemy card by swapping current round data with a future one
            if (currentRoundIndex < roundPool.Count - 1)
            {
                int swapIdx = UnityEngine.Random.Range(currentRoundIndex + 1, roundPool.Count);
                TCGRoundData temp = roundPool[currentRoundIndex];
                roundPool[currentRoundIndex] = roundPool[swapIdx];
                roundPool[swapIdx] = temp;
            }

            // Smooth Outro for table cards
            Coroutine lastPopOut = null;
            if (playerPlayedSlot.gameObject.activeSelf)
                lastPopOut = StartCoroutine(TCGCardAnimator.Instance.PopOut(playerPlayedSlot, 0.3f));
            
            if (enemyPlayedSlot.gameObject.activeSelf)
                lastPopOut = StartCoroutine(TCGCardAnimator.Instance.PopOut(enemyPlayedSlot, 0.3f));

            for (int i = 0; i < playerCardSlots.Length; i++)
            {
                if (playerCardSlots[i].gameObject.activeSelf)
                    lastPopOut = StartCoroutine(TCGCardAnimator.Instance.PopOut(playerCardSlots[i], 0.3f));
            }

            if (lastPopOut != null) yield return lastPopOut;

            // Reset selected slot and restart the same round number
            currentlySelectedSlotIndex = -1;
            SetupNextRound();
        }

        private void OnSTTSuccess()
        {
            currentRoundIndex++;
            if (currentRoundIndex >= roundPool.Count)
            {
                ShowWinScreen();
            }
            else
            {
                StartCoroutine(STTSuccessNextRoundSequence());
            }
        }

        private IEnumerator STTSuccessNextRoundSequence()
        {
            Coroutine lastPopOut = null;
            if (playerPlayedSlot.gameObject.activeSelf)
                lastPopOut = StartCoroutine(TCGCardAnimator.Instance.PopOut(playerPlayedSlot, 0.3f));
            
            if (enemyPlayedSlot.gameObject.activeSelf)
                lastPopOut = StartCoroutine(TCGCardAnimator.Instance.PopOut(enemyPlayedSlot, 0.3f));

            for (int i = 0; i < playerCardSlots.Length; i++)
            {
                if (playerCardSlots[i].gameObject.activeSelf)
                    lastPopOut = StartCoroutine(TCGCardAnimator.Instance.PopOut(playerCardSlots[i], 0.3f));
            }

            if (lastPopOut != null) yield return lastPopOut;

            SetupNextRound();
        }

        private void OnSTTFail()
        {
            // Failed STT 3 times. Player loses 1 card.
            cardsLeft--;
            UpdateHUD();

            if (cardsLeft <= 0)
            {
                ShowLoseScreen();
                return;
            }

            // Throw wrong card back to its hand slot
            StartCoroutine(STTFailResetSequence());
        }

        private IEnumerator STTFailResetSequence()
        {
            RectTransform cardSlot = playerCardSlots[currentlySelectedSlotIndex];
            cardSlot.anchoredPosition = playerCardOriginalAnchoredPositions[currentlySelectedSlotIndex];
            Vector3 targetHandWorldPos = cardSlot.position;

            cardSlot.position = playerPlayedSlot.position;
            cardSlot.gameObject.SetActive(true);
            playerPlayedSlot.gameObject.SetActive(false);

            yield return TCGCardAnimator.Instance.MoveCard(
                cardSlot,
                cardSlot.position,
                targetHandWorldPos,
                0.45f
            );

            // Shuffle a new enemy card for this exact same round (without advancing round index)
            List<TCGRoundData> remainingChoices = roundPool.Skip(currentRoundIndex).ToList();
            if (remainingChoices.Count > 1)
            {
                // Swap the current round item with a random upcoming one to simulate shuffling
                int swapTarget = UnityEngine.Random.Range(1, remainingChoices.Count);
                TCGRoundData temp = roundPool[currentRoundIndex];
                roundPool[currentRoundIndex] = roundPool[currentRoundIndex + swapTarget];
                roundPool[currentRoundIndex + swapTarget] = temp;
            }

            // Reload cards and redraw hand
            SetupNextRound();
        }

        private void ShowWinScreen()
        {
            PlaySFX(winSFX);

            if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
            if (winPanel != null) winPanel.SetActive(true);
            if (losePanel != null) losePanel.SetActive(false);

            // Calculate reward:
            // Remaining CardsLeft mirrors Remaining Baits from the fishing game
            int stars = 1;
            int coinsEarned = 10;

            if (cardsLeft >= 5) { stars = 5; coinsEarned = 50; }
            else if (cardsLeft == 4) { stars = 4; coinsEarned = 40; }
            else if (cardsLeft == 3) { stars = 3; coinsEarned = 30; }
            else if (cardsLeft == 2) { stars = 2; coinsEarned = 20; }
            else { stars = 1; coinsEarned = 10; }

            // Auto-recover winStars if unassigned in Inspector
            if (winStars == null || winStars.Length == 0 || winStars[0] == null)
            {
                if (winPanel != null)
                {
                    var foundList = new List<Image>();
                    for (int s = 1; s <= 5; s++)
                    {
                        foreach (Transform t in winPanel.GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name.Equals($"Star{s}", System.StringComparison.OrdinalIgnoreCase) && t.TryGetComponent<Image>(out var img))
                            {
                                foundList.Add(img);
                                break;
                            }
                        }
                    }
                    if (foundList.Count > 0) winStars = foundList.ToArray();
                }
            }

            // Auto-recover star sprites if unassigned in Inspector
            if (activeStarSprite == null || inactiveStarSprite == null)
            {
                Sprite[] allSprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var sp in allSprites)
                {
                    if (activeStarSprite == null && sp.name == "star_active") activeStarSprite = sp;
                    if (inactiveStarSprite == null && sp.name == "star_inactive") inactiveStarSprite = sp;
                }
            }

            // Assign Star UI elements
            if (winStars != null)
            {
                for (int i = 0; i < winStars.Length; i++)
                {
                    if (winStars[i] != null)
                    {
                        winStars[i].sprite = (i < stars) ? activeStarSprite : inactiveStarSprite;
                        winStars[i].gameObject.SetActive(true);
                    }
                }
            }

            if (winCoinsText != null) winCoinsText.text = $"+{coinsEarned}";

            // Save coins and minigame win state
            int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
            PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
            PlayerPrefs.SetInt("TCGMinigameWon", 1);
            PlayerPrefs.SetInt("FishingMinigameWon", 1);
            PlayerPrefs.SetInt("MinigameWon", 1);
            PlayerPrefs.Save();

            if (winPanel != null)
            {
                StartCoroutine(TCGCardAnimator.Instance.PopIn(winPanel.transform));
            }
        }

        private void ShowLoseScreen()
        {
            PlaySFX(loseSFX);

            if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(true);

            // Consolation prize (same as fishing game)
            int coinsEarned = 2;
            if (loseCoinsText != null) loseCoinsText.text = $"+{coinsEarned}";

            int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
            PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
            PlayerPrefs.SetInt("TCGMinigameWon", 0);
            PlayerPrefs.SetInt("FishingMinigameWon", 0);
            PlayerPrefs.SetInt("MinigameWon", 0);
            PlayerPrefs.Save();

            if (losePanel != null)
            {
                StartCoroutine(TCGCardAnimator.Instance.PopIn(losePanel.transform));
            }
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene");
            SceneLoader.ResetLoadingFlag();
            SceneLoader.targetSceneForLoading = prevScene;
            SceneLoader.keepBackgroundPersistent = false;
            SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
        }

        private void UpdateHUD()
        {
            if (roundNumberText != null) roundNumberText.text = (currentRoundIndex + 1).ToString();
            if (roundTotalText != null) roundTotalText.text = "/" + totalRounds;
            if (cardsLeftNumberText != null) cardsLeftNumberText.text = cardsLeft.ToString();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // Shared Fisher-Yates shuffle
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        // Helper to load sprite directly and safely from Resources or disk
        private IEnumerator LoadCardSprite(string spritePath, Action<Sprite> onComplete)
        {
            if (string.IsNullOrEmpty(spritePath))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            string cleanPath = spritePath.Replace(".png", "").Replace(".jpg", "");
            string fileName = Path.GetFileName(cleanPath);

            #if UNITY_EDITOR
            // Windows-normalized absolute paths to search
            string[] possibleDiskPaths = new string[]
            {
                Path.GetFullPath(Path.Combine(Application.dataPath, spritePath + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, spritePath)),
                Path.GetFullPath(Path.Combine(Application.dataPath, cleanPath + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Sprites", "Mini Games", "TCG", "Greetings_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Sprites", "Mini Games", "TCG", "Gratitude_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Sprites", "Mini Games", "TCG", "Responses_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Sprites", "Mini Games", "TCG", "Identity_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Sprites", "Mini Games", "TCG", "Request_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Resources", "Sprites", "Mini Games", "TCG", "Greetings_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Resources", "Sprites", "Mini Games", "TCG", "Gratitude_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Resources", "Sprites", "Mini Games", "TCG", "Responses_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Resources", "Sprites", "Mini Games", "TCG", "Identity_Images", fileName + ".png")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "Resources", "Sprites", "Mini Games", "TCG", "Request_Images", fileName + ".png"))
            };

            foreach (string p in possibleDiskPaths)
            {
                if (File.Exists(p))
                {
                    byte[] fileData = File.ReadAllBytes(p);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(fileData))
                    {
                        Sprite spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        onComplete?.Invoke(spr);
                        yield break;
                    }
                }
            }
            #endif

            // Direct Resources load fallback
            string resPath = cleanPath;
            if (resPath.StartsWith("Assets/Resources/")) resPath = resPath.Substring("Assets/Resources/".Length);
            else if (resPath.StartsWith("Resources/")) resPath = resPath.Substring("Resources/".Length);

            ResourceRequest resourceReq = Resources.LoadAsync<Sprite>(resPath);
            yield return resourceReq;
            if (resourceReq.asset != null)
            {
                onComplete?.Invoke((Sprite)resourceReq.asset);
                yield break;
            }

            Debug.LogWarning($"[TCGGameManager] Could not load sprite at: {spritePath}");
            onComplete?.Invoke(null);
        }
    }
}
