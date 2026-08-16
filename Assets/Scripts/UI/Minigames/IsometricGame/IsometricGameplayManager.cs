using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Luminang.UI.Minigames.IsometricGame
{
    public class IsometricGameplayManager : MonoBehaviour
    {
        public static IsometricGameplayManager Instance { get; private set; }

        [System.Serializable]
        public class ScenarioWaypointSetup
        {
            [Tooltip("Friendly label for this scenario (e.g. '1: South Gate Go Straight')")]
            public string scenarioName;
            [Tooltip("Optional: Visual walk path with corners/curves. If assigned, characters follow this path in line formation.")]
            public IsometricWalkPath walkPath;
            [Tooltip("Custom walk duration in seconds. Set to 0 to use the global default (characterWalkDuration = 2s).")]
            public float customWalkDuration = 0f;
            [Tooltip("Delay in seconds before Rodrick follows behind the Player in line formation (default 0.25s).")]
            public float followerDelay = 0.25f;
            [Tooltip("Where Rodrick should walk to on SUCCESS.")]
            public RectTransform successWaypoint;
            [Tooltip("Where the Player should walk to on SUCCESS (if they follow).")]
            public RectTransform playerSuccessWaypoint;
            [Tooltip("Optional: Where the active NPC (e.g. Neneng) should walk to on SUCCESS alongside Rodrick.")]
            public RectTransform npcSuccessWaypoint;
            [Tooltip("Optional: Where Rodrick walks on MISTAKE (e.g., to the fish stall).")]
            public RectTransform mistakeWaypoint;
            [Tooltip("Optional: Where Rodrick walks BACK to after the post-success dialogue exchange.")]
            public RectTransform returnWaypoint;
            [Tooltip("Optional: Where the Player walks BACK to after the post-success dialogue exchange.")]
            public RectTransform playerReturnWaypoint;
            [Tooltip("Optional: Where the active NPC walks BACK to after the post-success dialogue exchange.")]
            public RectTransform npcReturnWaypoint;
        }

        [Header("Character References")]
        public IsometricCharacter player;
        public IsometricCharacter rodrick;
        public IsometricCameraFollow cameraFollow;

        [Header("Scenario Waypoints Setup")]
        [Tooltip("Must match Scenarios 1-20 in order.")]
        public List<ScenarioWaypointSetup> scenarioWaypoints = new List<ScenarioWaypointSetup>();

        [Header("Character Dialogue Bubbles")]
        [Tooltip("Rodrick's duplicated overhead chat bubble controller.")]
        public IsometricIntroController rodrickDialogueBubble;
        
        [Tooltip("Lola Nida's speech bubble controller.")]
        public IsometricIntroController lolaNidaDialogueBubble;

        [Header("UI - Choices Panel")]
        public GameObject choicesPanel;
        public List<Button> choiceButtons;
        public List<TextMeshProUGUI> choiceTexts;

        [Header("UI - Overlays")]
        [Tooltip("The semi-transparent black overlay to dim background during dialogue/STT.")]
        public GameObject dialogueBlackOverlay;
        [Tooltip("The WoodHeader banner at the top of the HUD.")]
        public GameObject woodHeader;
        [Tooltip("The MenuButton at the top right of the HUD.")]
        public GameObject menuButton;

        [Header("UI - Patience Bar")]
        public GameObject patienceBarGroup;
        public List<Image> patienceHeartBars;

        [Header("UI - Win / Lose Result Panel")]
        public GameObject winOrLoseGroup;
        public GameObject winPanel;
        public GameObject losePanel;
        public Image[] winStars;
        public Sprite activeStarSprite;
        public Sprite inactiveStarSprite;
        public TextMeshProUGUI winCoinsText;
        public TextMeshProUGUI loseCoinsText;

        [Header("Result Audio")]
        public AudioClip winPanelSFX;
        public AudioClip losePanelSFX;

        [Header("UI - Lola Nida's Call System")]
        public GameObject phoneCallGroup;
        public RectTransform phoneRectTransform;
        public Image phoneImage;
        public Sprite phoneRingingSprite;
        public Sprite phoneAnsweredSprite;
        public GameObject lolaNidaPortraitGroup; // Parent group to toggle active
        public Image lolaNidaPortraitImage; // The Image component to swap sprites on
        public List<Sprite> lolaNidaPortraits; // List of her 5 portraits

        [Header("UI - How To Play Popup")]
        public GameObject howToPlayGroup;

        [System.Serializable]
        public class DialogueLine
        {
            [Tooltip("True if the NPC is speaking. False if Rodrick is speaking.")]
            public bool isNPC;
            [Tooltip("Text to show in English (e.g. for Rodrick).")]
            public string textEnglish;
            [Tooltip("Text to show in Cebuano.")]
            public string textCebuano;
            [Tooltip("Text to show in Ilokano.")]
            public string textIlokano;
            [Tooltip("Optional: if set, plays this animation on the NPC's animator BEFORE this line is shown. " +
                     "Use this to change Ronnie from Sleeping → Awake mid-conversation.")]
            public string animationToPlay;
            [Tooltip("Optional: if true, immediately inflates the next NPC in the sequences list during this dialogue line.")]
            public bool triggerInflateNPCNext;
        }

        [System.Serializable]
        public class NPCSequenceSetup
        {
            [Tooltip("Scenario Index (1-21) where this NPC will call out.")]
            public int scenarioIndex;
            [Tooltip("The parent GameObject of the NPC to show/inflate.")]
            public GameObject npcParent;
            [Tooltip("The animator component on the NPC sprite.")]
            public IsometricSpriteAnimator npcAnimator;
            [Tooltip("The dialogue speech bubble controller for this NPC.")]
            public IsometricIntroController npcDialogueBubble;
            [Tooltip("Animation state to play when calling out.")]
            public string callAnimationName = "Wave";
            [Tooltip("Dialogue text to display in Cebuano when calling out.")]
            public string dialogueCebuano;
            [Tooltip("Dialogue text to display in Ilokano when calling out.")]
            public string dialogueIlokano;

            [Header("Behaviour Flags")]
            [Tooltip("If TRUE: PlayCurrentScenario skips ALL NPC handling (no camera, no activate, no callout). " +
                     "Use this for NPCs whose appearance is entirely managed by the previous scenario's HandleCorrectChoice " +
                     "(e.g. Ronnie who is already inflated asleep and has no callout).")]
            public bool managedExternally;

            [Tooltip("If TRUE: PlayCurrentScenario does SetActive + camera focus but skips the callout animation+bubble. " +
                     "Use this for NPCs who already appeared mid-conversation (e.g. Neneng who popped up during Ronnie's last line).")]
            public bool skipCalloutDialogue;

            [Tooltip("If TRUE: the NPC will NOT be shrunk and disabled at the end of their scenario. " +
                     "Use this if the NPC needs to stay visible and walk alongside the player in subsequent scenarios.")]
            public bool keepActiveAfterSuccess;

            [Header("Success Conversation")]
            [Tooltip("Optional list of lines spoken between the NPC and Rodrick after reaching the success waypoint.")]
            public List<DialogueLine> successConversation = new List<DialogueLine>();

            [Tooltip("If true, Rodrick and Player walk to success waypoints BEFORE the conversation plays (Aling Riza style). " +
                     "If false, conversation plays first, THEN they walk (Padre Mario style).")]
            public bool walkBeforeConversation = true;

            [Header("Camera Override (during success conversation)")]
            [Tooltip("Zoom level to use while this NPC's success conversation plays. " +
                     "0 or negative = no override (keep the camera's inspector zoom). " +
                     "Use a value lower than the default zoomLevel to zoom out (e.g. 0.9).")]
            public float conversationZoom = 0f;

            [Tooltip("Extra canvas-space Y added to the pan target while this NPC's conversation plays. " +
                     "Positive = world shifts UP = camera looks more toward the south/bottom of the scene. " +
                     "Use this when the NPC is placed high up and their dialogue bubble gets clipped.")]
            public float conversationYOffset = 0f;

            [Tooltip("NPC pull weight to use during this conversation (0 = ignore NPC, 1 = lock onto NPC). " +
                     "Increase slightly when the NPC is far from Player/Rodrick so they all stay in frame.")]
            [Range(0f, 1f)]
            public float conversationNpcPull = 0.35f;

            [HideInInspector]
            public Vector3 originalScale = Vector3.one;
        }

        [Header("NPC Sequences Setup")]
        public List<NPCSequenceSetup> npcSequences = new List<NPCSequenceSetup>();
        
        [Header("UI - Errand HUD")]
        public Button hudErrandPaperButton;
        public TextMeshProUGUI errandPaperTitleText;
        public TextMeshProUGUI errandPaperPhraseText;

        [Header("UI - Zoomed Errand Paper Popup")]
        public GameObject zoomedErrandPaperPanel;
        public GameObject tapToCloseText;
        public TextMeshProUGUI zoomedErrandTitleText;
        public TextMeshProUGUI zoomedErrandPhraseText;

        [Header("Audio Settings")]
        public AudioSource audioSource;
        public AudioClip phoneRingingClip;
        public AudioClip phonePickUpClip;
        public AudioClip bumpClip;
        public AudioClip buttonClickClip;
        public AudioClip correctChoiceClip;
        public AudioClip wrongChoiceClip;
        public AudioClip errandPaperOpenClip;
        public AudioClip errandPaperCloseClip;

        [Header("Global Speech Bubble Audio (Applies to All Bubbles & NPCs)")]
        [Tooltip("Sound played when any speech bubble pops up (Rodrick, Lola Nida, All NPCs).")]
        public AudioClip bubblePopClip;
        [Tooltip("Typewriter ticking sound played as letters appear in any bubble.")]
        public AudioClip bubbleTypingClip;
        [Tooltip("Sound played when tapping to advance text.")]
        public AudioClip bubbleTapAdvanceClip;

        [Header("UI - Particle Effects")]
        [Tooltip("Assign a tiny star/sparkle sprite here for the edge sparkle effect.")]
        public Sprite sparkleSprite;

        [Header("Timing Settings")]
        public float dialogueAutoHoldTime = 2f;
        public float characterWalkDuration = 2f;

        // ── Scene 22 – Parade Dancer March ──────────────────────────────

        [System.Serializable]
        public class DancerMarchSetup
        {
            [Tooltip("The dancer parent GameObject (e.g. Dancers, Dancers1, Dancers2).")]
            public GameObject dancerParent;
            [Tooltip("The IsometricSpriteAnimator on this dancer group for Walk/Idle.")]
            public IsometricSpriteAnimator dancerAnimator;
            [Tooltip("OPTIONAL: Waypoint where this dancer group moves to during initial formation (Step 1). " +
                     "Use this to position dancers closer to Rodrick and Player before the march begins.")]
            public RectTransform formationWaypoint;
            [Tooltip("OPTIONAL: Waypoint where this dancer group ends up at the end of the march (Step 2). " +
                     "If assigned, the dancer moves directly to this waypoint. If empty, moves by Rodrick's march delta.")]
            public RectTransform stopWaypoint;
        }

        [Header("Scene 22 – Parade March")]
        [Tooltip("All dancer groups that march with Rodrick and the Player. " +
                 "Assign optional formation & stop waypoints to control exact positions/spacing per dancer group in Inspector.")]
        public List<DancerMarchSetup> paradeDancers = new List<DancerMarchSetup>();

        [Tooltip("Where Rodrick stops at the END of the parade march.")]
        public RectTransform paradeMarchRodrickStop;

        [Tooltip("Where the Player stops at the END of the parade march.")]
        public RectTransform paradeMarchPlayerStop;

        [Tooltip("How long the upward march takes in seconds. Slower = more cinematic.")]
        public float paradeMarchDuration = 5f;

        [System.Serializable]
        public class ScenarioData
        {
            public int scenarioIndex;
            public string errandTitle;
            public string context;
            public string dialogue;
            public string phraseId;
            public List<string> choices;
            public int correctChoiceIndex;
            public bool useSTT;
            public string mistakeDialogue;
            public string sttFailDialogue;
            public string mistakeAnimation;
            public int lolaNidaPortraitIndex;
        }

        private List<PhraseEntry> _phraseDatabase = new List<PhraseEntry>();
        private List<ScenarioData> _scenarios = new List<ScenarioData>();
        private int _currentScenarioIndex = 0;
        private int _currentPatience = 5;
        private const int MaxPatience = 5;
        private bool _waitingForChoice = false;
        private bool _zoomedPaperOpen = false;
        private int _currentCorrectChoiceIndex = 0;
        private bool _gameplayStarted = false;

        private void Awake()
        {
            Instance = this;
            LoadScenariosDatabase();
            LoadPhrasesDatabase();
        }

        private void Start()
        {
            if (choicesPanel != null) choicesPanel.SetActive(false);
            if (phoneCallGroup != null) phoneCallGroup.SetActive(false);
            if (lolaNidaPortraitGroup != null) lolaNidaPortraitGroup.SetActive(false);
            if (rodrickDialogueBubble != null) rodrickDialogueBubble.gameObject.SetActive(false);
            if (lolaNidaDialogueBubble != null) lolaNidaDialogueBubble.gameObject.SetActive(false);
            if (zoomedErrandPaperPanel != null) zoomedErrandPaperPanel.SetActive(false);
            if (winOrLoseGroup != null) winOrLoseGroup.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            
            // Cache original scales and disable all NPCs in the list on start
            foreach (var npcSeq in npcSequences)
            {
                if (npcSeq != null && npcSeq.npcParent != null)
                {
                    npcSeq.originalScale = npcSeq.npcParent.transform.localScale;
                    npcSeq.npcParent.SetActive(false);
                }
                if (npcSeq != null && npcSeq.npcDialogueBubble != null)
                {
                    npcSeq.npcDialogueBubble.gameObject.SetActive(false);
                }
            }
            
            if (hudErrandPaperButton != null)
            {
                hudErrandPaperButton.onClick.AddListener(OnHUDErrandPaperClicked);
            }

            UpdatePatienceUI();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug key overrides
            #if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                // PageUp to skip forward
                if (keyboard.pageUpKey.wasPressedThisFrame)
                {
                    int nextIndex = Mathf.Min(_currentScenarioIndex + 1, _scenarios.Count - 1);
                    JumpToScenario(nextIndex);
                }
                // PageDown to skip backward
                if (keyboard.pageDownKey.wasPressedThisFrame)
                {
                    int prevIndex = Mathf.Max(_currentScenarioIndex - 1, 0);
                    JumpToScenario(prevIndex);
                }
                // Digit keys to jump to Scenario 1-9 directly
                if (keyboard.digit1Key.wasPressedThisFrame) JumpToScenario(0);
                if (keyboard.digit2Key.wasPressedThisFrame) JumpToScenario(1);
                if (keyboard.digit3Key.wasPressedThisFrame) JumpToScenario(2);
                if (keyboard.digit4Key.wasPressedThisFrame) JumpToScenario(3);
                if (keyboard.digit5Key.wasPressedThisFrame) JumpToScenario(4);
                if (keyboard.digit6Key.wasPressedThisFrame) JumpToScenario(5);
                if (keyboard.digit7Key.wasPressedThisFrame) JumpToScenario(6);
                if (keyboard.digit8Key.wasPressedThisFrame) JumpToScenario(7);
                if (keyboard.digit9Key.wasPressedThisFrame) JumpToScenario(8);
            }
            #endif

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                int nextIndex = Mathf.Min(_currentScenarioIndex + 1, _scenarios.Count - 1);
                JumpToScenario(nextIndex);
            }
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                int prevIndex = Mathf.Max(_currentScenarioIndex - 1, 0);
                JumpToScenario(prevIndex);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) JumpToScenario(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) JumpToScenario(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) JumpToScenario(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) JumpToScenario(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) JumpToScenario(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) JumpToScenario(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) JumpToScenario(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) JumpToScenario(7);
            if (Input.GetKeyDown(KeyCode.Alpha9)) JumpToScenario(8);
            #endif
#endif
        }

        private void LoadPhrasesDatabase()
        {
            // Try using DatasetManager if available, otherwise read directly from resources
            if (DatasetManager.Instance != null)
            {
                _phraseDatabase = DatasetManager.Instance.GetAllPhrases();
            }
            else
            {
                TextAsset phrasesAsset = Resources.Load<TextAsset>("LuminangPhrases");
                if (phrasesAsset != null)
                {
                    PhraseDataset dataset = JsonUtility.FromJson<PhraseDataset>(phrasesAsset.text);
                    _phraseDatabase = dataset.phrases;
                }
                else
                {
                    Debug.LogError("[IsometricGameplayManager] LuminangPhrases.json not found in Resources!");
                }
            }
        }

        private string GetSelectedLanguage()
        {
            if (!string.IsNullOrEmpty(FishingGameConfig.TargetLanguage))
            {
                return FishingGameConfig.TargetLanguage.ToLower();
            }
            return "ilokano"; // Default fallback
        }

        private string GetTranslatedPhrase(string phraseId)
        {
            string selectedLanguage = GetSelectedLanguage();
            
            // Find phrase entry in our loaded database
            PhraseEntry entry = _phraseDatabase.Find(p => string.Equals(p.id, phraseId, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                return entry.GetPhrase(selectedLanguage);
            }
            
            return $"[Missing: {phraseId}]";
        }

        private PhraseEntry GetPhraseEntry(string phraseId)
        {
            return _phraseDatabase.Find(p => string.Equals(p.id, phraseId, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadScenariosDatabase()
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("Minigames/IsometricGame/scenarios");
            if (jsonAsset != null)
            {
                // Simple wrapper to parse array from JSON in Unity
                string wrappedJson = "{\"scenarios\":" + jsonAsset.text + "}";
                ScenarioList list = JsonUtility.FromJson<ScenarioList>(wrappedJson);
                _scenarios = list.scenarios;
                Debug.Log($"[IsometricGameplayManager] Loaded {_scenarios.Count} scenarios from JSON.");
            }
            else
            {
                Debug.LogError("[IsometricGameplayManager] scenarios.json not found in Resources/Minigames/IsometricGame/ !");
            }
        }

        public void StartGame()
        {
            _currentScenarioIndex = 0;
            _currentPatience = MaxPatience;
            _gameplayStarted = true;
            UpdatePatienceUI();
            
            if (patienceBarGroup != null) patienceBarGroup.SetActive(true);
            
            PlayCurrentScenario();
        }

        private void PlayCurrentScenario()
        {
            if (_currentScenarioIndex >= _scenarios.Count)
            {
                Debug.Log("[IsometricGameplayManager] All scenarios completed! Minigame Finished.");
                ShowWinScreen();
                return;
            }

            ScenarioData data = _scenarios[_currentScenarioIndex];
            
            // 1. Fetch the translated phrase from LuminangPhrases
            string translatedPhrase = GetTranslatedPhrase(data.phraseId);

            // Update Errand Paper HUD
            if (errandPaperTitleText != null) errandPaperTitleText.text = data.errandTitle;
            if (errandPaperPhraseText != null) errandPaperPhraseText.text = translatedPhrase;

            // Update Zoomed Errand Paper UI texts
            if (zoomedErrandTitleText != null) zoomedErrandTitleText.text = data.errandTitle;
            if (zoomedErrandPhraseText != null) zoomedErrandPhraseText.text = translatedPhrase;

            // Check if there is a registered NPC sequence for the current scenario
            NPCSequenceSetup npcSeq = npcSequences.Find(n => n.scenarioIndex == data.scenarioIndex);

            // ── managedExternally: NPC appearance was handled by the previous scenario.
            // Skip ALL NPC logic here — no camera update, no activate, no callout.
            // Camera stays wherever HandleCorrectChoice left it, wood header stays visible.
            if (npcSeq != null && npcSeq.managedExternally)
            {
                ShowRodrickDialogueAndPresentChoices(data);
                return;
            }

            // If multiple NPC sequences share the same scenarioIndex (e.g. Scene 22's 3 dancer groups),
            // activate all of the secondary ones now so they're visible from the start.
            // The primary npcSeq (first Find match) handles camera focus and the callout bubble as usual.
            // Secondary extras are just activated and play their callAnimationName (typically "Idle").
            if (npcSeq != null)
            {
                List<NPCSequenceSetup> allForScenario = npcSequences.FindAll(n => n.scenarioIndex == data.scenarioIndex);
                foreach (var extra in allForScenario)
                {
                    if (extra == npcSeq) continue; // primary is handled below
                    if (extra.npcParent != null && !extra.npcParent.activeSelf)
                    {
                        extra.npcParent.SetActive(true);
                        if (extra.npcAnimator != null && !string.IsNullOrEmpty(extra.callAnimationName))
                            extra.npcAnimator.Play(extra.callAnimationName);
                    }
                }
            }

            if (npcSeq != null)
            {
                if (woodHeader != null) woodHeader.SetActive(false);
                if (menuButton != null) menuButton.SetActive(false);

                // Push camera override if this NPC defines one
                if (cameraFollow != null && npcSeq.conversationZoom > 0f)
                {
                    cameraFollow.PushCameraOverride(
                        npcSeq.conversationZoom,
                        npcSeq.conversationYOffset,
                        npcSeq.conversationNpcPull
                    );
                }

                // Activate NPC and update camera focus
                if (npcSeq.npcParent != null)
                {
                    npcSeq.npcParent.SetActive(true);
                    if (cameraFollow != null)
                    {
                        cameraFollow.activeNpcTransform = npcSeq.npcParent.GetComponent<RectTransform>();
                    }
                }

                string language = GetSelectedLanguage();
                string npcText = language == "cebuano" ? npcSeq.dialogueCebuano : npcSeq.dialogueIlokano;

                // skipCalloutDialogue: NPC is already on-screen (inflated mid-previous-conversation).
                // Still focus camera on them but skip the intro bubble — jump straight to Rodrick's line.
                if (npcSeq.skipCalloutDialogue)
                {
                    ShowRodrickDialogueAndPresentChoices(data);
                }
                else
                {
                    if (npcSeq.npcAnimator != null && !string.IsNullOrEmpty(npcSeq.callAnimationName))
                        npcSeq.npcAnimator.Play(npcSeq.callAnimationName);

                    if (npcSeq.npcDialogueBubble != null)
                    {
                        npcSeq.npcDialogueBubble.ShowDialogueAuto(npcText, dialogueAutoHoldTime, () =>
                        {
                            if (data.scenarioIndex == 9 && npcSeq.npcParent != null)
                                StartCoroutine(DelayedDeflateNPC(npcSeq.npcParent, 3.0f, 0.4f));
                            ShowRodrickDialogueAndPresentChoices(data);
                        });
                    }
                    else
                    {
                        if (data.scenarioIndex == 9 && npcSeq.npcParent != null)
                            StartCoroutine(DelayedDeflateNPC(npcSeq.npcParent, 3.0f, 0.4f));
                        ShowRodrickDialogueAndPresentChoices(data);
                    }
                }
            }
            else
            {
                if (cameraFollow != null)
                    cameraFollow.activeNpcTransform = null;
                ShowRodrickDialogueAndPresentChoices(data);
            }
        }

        private void ShowRodrickDialogueAndPresentChoices(ScenarioData data)
        {
            if (rodrickDialogueBubble != null)
            {
                rodrickDialogueBubble.ShowDialogueAuto(data.dialogue, dialogueAutoHoldTime, () =>
                {
                    PresentChoices(data);
                });
            }
            else
            {
                PresentChoices(data);
            }
        }

        private void PresentChoices(ScenarioData data)
        {
            _waitingForChoice = true;
            if (woodHeader != null) woodHeader.SetActive(true);
            if (menuButton != null) menuButton.SetActive(true);
            ShowChoiceButtons(data);
        }

        private IEnumerator RunSTTVerification(ScenarioData data)
        {
            if (TCGSTTManager.Instance != null)
            {
                PhraseEntry phrase = GetPhraseEntry(data.phraseId);
                if (phrase != null)
                {
                    bool sttCompleted = false;
                    bool sttSuccess = false;

                    // Enable the black overlay to dim background
                    if (dialogueBlackOverlay != null) dialogueBlackOverlay.SetActive(true);

                    TCGSTTManager.Instance.StartSTT(phrase,
                        onSuccess: () =>
                        {
                            sttSuccess = true;
                            sttCompleted = true;
                        },
                        onFail: () =>
                        {
                            sttSuccess = false;
                            sttCompleted = true;
                        }
                    );

                    // Wait for STT with a safety timeout (prevents infinite hang on Android)
                    float _sttTimeout = 20f;
                    float _sttTimer = 0f;
                    while (!sttCompleted && _sttTimer < _sttTimeout)
                    {
                        _sttTimer += Time.deltaTime;
                        yield return null;
                    }
                    if (!sttCompleted)
                    {
                        Debug.LogWarning("[IsometricGameplayManager] STT timed out — treating as failure.");
                        sttSuccess = false;
                    }

                    // Disable the black overlay when done
                    if (dialogueBlackOverlay != null) dialogueBlackOverlay.SetActive(false);

                    if (sttSuccess)
                    {
                        if (data.scenarioIndex == 9)
                        {
                            StartCoroutine(ReappearPadreMarioAndSucceed(data));
                        }
                        else
                        {
                            StartCoroutine(HandleCorrectChoice());
                        }
                    }
                    else
                    {
                        StartCoroutine(HandleSTTFailureAndMistake(data));
                    }
                    yield break;
                }
            }

            // Fallback directly to success if STT manager is missing
            StartCoroutine(HandleCorrectChoice());
        }

        private IEnumerator ReappearPadreMarioAndSucceed(ScenarioData data)
        {
            yield return new WaitForSeconds(2.0f);

            NPCSequenceSetup npcSeq = npcSequences.Find(n => n.scenarioIndex == data.scenarioIndex);
            if (npcSeq != null && npcSeq.npcParent != null)
            {
                yield return StartCoroutine(InflateNPC(npcSeq.npcParent, npcSeq.originalScale, 0.4f));
            }

            StartCoroutine(HandleCorrectChoice());
        }

        private IEnumerator HandleSTTFailureAndMistake(ScenarioData data)
        {
            Debug.Log("[IsometricGameplayManager] STT Speech evaluation failed (retrying with no patience deduction).");

            yield return new WaitForSeconds(0.5f);

            // 1. Display Rodrick's tongue-twisted failure bubble
            if (rodrickDialogueBubble != null && !string.IsNullOrEmpty(data.sttFailDialogue))
            {
                bool lineFinished = false;
                rodrickDialogueBubble.ShowDialogueAuto(data.sttFailDialogue, dialogueAutoHoldTime, () => lineFinished = true);
                float _failTimeout = dialogueAutoHoldTime + 5f;
                float _failTimer = 0f;
                while (!lineFinished && _failTimer < _failTimeout) { _failTimer += Time.deltaTime; yield return null; }
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }

            // 2. Present choices again for retry
            PresentChoices(data);
        }

        private void ShowChoiceButtons(ScenarioData data)
        {
            if (choicesPanel != null) choicesPanel.SetActive(true);

            // Shuffling the choices
            List<string> shuffledChoices = new List<string>(data.choices);
            string correctText = (data.correctChoiceIndex >= 0 && data.correctChoiceIndex < data.choices.Count) 
                ? data.choices[data.correctChoiceIndex] 
                : "";

            // Fisher-Yates shuffle
            for (int i = 0; i < shuffledChoices.Count; i++)
            {
                string temp = shuffledChoices[i];
                int randomIndex = UnityEngine.Random.Range(i, shuffledChoices.Count);
                shuffledChoices[i] = shuffledChoices[randomIndex];
                shuffledChoices[randomIndex] = temp;
            }

            // Dynamically track new correct choice index
            _currentCorrectChoiceIndex = shuffledChoices.IndexOf(correctText);

            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (i < shuffledChoices.Count)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    if (choiceTexts[i] != null) choiceTexts[i].text = shuffledChoices[i];

                    int index = i; // Avoid closure allocation issues
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            StartCoroutine(OnChoiceSelectedCoroutine(choiceIndex));
        }

        private IEnumerator OnChoiceSelectedCoroutine(int choiceIndex)
        {
            if (!_waitingForChoice) yield break;
            _waitingForChoice = false;

            ScenarioData data = _scenarios[_currentScenarioIndex];
            bool isCorrect = (choiceIndex == _currentCorrectChoiceIndex);

            // 1. Play button click sound
            if (audioSource != null && buttonClickClip != null)
            {
                audioSource.PlayOneShot(buttonClickClip);
            }

            // 2. Play correct/wrong choice feedback sound
            if (audioSource != null)
            {
                AudioClip clipToPlay = isCorrect ? correctChoiceClip : wrongChoiceClip;
                if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
            }

            // 3. If correct, spawn sparkles around the button edges
            if (isCorrect && choiceIndex < choiceButtons.Count && choiceButtons[choiceIndex] != null)
            {
                StartCoroutine(SpawnSparkles(choiceButtons[choiceIndex].GetComponent<RectTransform>()));
            }

            // Wait 0.5s so players hear the full sound effects and see the sparkles before choices disappear
            yield return new WaitForSeconds(0.5f);

            if (choicesPanel != null) choicesPanel.SetActive(false);

            if (isCorrect)
            {
                if (data.useSTT)
                {
                    StartCoroutine(RunSTTVerification(data));
                }
                else
                {
                    StartCoroutine(HandleCorrectChoice());
                }
            }
            else
            {
                StartCoroutine(HandleMistake(data));
            }
        }

        private IEnumerator SpawnSparkles(RectTransform buttonRect)
        {
            if (sparkleSprite == null || buttonRect == null) yield break;

            int sparkleCount = 8;
            List<GameObject> sparkles = new List<GameObject>();
            List<Vector3> velocities = new List<Vector3>();

            float w = buttonRect.rect.width * 0.5f;
            float h = buttonRect.rect.height * 0.5f;

            for (int i = 0; i < sparkleCount; i++)
            {
                GameObject spark = new GameObject("Sparkle", typeof(Image));
                spark.transform.SetParent(buttonRect, false);

                Image img = spark.GetComponent<Image>();
                img.sprite = sparkleSprite;
                img.raycastTarget = false;

                // Position randomly along the edges
                Vector2 localPos = Vector2.zero;
                int edge = UnityEngine.Random.Range(0, 4);
                if (edge == 0) localPos = new Vector2(UnityEngine.Random.Range(-w, w), h); // top
                else if (edge == 1) localPos = new Vector2(UnityEngine.Random.Range(-w, w), -h); // bottom
                else if (edge == 2) localPos = new Vector2(-w, UnityEngine.Random.Range(-h, h)); // left
                else localPos = new Vector2(w, UnityEngine.Random.Range(-h, h)); // right

                spark.GetComponent<RectTransform>().anchoredPosition = localPos;
                spark.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.15f, 0.35f);

                sparkles.Add(spark);

                // Outward push velocity
                Vector3 vel = new Vector3(localPos.x, localPos.y, 0f).normalized * UnityEngine.Random.Range(80f, 150f);
                velocities.Add(vel);
            }

            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < sparkles.Count; i++)
                {
                    if (sparkles[i] != null)
                    {
                        RectTransform rt = sparkles[i].GetComponent<RectTransform>();
                        rt.anchoredPosition += (Vector2)(velocities[i] * Time.deltaTime);
                        sparkles[i].transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                    }
                }
                yield return null;
            }

            foreach (var spark in sparkles)
            {
                if (spark != null) Destroy(spark);
            }
        }

        private IEnumerator HandleCorrectChoice()
        {
            Debug.Log("[IsometricGameplayManager] Correct Answer selected!");

            // ── SCENE 22 SPECIAL: Parade march sequence ──────────────────
            if (_currentScenarioIndex < _scenarios.Count &&
                _scenarios[_currentScenarioIndex].scenarioIndex == 22 &&
                paradeMarchRodrickStop != null)
            {
                yield return StartCoroutine(RunParadeMarch());
                yield return new WaitForSeconds(0.5f);
                _currentScenarioIndex++;
                PlayCurrentScenario();
                yield break;
            }
            // ─────────────────────────────────────────────────────────────

            // Look up the current NPC sequence (if any)
            NPCSequenceSetup currentNpc = null;
            if (_currentScenarioIndex < _scenarios.Count)
            {
                int currentScenarioNum = _scenarios[_currentScenarioIndex].scenarioIndex;
                currentNpc = npcSequences.Find(n => n.scenarioIndex == currentScenarioNum);
            }

            bool hasConversation = currentNpc != null
                && currentNpc.successConversation != null
                && currentNpc.successConversation.Count > 0;

            // ── ALING RIZA STYLE: walk first, then talk ──────────────────
            if (!hasConversation || currentNpc.walkBeforeConversation)
            {
                // Step 1 — Walk to success waypoints (Rodrick, Player, and optionally NPC together / in-line)
                if (_currentScenarioIndex < scenarioWaypoints.Count)
                {
                    yield return StartCoroutine(ExecuteScenarioWalk(scenarioWaypoints[_currentScenarioIndex], currentNpc));
                }

                // Step 2 — Play success conversation (if any)
                if (hasConversation)
                    yield return StartCoroutine(PlaySuccessConversation(currentNpc));

                // Step 3 — Shrink NPC (unless instructed to keep them active)
                if (currentNpc != null && currentNpc.npcParent != null && currentNpc.npcParent.activeSelf)
                {
                    if (!currentNpc.keepActiveAfterSuccess)
                        yield return StartCoroutine(DeflateNPC(currentNpc.npcParent, 0.4f));
                }
            }
            // ── PADRE MARIO STYLE: talk first, then walk ─────────────────
            else
            {
                // Step 1 — Play success conversation first
                yield return StartCoroutine(PlaySuccessConversation(currentNpc));

                // Step 2 — Shrink NPC (unless instructed to keep them active)
                if (currentNpc.npcParent != null && currentNpc.npcParent.activeSelf)
                {
                    if (!currentNpc.keepActiveAfterSuccess)
                        yield return StartCoroutine(DeflateNPC(currentNpc.npcParent, 0.4f));
                }

                // Step 3 — Walk to success waypoints (Rodrick, Player, and optionally NPC together / in-line)
                if (_currentScenarioIndex < scenarioWaypoints.Count)
                {
                    yield return StartCoroutine(ExecuteScenarioWalk(scenarioWaypoints[_currentScenarioIndex], currentNpc));
                }
            }

            // Walk back to return waypoints if configured
            if (_currentScenarioIndex < scenarioWaypoints.Count)
            {
                var waypoints = scenarioWaypoints[_currentScenarioIndex];
                if (waypoints.returnWaypoint != null || waypoints.playerReturnWaypoint != null || waypoints.npcReturnWaypoint != null)
                {
                    Coroutine rodrickReturn = null;
                    Coroutine playerReturn  = null;
                    Coroutine npcReturn     = null;

                    if (rodrick != null && waypoints.returnWaypoint != null)
                        rodrickReturn = StartCoroutine(rodrick.MoveTo(waypoints.returnWaypoint.position, characterWalkDuration));
                    if (player != null && waypoints.playerReturnWaypoint != null)
                        playerReturn  = StartCoroutine(player.MoveTo(waypoints.playerReturnWaypoint.position, characterWalkDuration));
                    if (currentNpc != null && currentNpc.npcParent != null && waypoints.npcReturnWaypoint != null)
                        npcReturn     = StartCoroutine(MoveNPC(currentNpc, waypoints.npcReturnWaypoint, characterWalkDuration));

                    if (rodrickReturn != null) yield return rodrickReturn;
                    if (playerReturn  != null) yield return playerReturn;
                    if (npcReturn     != null) yield return npcReturn;
                }
            }

            // Inflate the next NPC in sequence if it hasn't been activated yet.
            // We look up the NEXT scenario's scenarioIndex field value directly so this
            // is never broken by gaps in the array or scenarioIndex numbering.
            if (_currentScenarioIndex + 1 < _scenarios.Count)
            {
                int upcomingScenIdx = _scenarios[_currentScenarioIndex + 1].scenarioIndex;
                NPCSequenceSetup nextNpc = npcSequences.Find(n => n.scenarioIndex == upcomingScenIdx);
                if (nextNpc != null && nextNpc.npcParent != null && !nextNpc.npcParent.activeSelf)
                    yield return StartCoroutine(InflateNPC(nextNpc.npcParent, nextNpc.originalScale, 0.4f));
            }

            // Brief pause then advance
            yield return new WaitForSeconds(0.5f);
            _currentScenarioIndex++;
            PlayCurrentScenario();
        }

        private IEnumerator PlaySuccessConversation(NPCSequenceSetup npc)
        {
            if (woodHeader != null) woodHeader.SetActive(false);
            if (menuButton  != null) menuButton.SetActive(false);

            // If this NPC is managedExternally it has no callout intro, so the camera wasn't
            // pointed at it by PlayCurrentScenario. Set it now for the conversation phase.
            if (cameraFollow != null && npc.npcParent != null && npc.managedExternally)
            {
                cameraFollow.activeNpcTransform = npc.npcParent.GetComponent<RectTransform>();
            }

            // Push a camera override if this NPC defines one.
            // conversationZoom <= 0 means "no override" so we leave the camera alone.
            bool hasCameraOverride = npc.conversationZoom > 0f;
            if (hasCameraOverride && cameraFollow != null)
            {
                cameraFollow.PushCameraOverride(
                    npc.conversationZoom,
                    npc.conversationYOffset,
                    npc.conversationNpcPull
                );
            }

            string language = GetSelectedLanguage();

            foreach (var line in npc.successConversation)
            {
                // Trigger next NPC inflation if explicitly flagged on this dialogue line.
                // We search by the NEXT scenarioIndex field value (current NPC's scenarioIndex + 1),
                // NOT by array offset, so it always finds the right NPC regardless of list order.
                if (line.triggerInflateNPCNext)
                {
                    int nextScenIdx = npc.scenarioIndex + 1;
                    NPCSequenceSetup nextNpc = npcSequences.Find(n => n.scenarioIndex == nextScenIdx);
                    if (nextNpc != null && nextNpc.npcParent != null && !nextNpc.npcParent.activeSelf)
                    {
                        StartCoroutine(InflateNPC(nextNpc.npcParent, nextNpc.originalScale, 0.4f));
                        // Shift camera so the newly-appeared NPC is pulled into frame alongside the group
                        if (cameraFollow != null)
                            cameraFollow.activeNpcTransform = nextNpc.npcParent.GetComponent<RectTransform>();
                    }
                }

                // If this line specifies an animation, play it on the NPC before showing dialogue
                if (!string.IsNullOrEmpty(line.animationToPlay) && npc.npcAnimator != null)
                {
                    npc.npcAnimator.Play(line.animationToPlay);
                    // Brief pause so the animation visually registers before the text pops up
                    yield return new WaitForSeconds(0.35f);
                }

                string textToShow = language == "cebuano"
                    ? (!string.IsNullOrEmpty(line.textCebuano)  ? line.textCebuano  : line.textEnglish)
                    : (!string.IsNullOrEmpty(line.textIlokano)  ? line.textIlokano  : line.textEnglish);

                bool lineFinished = false;

                if (line.isNPC && npc.npcDialogueBubble != null)
                    npc.npcDialogueBubble.ShowDialogueAuto(textToShow, dialogueAutoHoldTime, () => lineFinished = true);
                else if (!line.isNPC && rodrickDialogueBubble != null)
                    rodrickDialogueBubble.ShowDialogueAuto(textToShow, dialogueAutoHoldTime, () => lineFinished = true);
                else
                    lineFinished = true;

                // Timeout guard
                float _convTimeout = dialogueAutoHoldTime + 5f;
                float _convTimer = 0f;
                while (!lineFinished && _convTimer < _convTimeout) { _convTimer += Time.deltaTime; yield return null; }
            }

            // Reset camera focus to player/rodrick midpoint and release any override
            if (cameraFollow != null)
            {
                cameraFollow.activeNpcTransform = null;
                if (hasCameraOverride)
                    cameraFollow.ClearCameraOverride();
            }

            if (woodHeader != null) woodHeader.SetActive(true);
            if (menuButton  != null) menuButton.SetActive(true);
        }

        private IEnumerator HandleMistake(ScenarioData data)
        {
            Debug.Log("[IsometricGameplayManager] Mistake made!");

            Vector3 rodrickStartPos = rodrick != null ? rodrick.transform.position : Vector3.zero;
            bool hasMistakeWaypoint = false;
            
            // 1. Check if there's a custom mistake waypoint configured
            if (_currentScenarioIndex < scenarioWaypoints.Count)
            {
                var waypoints = scenarioWaypoints[_currentScenarioIndex];
                if (rodrick != null && waypoints.mistakeWaypoint != null)
                {
                    hasMistakeWaypoint = true;
                    yield return StartCoroutine(rodrick.MoveTo(waypoints.mistakeWaypoint.position, characterWalkDuration));
                }
            }

            // 2. Play Bump/Boink Audio
            if (audioSource != null && bumpClip != null)
            {
                audioSource.PlayOneShot(bumpClip);
            }

            // 3. Play Bump/Confused animations (using the custom one from JSON, or default to Confused)
            if (rodrick != null)
            {
                string anim = !string.IsNullOrEmpty(data.mistakeAnimation) ? data.mistakeAnimation : "Confused";
                rodrick.PlayState(anim);
            }
            
            // Deduct patience and blink the lost bar
            if (_currentPatience > 0 && _currentPatience - 1 < patienceHeartBars.Count)
            {
                Image lostBar = patienceHeartBars[_currentPatience - 1];
                StartCoroutine(BlinkAndDisableBar(lostBar));
            }

            _currentPatience = Mathf.Max(0, _currentPatience - 1);

            yield return new WaitForSeconds(1.5f);

            if (rodrick != null) rodrick.PlayState("Idle");

            // 4. Trigger Lola Nida Phone Call Sequence (pass scenario data to read portrait index)
            yield return StartCoroutine(RunLolaNidaCall(data));

            // 5. Walk Rodrick back to starting position if he wandered off
            if (hasMistakeWaypoint && rodrick != null)
            {
                yield return StartCoroutine(rodrick.MoveTo(rodrickStartPos, characterWalkDuration));
            }

            // 6. Check if out of patience (Game Over) or return to same scenario choice
            if (_currentPatience <= 0)
            {
                ShowLoseScreen();
            }
            else
            {
                PlayCurrentScenario();
            }
        }

        private IEnumerator RunLolaNidaCall(ScenarioData data)
        {
            if (phoneCallGroup == null) yield break;

            phoneCallGroup.SetActive(true);
            
            // Hide portrait and dialogue bubble during ringing
            if (lolaNidaPortraitImage != null) lolaNidaPortraitImage.gameObject.SetActive(false);
            if (lolaNidaDialogueBubble != null) lolaNidaDialogueBubble.gameObject.SetActive(false);

            // Ringing phase
            if (phoneImage != null && phoneRingingSprite != null)
            {
                phoneImage.gameObject.SetActive(true);
                phoneImage.sprite = phoneRingingSprite;
            }

            // Start Ringing Audio
            if (audioSource != null && phoneRingingClip != null)
            {
                audioSource.clip = phoneRingingClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Wobble/Shake the phone slowly in a wide way
            float shakeTime = 2.5f;
            float elapsed = 0f;
            Vector2 originalPos = phoneRectTransform != null ? phoneRectTransform.anchoredPosition : Vector2.zero;

            while (elapsed < shakeTime)
            {
                elapsed += Time.deltaTime;
                if (phoneRectTransform != null)
                {
                    // Gentle wide wobble using sine waves
                    float angle = Mathf.Sin(elapsed * 8f) * 12f;
                    float offsetX = Mathf.Sin(elapsed * 12f) * 15f;
                    phoneRectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    phoneRectTransform.anchoredPosition = originalPos + new Vector2(offsetX, 0f);
                }
                yield return null;
            }

            // Stop Ringing Audio, Play PickUp Audio
            if (audioSource != null)
            {
                audioSource.Stop();
                if (phonePickUpClip != null)
                {
                    audioSource.PlayOneShot(phonePickUpClip);
                }
            }

            // Abrupt pick up swap
            if (phoneRectTransform != null)
            {
                phoneRectTransform.localRotation = Quaternion.identity;
                phoneRectTransform.anchoredPosition = originalPos;
            }
            if (phoneImage != null && phoneAnsweredSprite != null)
                phoneImage.sprite = phoneAnsweredSprite;

            // Wait for pickup sound to finish (or fixed delay)
            float pickUpDuration = phonePickUpClip != null ? phonePickUpClip.length : 1f;
            yield return new WaitForSeconds(pickUpDuration);

            // Set custom portrait sprite if configured
            if (lolaNidaPortraitImage != null && lolaNidaPortraits != null && lolaNidaPortraits.Count > 0)
            {
                int index = Mathf.Clamp(data.lolaNidaPortraitIndex, 0, lolaNidaPortraits.Count - 1);
                lolaNidaPortraitImage.sprite = lolaNidaPortraits[index];
            }

            // Hide the phone image, and reveal Nida's portrait and dialogue bubble
            if (phoneImage != null) phoneImage.gameObject.SetActive(false);
            if (lolaNidaPortraitImage != null) lolaNidaPortraitImage.gameObject.SetActive(true);
            if (lolaNidaDialogueBubble != null) lolaNidaDialogueBubble.gameObject.SetActive(true);

            // Auto type Lola Nida's dialogue
            bool dialogueFinished = false;
            if (lolaNidaDialogueBubble != null)
            {
                lolaNidaDialogueBubble.ShowDialogueAuto(data.mistakeDialogue, dialogueAutoHoldTime, () =>
                {
                    dialogueFinished = true;
                });
            }
            else
            {
                dialogueFinished = true;
            }

            // Timeout guard: prevents infinite hang if dialogue callback never fires (e.g. on Android)
            float _nidaTimeout = dialogueAutoHoldTime + 5f;
            float _nidaTimer = 0f;
            while (!dialogueFinished && _nidaTimer < _nidaTimeout) { _nidaTimer += Time.deltaTime; yield return null; }

            // Hide everything call-related
            phoneCallGroup.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────
        // Win / Lose Results & Scoring Logic
        // ─────────────────────────────────────────────────────────────────

        public void ShowWinScreen()
        {
            Debug.Log($"[IsometricGameplayManager] Minigame Won! Remaining patience: {_currentPatience}");

            if (woodHeader != null) woodHeader.SetActive(false);
            if (menuButton != null) menuButton.SetActive(false);
            if (choicesPanel != null) choicesPanel.SetActive(false);
            if (phoneCallGroup != null) phoneCallGroup.SetActive(false);
            if (rodrickDialogueBubble != null) rodrickDialogueBubble.gameObject.SetActive(false);
            if (lolaNidaDialogueBubble != null) lolaNidaDialogueBubble.gameObject.SetActive(false);

            if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
            if (winPanel != null) winPanel.SetActive(true);
            if (losePanel != null) losePanel.SetActive(false);

            int stars = Mathf.Clamp(_currentPatience, 1, MaxPatience);
            int coinsEarned = _currentPatience * 10;

            // Auto-find star images if unassigned in Inspector
            if (winStars == null || winStars.Length == 0)
            {
                if (winPanel != null)
                {
                    var foundList = new List<Image>();
                    for (int i = 1; i <= 5; i++)
                    {
                        var child = winPanel.transform.Find($"Star{i}");
                        if (child != null && child.TryGetComponent<Image>(out var img))
                        {
                            foundList.Add(img);
                        }
                    }
                    if (foundList.Count > 0) winStars = foundList.ToArray();
                }
            }

            // Auto-recover star sprites if unassigned in Inspector (mobile-optimized fallback)
            if (activeStarSprite == null || inactiveStarSprite == null)
            {
                if (activeStarSprite == null) activeStarSprite = UnityEngine.Resources.Load<Sprite>("UI/star_active");
                if (inactiveStarSprite == null) inactiveStarSprite = UnityEngine.Resources.Load<Sprite>("UI/star_inactive");

                #if UNITY_EDITOR
                if (activeStarSprite == null || inactiveStarSprite == null)
                {
                    Sprite[] allSprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();
                    foreach (var sp in allSprites)
                    {
                        if (activeStarSprite == null && sp.name == "star_active") activeStarSprite = sp;
                        if (inactiveStarSprite == null && sp.name == "star_inactive") inactiveStarSprite = sp;
                    }
                }
                #endif
            }

            // Assign star sprites
            if (winStars != null)
            {
                for (int i = 0; i < winStars.Length; i++)
                {
                    if (winStars[i] != null)
                    {
                        Sprite s = (i < stars) ? activeStarSprite : inactiveStarSprite;
                        if (s != null) winStars[i].sprite = s;
                        winStars[i].gameObject.SetActive(true);
                    }
                }
            }

            if (winCoinsText != null)
            {
                winCoinsText.text = $"+{coinsEarned}";
            }

            // Save coins & minigame win state
            int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
            PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
            PlayerPrefs.SetInt("IsometricMinigameWon", 1);
            PlayerPrefs.SetInt("MinigameWon", 1);
            PlayerPrefs.Save();

            if (audioSource != null && winPanelSFX != null)
            {
                audioSource.PlayOneShot(winPanelSFX);
            }
        }

        public void ShowLoseScreen()
        {
            Debug.Log("[IsometricGameplayManager] Game Over! Showing Lose Screen.");

            if (woodHeader != null) woodHeader.SetActive(false);
            if (menuButton != null) menuButton.SetActive(false);
            if (choicesPanel != null) choicesPanel.SetActive(false);
            if (phoneCallGroup != null) phoneCallGroup.SetActive(false);
            if (rodrickDialogueBubble != null) rodrickDialogueBubble.gameObject.SetActive(false);
            if (lolaNidaDialogueBubble != null) lolaNidaDialogueBubble.gameObject.SetActive(false);

            if (winOrLoseGroup != null) winOrLoseGroup.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(true);

            int consolationCoins = 2;
            if (loseCoinsText != null)
            {
                loseCoinsText.text = $"+{consolationCoins}";
            }

            int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
            PlayerPrefs.SetInt("PlayerCoins", currentCoins + consolationCoins);
            PlayerPrefs.SetInt("IsometricMinigameWon", 0);
            PlayerPrefs.SetInt("MinigameWon", 0);
            PlayerPrefs.Save();

            if (audioSource != null && losePanelSFX != null)
            {
                audioSource.PlayOneShot(losePanelSFX);
            }
        }

        // Call this from the "Try Again" Button OnClick()
        public void RestartGame()
        {
            if (audioSource != null && buttonClickClip != null) audioSource.PlayOneShot(buttonClickClip);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // Call this from the WinPanel's "Continue" Button OnClick()
        public void ContinueToNextObjective()
        {
            if (audioSource != null && buttonClickClip != null) audioSource.PlayOneShot(buttonClickClip);
            ExitMinigameToPreviousScene();
        }

        // Call this from the LosePanel's "Quit" Button OnClick()
        public void QuitToPreviousScene()
        {
            if (audioSource != null && buttonClickClip != null) audioSource.PlayOneShot(buttonClickClip);
            ExitMinigameToPreviousScene();
        }

        // Alias for QuitToPreviousScene
        public void QuitGame()
        {
            if (audioSource != null && buttonClickClip != null) audioSource.PlayOneShot(buttonClickClip);
            ExitMinigameToPreviousScene();
        }

        // Call this from the HowToPlay XButton OnClick()
        public void CloseHowToPlay()
        {
            if (audioSource != null && buttonClickClip != null) audioSource.PlayOneShot(buttonClickClip);

            if (howToPlayGroup != null)
            {
                howToPlayGroup.SetActive(false);
            }

            // Only trigger the intro sequence's CloseHowToPlay if the main game hasn't started yet!
            if (!_gameplayStarted)
            {
                var seqManager = FindFirstObjectByType<IsometricSequenceManager>();
                if (seqManager != null)
                {
                    seqManager.CloseHowToPlay();
                }
            }
        }

        public void OpenHowToPlay()
        {
            if (audioSource != null && buttonClickClip != null) audioSource.PlayOneShot(buttonClickClip);

            if (howToPlayGroup != null)
            {
                howToPlayGroup.SetActive(true);
            }
        }

        public void ExitMinigameToPreviousScene()
        {
            string prevScene = PlayerPrefs.GetString("PreviousScene", "Magellan's_Cross");
            SceneLoader.ResetLoadingFlag();
            SceneLoader.targetSceneForLoading = prevScene;
            SceneLoader.keepBackgroundPersistent = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        private IEnumerator BlinkAndDisableBar(Image barImage)
        {
            if (barImage == null) yield break;

            // Blink 3 times
            for (int i = 0; i < 3; i++)
            {
                barImage.enabled = false;
                yield return new WaitForSeconds(0.15f);
                barImage.enabled = true;
                yield return new WaitForSeconds(0.15f);
            }
            barImage.enabled = false;
        }

        private void UpdatePatienceUI()
        {
            for (int i = 0; i < patienceHeartBars.Count; i++)
            {
                if (patienceHeartBars[i] != null)
                {
                    patienceHeartBars[i].enabled = (i < _currentPatience);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Zoomed Errand Paper Popup Logic
        // ─────────────────────────────────────────────────────────────────

        private void OnHUDErrandPaperClicked()
        {
            if (_zoomedPaperOpen) return;
            StartCoroutine(ShowZoomedErrandPaper());
        }

        private IEnumerator ShowZoomedErrandPaper()
        {
            _zoomedPaperOpen = true;

            // Play open SFX
            if (audioSource != null && errandPaperOpenClip != null)
            {
                audioSource.PlayOneShot(errandPaperOpenClip);
            }

            if (zoomedErrandPaperPanel != null) zoomedErrandPaperPanel.SetActive(true);
            if (tapToCloseText != null) tapToCloseText.SetActive(false);

            // Wait 1 full second (non-skippable reading phase)
            yield return new WaitForSecondsRealtime(1f);

            if (tapToCloseText != null) tapToCloseText.SetActive(true);

            // Wait for user touch or click to dismiss (cross-platform)
            bool dismissed = false;
            while (!dismissed)
            {
                #if ENABLE_INPUT_SYSTEM
                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                {
                    dismissed = true;
                }
                else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    dismissed = true;
                }
                else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    dismissed = true;
                }
                #endif

                #if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    dismissed = true;
                }
                #endif

                yield return null;
            }

            if (zoomedErrandPaperPanel != null) zoomedErrandPaperPanel.SetActive(false);

            // Play close SFX
            if (audioSource != null && errandPaperCloseClip != null)
            {
                audioSource.PlayOneShot(errandPaperCloseClip);
            }

            _zoomedPaperOpen = false;
        }

        private IEnumerator InflateNPC(GameObject npc, Vector3 targetScale, float duration)
        {
            npc.SetActive(true);
            npc.transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Overshoot curve
                float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.15f;
                if (t >= 0.85f)
                {
                    scale = Mathf.Lerp(1.15f, 1.0f, (t - 0.85f) / 0.15f);
                }
                
                npc.transform.localScale = targetScale * scale;
                yield return null;
            }

            npc.transform.localScale = targetScale;
        }

        private IEnumerator DeflateNPC(GameObject npc, float duration)
        {
            Vector3 startScale = npc.transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Scale down smoothly to 0
                float scale = 1f - t;
                npc.transform.localScale = startScale * scale;
                yield return null;
            }

            npc.transform.localScale = Vector3.zero;
            npc.SetActive(false);
        }

        private IEnumerator DelayedDeflateNPC(GameObject npc, float delay, float deflateDuration)
        {
            yield return new WaitForSeconds(delay);
            yield return StartCoroutine(DeflateNPC(npc, deflateDuration));
        }

        private IEnumerator MoveTransformTo(Transform trans, Vector3 targetPos, float duration)
        {
            Vector3 startPos = trans.position;
            float elapsed = 0f;

            // Simple movement logic similar to IsometricCharacter's MoveTo
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smoother step
                t = t * t * (3f - 2f * t);
                trans.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            trans.position = targetPos;
        }

        // ── Scene 22 helpers ─────────────────────────────────────────────

        /// <summary>
        /// Moves a dancer's RectTransform by a world-space delta offset over <paramref name="duration"/> seconds.
        /// The dancer plays Walk on start and Idle when done. This keeps their relative formation
        /// intact (they each move the SAME vector Rodrick travels, so spacing is preserved).
        /// </summary>
        private IEnumerator MarchDancerBy(DancerMarchSetup dancer, Vector3 worldDelta, float duration)
        {
            if (dancer == null || dancer.dancerParent == null) yield break;

            Transform t = dancer.dancerParent.transform;
            if (t.parent == null) yield break;

            // Face upward (no horizontal flip needed for straight-up march)
            if (dancer.dancerAnimator != null)
                dancer.dancerAnimator.Play("Walk");

            // Convert start world position → local, and target = start + delta → local
            Vector3 localStart  = t.localPosition;
            Vector3 worldTarget = t.position + worldDelta;
            Vector3 localTarget = t.parent.InverseTransformPoint(worldTarget);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pct = Mathf.Clamp01(elapsed / duration);
                // Linear lerp (matches IsometricCharacter.MoveTo for 1:1 sync)
                t.localPosition = Vector3.Lerp(localStart, localTarget, pct);
                yield return null;
            }

            t.localPosition = localTarget;

            if (dancer.dancerAnimator != null)
                dancer.dancerAnimator.Play("Idle");
        }

        /// <summary>
        /// Scene 22 parade sequence:
        ///   1. Rodrick + Player (and optional dancers with formationWaypoint) walk into formation.
        ///   2. All dancers, Rodrick, and Player march upward in unison.
        ///   3. Dancers idle in place; Rodrick + Player arrive at their stop waypoints.
        /// </summary>
        private IEnumerator RunParadeMarch()
        {
            // ── Step 1: Walk Rodrick + Player (and dancers with formationWaypoint) to starting positions ──
            List<Coroutine> formationWalks = new List<Coroutine>();
            if (_currentScenarioIndex < scenarioWaypoints.Count)
            {
                var wp = scenarioWaypoints[_currentScenarioIndex];
                if (rodrick != null && wp.successWaypoint != null)
                    formationWalks.Add(StartCoroutine(rodrick.MoveTo(wp.successWaypoint.position, characterWalkDuration)));
                if (player != null && wp.playerSuccessWaypoint != null)
                    formationWalks.Add(StartCoroutine(player.MoveTo(wp.playerSuccessWaypoint.position, characterWalkDuration)));
            }

            foreach (var dancer in paradeDancers)
            {
                if (dancer != null && dancer.dancerParent != null && dancer.formationWaypoint != null)
                {
                    NPCSequenceSetup dummySetup = new NPCSequenceSetup { npcParent = dancer.dancerParent, npcAnimator = dancer.dancerAnimator };
                    formationWalks.Add(StartCoroutine(MoveNPC(dummySetup, dancer.formationWaypoint, characterWalkDuration)));
                }
            }
            foreach (var c in formationWalks) yield return c;

            // Brief pause before the march begins — feels more natural
            yield return new WaitForSeconds(0.3f);

            // ── Step 2: Compute the default march delta from Rodrick's current pos → his stop point ──
            Vector3 rodrickCurrentWorld = rodrick != null ? rodrick.transform.position : Vector3.zero;
            Vector3 marchDelta = paradeMarchRodrickStop != null
                ? (paradeMarchRodrickStop.position - rodrickCurrentWorld)
                : Vector3.zero;

            // ── Step 3: Start Walk animation on dancers before march coroutines fire ──
            foreach (var d in paradeDancers)
                if (d != null && d.dancerAnimator != null) d.dancerAnimator.Play("Walk");

            // ── Step 4: Launch all march coroutines in parallel ──
            List<Coroutine> marchCoroutines = new List<Coroutine>();

            if (rodrick != null && paradeMarchRodrickStop != null)
                marchCoroutines.Add(StartCoroutine(rodrick.MoveTo(paradeMarchRodrickStop.position, paradeMarchDuration)));
            if (player != null && paradeMarchPlayerStop != null)
                marchCoroutines.Add(StartCoroutine(player.MoveTo(paradeMarchPlayerStop.position, paradeMarchDuration)));

            foreach (var dancer in paradeDancers)
            {
                if (dancer != null && dancer.dancerParent != null)
                {
                    if (dancer.stopWaypoint != null)
                    {
                        NPCSequenceSetup dummySetup = new NPCSequenceSetup { npcParent = dancer.dancerParent, npcAnimator = dancer.dancerAnimator };
                        marchCoroutines.Add(StartCoroutine(MoveNPC(dummySetup, dancer.stopWaypoint, paradeMarchDuration)));
                    }
                    else
                    {
                        marchCoroutines.Add(StartCoroutine(MarchDancerBy(dancer, marchDelta, paradeMarchDuration)));
                    }
                }
            }

            // Wait for everyone to finish marching
            foreach (var c in marchCoroutines) yield return c;

            // ── Step 5: Dancers stay in Idle when done. ──
            Debug.Log("[IsometricGameplayManager] Scene 22 parade march complete.");
        }

        /// <summary>
        /// Moves an NPC to a target waypoint using the same technique as IsometricCharacter.MoveTo:
        /// converts the waypoint's world position into the NPC parent's local space, then lerps
        /// localPosition. This is correct for Canvas UI elements and is immune to Canvas Scaler distortion.
        /// </summary>
        private IEnumerator MoveNPC(NPCSequenceSetup npcSetup, Transform targetWaypoint, float duration)
        {
            if (npcSetup == null || npcSetup.npcParent == null || targetWaypoint == null)
            {
                Debug.LogWarning("[MoveNPC] Aborted — npcSetup, npcParent, or targetWaypoint is null.");
                yield break;
            }

            Transform npcTransform = npcSetup.npcParent.transform;

            if (npcTransform.parent == null)
            {
                Debug.LogWarning("[MoveNPC] Aborted — NPC has no parent transform (cannot convert to local space).");
                yield break;
            }

            // --- Direction / sprite mirror ---
            // Compare world X positions BEFORE converting to local space, so direction is always correct.
            float worldDeltaX = targetWaypoint.position.x - npcTransform.position.x;
            if (Mathf.Abs(worldDeltaX) > 0.01f)
            {
                // Mirror the child animator sprite, NOT the parent (same pattern as IsometricCharacter)
                if (npcSetup.npcAnimator != null)
                {
                    Vector3 animScale = npcSetup.npcAnimator.transform.localScale;
                    float absX = Mathf.Abs(animScale.x);
                    animScale.x = (worldDeltaX < 0f) ? -absX : absX;
                    npcSetup.npcAnimator.transform.localScale = animScale;
                }
            }

            // --- Walk animation ---
            if (npcSetup.npcAnimator != null)
                npcSetup.npcAnimator.Play("Walk");

            // --- Convert waypoint world position → NPC parent local space ---
            // This is identical to what IsometricCharacter.MoveTo does on line 61.
            Vector3 localStart  = npcTransform.localPosition;
            Vector3 localTarget = npcTransform.parent.InverseTransformPoint(targetWaypoint.position);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Linear lerp (matches IsometricCharacter.MoveTo for 1:1 sync)
                npcTransform.localPosition = Vector3.Lerp(localStart, localTarget, t);
                yield return null;
            }

            // Snap exactly to target
            npcTransform.localPosition = localTarget;

            // --- Idle animation ---
            if (npcSetup.npcAnimator != null)
                npcSetup.npcAnimator.Play("Idle");
        }

        // ─────────────────────────────────────────────────────────────────
        // Visual Walk Path & In-Line Movement Helpers
        // ─────────────────────────────────────────────────────────────────

        private IEnumerator ExecuteScenarioWalk(ScenarioWaypointSetup waypoints, NPCSequenceSetup currentNpc)
        {
            if (waypoints == null) yield break;

            float duration = (waypoints.customWalkDuration > 0f) ? waypoints.customWalkDuration : characterWalkDuration;

            // ── Walk Path Mode: characters walk through the visual path nodes drawn in Scene View ──
            if (waypoints.walkPath != null && waypoints.walkPath.HasPoints())
            {
                List<Vector3> pathNodes = waypoints.walkPath.GetPathWorldPositions();

                if (pathNodes.Count >= 2)
                {
                    // Player path: starts from Player's current world position, then walks through all path nodes.
                    // The FIRST path node should be placed where Player is standing, and the LAST node
                    // is where Player will stop. No teleporting — Player walks the full route.
                    List<Vector3> playerPath = new List<Vector3>();
                    if (player != null) playerPath.Add(player.transform.position); // current pos as start
                    playerPath.AddRange(pathNodes);                                 // all drawn nodes follow

                    // Rodrick's path: same nodes but from Rodrick's own current position.
                    // followerDelay ensures Rodrick is always BEHIND Player on the same line.
                    List<Vector3> rodrickPath = new List<Vector3>();
                    if (rodrick != null) rodrickPath.Add(rodrick.transform.position); // current pos as start
                    rodrickPath.AddRange(pathNodes);                                   // same nodes

                    Coroutine playerWalk  = null;
                    Coroutine rodrickWalk = null;
                    Coroutine npcWalk     = null;

                    // 1. Player leads — walks from their spot, through every node, to the last node
                    if (player != null)
                        playerWalk = StartCoroutine(player.MoveAlongPath(playerPath, duration));

                    // 2. Rodrick follows the SAME path with a delay — always stays BEHIND Player on the line
                    if (rodrick != null)
                        rodrickWalk = StartCoroutine(FollowPathDelayed(rodrick, rodrickPath, duration, waypoints.followerDelay));

                    // 3. Optional NPC movement (straight walk, not path-based)
                    if (currentNpc != null && currentNpc.npcParent != null && waypoints.npcSuccessWaypoint != null)
                        npcWalk = StartCoroutine(MoveNPC(currentNpc, waypoints.npcSuccessWaypoint, duration));

                    if (playerWalk  != null) yield return playerWalk;
                    if (rodrickWalk != null) yield return rodrickWalk;
                    if (npcWalk     != null) yield return npcWalk;

                    // After the path walk, walk them straight to their exact success waypoints
                    Coroutine playerFinal  = null;
                    Coroutine rodrickFinal = null;
                    if (player  != null && waypoints.playerSuccessWaypoint != null)
                        playerFinal  = StartCoroutine(player.MoveTo(waypoints.playerSuccessWaypoint.position, 0.4f));
                    if (rodrick != null && waypoints.successWaypoint != null)
                        rodrickFinal = StartCoroutine(rodrick.MoveTo(waypoints.successWaypoint.position, 0.4f));
                    if (playerFinal  != null) yield return playerFinal;
                    if (rodrickFinal != null) yield return rodrickFinal;
                }
            }
            // ── Fallback Mode: straight-line walk directly to successWaypoints ──
            else
            {
                Coroutine rodrickWalk = null;
                Coroutine playerWalk  = null;
                Coroutine npcWalk     = null;

                if (rodrick != null && waypoints.successWaypoint != null)
                    rodrickWalk = StartCoroutine(rodrick.MoveTo(waypoints.successWaypoint.position, duration));
                if (player != null && waypoints.playerSuccessWaypoint != null)
                    playerWalk  = StartCoroutine(player.MoveTo(waypoints.playerSuccessWaypoint.position, duration));
                if (currentNpc != null && currentNpc.npcParent != null && waypoints.npcSuccessWaypoint != null)
                    npcWalk     = StartCoroutine(MoveNPC(currentNpc, waypoints.npcSuccessWaypoint, duration));

                if (rodrickWalk != null) yield return rodrickWalk;
                if (playerWalk  != null) yield return playerWalk;
                if (npcWalk     != null) yield return npcWalk;
            }
        }


        private IEnumerator FollowPathDelayed(IsometricCharacter character, List<Vector3> path, float duration, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            yield return StartCoroutine(character.MoveAlongPath(path, duration));
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _showDebugPanel = false;
        private Vector2 _debugScrollPos;

        private void OnGUI()
        {
            // Small toggle button in the top left corner of the screen
            if (GUI.Button(new Rect(10, 80, 100, 30), "Debug Skip"))
            {
                _showDebugPanel = !_showDebugPanel;
            }

            if (_showDebugPanel)
            {
                GUI.Box(new Rect(10, 115, 200, 350), "Jump to Scenario");
                
                GUILayout.BeginArea(new Rect(15, 135, 190, 320));
                _debugScrollPos = GUILayout.BeginScrollView(_debugScrollPos);

                for (int i = 0; i < _scenarios.Count; i++)
                {
                    int scenarioIndexNum = _scenarios[i].scenarioIndex;
                    string name = $"Scenario {scenarioIndexNum}";
                    if (GUILayout.Button(name))
                    {
                        JumpToScenario(i);
                        _showDebugPanel = false;
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void JumpToScenario(int index)
        {
            if (index < 0 || index >= _scenarios.Count) return;

            // 1. Hide active dialogues/panels
            if (choicesPanel != null) choicesPanel.SetActive(false);
            if (rodrickDialogueBubble != null) rodrickDialogueBubble.gameObject.SetActive(false);
            if (lolaNidaDialogueBubble != null) lolaNidaDialogueBubble.gameObject.SetActive(false);
            if (phoneCallGroup != null) phoneCallGroup.SetActive(false);
            if (lolaNidaPortraitGroup != null) lolaNidaPortraitGroup.SetActive(false);
            if (zoomedErrandPaperPanel != null) zoomedErrandPaperPanel.SetActive(false);
            if (winOrLoseGroup != null) winOrLoseGroup.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);

            // 2. Clear camera NPC focus and override
            if (cameraFollow != null)
            {
                cameraFollow.activeNpcTransform = null;
                cameraFollow.ClearCameraOverride();
            }

            // 3. Deactivate all NPCs
            foreach (var npcSeq in npcSequences)
            {
                if (npcSeq != null && npcSeq.npcParent != null)
                {
                    npcSeq.npcParent.SetActive(false);
                }
                if (npcSeq != null && npcSeq.npcDialogueBubble != null)
                {
                    npcSeq.npcDialogueBubble.gameObject.SetActive(false);
                }
            }

            // 4. Snap characters to the waypoint BEFORE the target scenario
            int targetWaypointIndex = index - 1;
            if (targetWaypointIndex >= 0 && targetWaypointIndex < scenarioWaypoints.Count)
            {
                var waypoints = scenarioWaypoints[targetWaypointIndex];
                // Snap Rodrick
                if (rodrick != null && waypoints.successWaypoint != null)
                {
                    rodrick.transform.position = waypoints.successWaypoint.position;
                    // Reset scale mirroring
                    Vector3 rScale = rodrick.transform.localScale;
                    rodrick.transform.localScale = new Vector3(Mathf.Abs(rScale.x), rScale.y, rScale.z);
                    var rAnim = rodrick.GetComponentInChildren<IsometricSpriteAnimator>();
                    if (rAnim != null)
                    {
                        Vector3 aScale = rAnim.transform.localScale;
                        rAnim.transform.localScale = new Vector3(Mathf.Abs(aScale.x), aScale.y, aScale.z);
                    }
                }
                // Snap Player
                if (player != null && waypoints.playerSuccessWaypoint != null)
                {
                    player.transform.position = waypoints.playerSuccessWaypoint.position;
                    // Reset scale mirroring
                    Vector3 pScale = player.transform.localScale;
                    player.transform.localScale = new Vector3(Mathf.Abs(pScale.x), pScale.y, pScale.z);
                    var pAnim = player.GetComponentInChildren<IsometricSpriteAnimator>();
                    if (pAnim != null)
                    {
                        Vector3 aScale = pAnim.transform.localScale;
                        pAnim.transform.localScale = new Vector3(Mathf.Abs(aScale.x), aScale.y, aScale.z);
                    }
                }
            }
            else
            {
                // Reset to start transition points
                var seqManager = FindFirstObjectByType<IsometricSequenceManager>();
                if (seqManager != null)
                {
                    if (rodrick != null && seqManager.transitionRodrickWaypoint != null)
                        rodrick.transform.position = seqManager.transitionRodrickWaypoint.position;
                    if (player != null && seqManager.transitionPlayerWaypoint != null)
                        player.transform.position = seqManager.transitionPlayerWaypoint.position;
                }
            }

            // 5. Activate the NPC of the target scenario if one is registered (except scenario 14 where Ronnie should start disabled/asleep until hit)
            ScenarioData data = _scenarios[index];
            NPCSequenceSetup targetNpc = npcSequences.Find(n => n.scenarioIndex == data.scenarioIndex);
            if (targetNpc != null && targetNpc.npcParent != null && data.scenarioIndex != 14)
            {
                targetNpc.npcParent.SetActive(true);
                targetNpc.npcParent.transform.localScale = targetNpc.originalScale;
            }

            // 6. Set active index and Play
            _currentScenarioIndex = index;
            _waitingForChoice = false;
            PlayCurrentScenario();

            // 7. Snap camera follow instantly (skip focusing on Ronnie/Neneng if they aren't supposed to be visible yet)
            if (cameraFollow != null)
            {
                if (targetNpc != null && targetNpc.npcParent != null && data.scenarioIndex != 14)
                {
                    cameraFollow.activeNpcTransform = targetNpc.npcParent.GetComponent<RectTransform>();
                }
                cameraFollow.SnapToTarget();
            }
        }
        #endif

        // Helper class to deserialize lists of JSON scenarios
        [System.Serializable]
        private class ScenarioList
        {
            public List<ScenarioData> scenarios;
        }
    }
}
