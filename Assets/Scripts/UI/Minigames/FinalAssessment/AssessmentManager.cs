using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SectionIntroData
{
    public Sprite panelSprite;      // The full image containing all the text/graphics
    public string startButtonLabel; // e.g. "Start Section 1"
}

[System.Serializable]
public class AssessmentQuestionData
{
    public string id;
    public string category;
    public string english_prompt;
    public string native_context;
    public string correct_answer;
    public string[] wrong_choices;
    public string quiz_type; // "MC", "FIB", "STT", "SB"
}

[System.Serializable]
public class AssessmentDataWrapper
{
    public List<AssessmentQuestionData> questions;
}

public class AssessmentManager : MonoBehaviour
{
    public static AssessmentManager Instance { get; private set; }

    [Header("--- INTRO UI ---")]
    public GameObject introPanel;
    public Image backgroundImage;
    public Sprite ilokanoBackground;
    public Sprite cebuanoBackground;

    private int pendingRewardCoins = 0;

    [Header("--- CATEGORY INTROS ---")]
    public GameObject categoryIntrosGroup;   // The dark overlay (has UIFadeAnimator)
    public GameObject categoryIntroPanel;    // The actual panel GameObject (gets safe pop-in)
    public Image categoryIntroImage;         // The Image component on the panel to swap sprites
    public TextMeshProUGUI startButtonText;

    public SectionIntroData section1Intro;
    public SectionIntroData section2Intro;
    public SectionIntroData section3Intro;

    private Coroutine _introPanelAnim;
    
    [Header("Intro Text & Colors")]
    public TextMeshProUGUI message1Text;
    public TextMeshProUGUI message2Text;
    public string defaultTextColorHex = "#4A3600"; // Default dark brown/black
    public string antingAntingColorHex = "#8A2BE2"; // Purple
    public string ilokanoCrystalColorHex = "#1E90FF"; // Blue
    public string cebuanoCrystalColorHex = "#FF4500"; // Orange/Red

    [Header("--- QUESTION PANELS ---")]
    public GameObject multipleChoicePanel;
    public GameObject fillInBlankPanel;
    public GameObject sttPanel;
    public GameObject sentenceBuilderPanel;

    [Header("--- HOW TO PLAY ---")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;
    private bool hasSeenHowToPlay = false;

    [Header("--- RESULTS UI ---")]
    public GameObject resultsGroup;
    public GameObject resultsPanel;
    public TextMeshProUGUI titleMessageText;
    public TextMeshProUGUI scorePercentageText;
    public Image[] starImages;
    public Sprite starEmptySprite;
    public Sprite starFilledSprite;
    public TextMeshProUGUI outOfStarsText;
    public Slider convSocialBar;
    public TextMeshProUGUI convSocialText;
    public Slider funcNavBar;
    public TextMeshProUGUI funcNavText;
    public Slider grammarBar;
    public TextMeshProUGUI grammarText;
    public TextMeshProUGUI coinRewardText;
    public Button claimContinueButton;

    [Header("--- GENERAL TEST UI ---")]
    public GameObject testGroup;
    public TextMeshProUGUI guideDialogueText;
    public Image correctOrWrongImage;
    public Sprite correctSprite;
    public Sprite wrongSprite;

    [Header("--- AUDIO ---")]
    public AudioSource audioSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;
    public AudioClip nextQuestionSfx;
    public AudioClip winSfx;
    public AudioClip loseSfx;

    [Header("--- MULTIPLE CHOICE ---")]
    public TextMeshProUGUI mcChooseText;
    public TextMeshProUGUI mcQuestionText;
    public Button[] mcChoiceButtons;
    public TextMeshProUGUI[] mcChoiceTexts;
    public Button mcSubmitButton;
    
    [Header("MC Colors")]
    public Color mcDefaultColor = Color.white;
    public Color mcSelectedColor = Color.yellow;
    public Color mcCorrectColor = Color.green;
    public Color mcWrongColor = Color.red;

    private int mcSelectedIndex = -1;
    private int mcCorrectIndex = -1;

    [Header("--- SENTENCE BUILDER ---")]
    public TextMeshProUGUI sbBuildText;
    public TextMeshProUGUI sbQuestionText;
    public Transform sbSentenceBox;
    public Transform sbWordBoxGroup;
    public Button sbSubmitButton;
    public GameObject wordSlotPrefab;
    public GameObject wordBlockPrefab;
    public AudioClip sbDropSfx;
    
    [HideInInspector] public bool isCheckingSBAnswer = false;
    private List<string> sbCorrectWords = new List<string>();

    [Header("--- FILL IN THE BLANKS ---")]
    public TextMeshProUGUI fibChooseText;
    public TextMeshProUGUI fibQuestionText;
    public TextMeshProUGUI fibTranslationText;
    public Button[] fibChoiceButtons;
    public TextMeshProUGUI[] fibChoiceTexts;
    public Button fibSubmitButton;
    
    private int fibSelectedIndex = -1;
    private int fibCorrectIndex = -1;
    private string fibCorrectWord = "";
    private string fibOriginalQuestion = "";

    [Header("--- SPEAK-TO-TEXT (STT) ---")]
    [Header("Question Panel")]
    public TextMeshProUGUI sttReadText;
    public TextMeshProUGUI sttQuestionText;
    public TextMeshProUGUI sttInstructionsText;

    [Header("STT Panel")]
    public TextMeshProUGUI sttStatusText;
    public TextMeshProUGUI sttHeardText;
    public Button sttMicButton;
    public Image sttMicButtonImage;
    public Sprite sttMicNormalSprite;
    public Sprite sttMicActiveSprite;

    [Header("STT Visualizers")]
    public RectTransform[] sttLeftVisualizers;
    public RectTransform[] sttRightVisualizers;
    public float sttVisualizerMaxScale = 1.5f;
    public float sttVisualizerSmoothSpeed = 10f;
    private float[] currentVisualizerScales;

    [Header("STT Tries UI")]
    public Image[] sttTriesImages; // 3 lives
    public Sprite tryUnusedSprite;
    public Sprite tryUsedSprite;

    [Header("STT Colors")]
    public Color sttColorNormal = Color.white;
    public Color sttColorListening = Color.yellow;
    public Color sttColorProcessing = Color.cyan;
    public Color sttColorCorrect = Color.green;
    public Color sttColorWrong = Color.red;
    
    private int sttTries = 3;
    private bool isRecording = false;
    private bool isSTTActive = false;
    private bool isSTTProcessing = false;
    private string sttTargetWord = "";

    [Header("--- PROGRESS UI ---")]
    public AssessmentProgressBar customProgressBar;

    [Header("NPC & Character")]
    public Image npcImage;
    public Image testGroupNpcImage;
    public Sprite kalawSprite;
    public Sprite tiptipSprite;

    [Header("--- NPC DIALOGUE ---")]
    public string[] section1StartDialogues = { "Let's start with conversational basics! Good luck!", "Ready for some basic conversations?", "First up: greetings and identity!" };
    public string[] section2StartDialogues = { "Great job! Now let's test your navigational vocabulary!", "Moving on! Let's see your functional skills.", "Next section: requests and directions!" };
    public string[] section3StartDialogues = { "You're on the final stretch! Let's test your grammar foundations!", "Almost done! Time for grammar and verbs.", "Last section! Show me what you've got!" };
    public string[] defaultEncourageDialogues = { "Take your time and read carefully!", "You're doing great!", "Keep it up!", "Don't rush, you've got this!" };
    public string[] selectAnAnswerDialogues = { "Please select an answer before submitting!", "You need to choose an option first!", "Don't forget to pick an answer!" };
    public string[] correctAnswerDialogues = { "Correct! You're doing amazing!", "Spot on!", "Exactly right!", "Perfect!" };
    public string[] wrongAnswerDialogues = { "Oops, not quite! Keep trying!", "Almost! But not quite.", "That's not it, but good try!", "Incorrect, but don't give up!" };
    public string[] fillAllSlotsDialogues = { "Please fill all the empty slots first!", "Don't leave any blanks!", "Complete the sentence before submitting!" };
    
    public Transform chatBubbleTransform;
    private Coroutine chatBubbleCoroutine;

    [Header("NPC Animations (Sprite Frames)")]
    public float animationFrameRate = 0.1f;
    [Tooltip("Leave arrays empty to just use the default sprite")]
    public Sprite[] kalawHappyFrames;
    public Sprite[] kalawWrongFrames;
    public Sprite[] tiptipHappyFrames;
    public Sprite[] tiptipWrongFrames;

    private Coroutine npcAnimCoroutine;

    [Header("State")]
    private List<AssessmentQuestionData> allQuestions;
    private int currentQuestionIndex = 0;
    private float totalScore = 0f;
    private float convSocialScore = 0f;
    private float convSocialTotal = 0f;
    private float funcNavScore = 0f;
    private float funcNavTotal = 0f;
    private float grammarScore = 0f;
    private float grammarTotal = 0f;
    private string selectedLanguage;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Pulls whatever language the player actually chose in the previous scene
        selectedLanguage = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");

        EnsureDependencies();
        
        if (sttMicButton != null) sttMicButton.onClick.AddListener(OnMicButtonClicked);
        if (claimContinueButton != null) claimContinueButton.onClick.AddListener(ReturnToMap);

        LoadAssessmentData();
        SetupNPC();
        StartCoroutine(IntroSequence());
    }

    private void EnsureDependencies()
    {
        if (SpeechRecorder.Instance == null && FindFirstObjectByType<SpeechRecorder>() == null)
            new GameObject("SpeechRecorder").AddComponent<SpeechRecorder>();

        if (GroqWhisperManager.Instance == null && FindFirstObjectByType<GroqWhisperManager>() == null)
            new GameObject("GroqWhisperManager").AddComponent<GroqWhisperManager>();

        if (PhraseEvaluator.Instance == null && FindFirstObjectByType<PhraseEvaluator>() == null)
            new GameObject("PhraseEvaluator").AddComponent<PhraseEvaluator>();
        
        if (PhraseEvaluator.Instance != null)
        {
            // Set PhraseEvaluator region to match the selected language
            RegionMode mode = selectedLanguage == "Ilokano" ? RegionMode.Ilokano : RegionMode.Cebuano;
            PhraseEvaluator.Instance.SetRegion(mode);
        }
    }

    private void LoadAssessmentData()
    {
        string fileName = selectedLanguage == "Ilokano" ? "FinalAssessment_Ilokano" : "FinalAssessment_Cebuano";
        TextAsset jsonFile = Resources.Load<TextAsset>("Minigames/FinalAssessment/Resources/" + fileName);
        
        // Fallback for direct Resources folder if moved back
        if (jsonFile == null) jsonFile = Resources.Load<TextAsset>(fileName);
        
        if (jsonFile != null)
        {
            AssessmentDataWrapper wrapper = JsonUtility.FromJson<AssessmentDataWrapper>(jsonFile.text);
            
            // Build the 50-question randomized test
            allQuestions = GenerateRandomizedTest(wrapper.questions);
        }
        else
        {
            Debug.LogError("Could not find Assessment Data JSON: " + fileName);
            allQuestions = new List<AssessmentQuestionData>();
        }
    }

    private List<AssessmentQuestionData> GenerateRandomizedTest(List<AssessmentQuestionData> sourceList)
    {
        List<AssessmentQuestionData> section1 = new List<AssessmentQuestionData>();
        List<AssessmentQuestionData> section2 = new List<AssessmentQuestionData>();
        List<AssessmentQuestionData> section3 = new List<AssessmentQuestionData>();

        // Sort into sections
        foreach (var q in sourceList)
        {
            if (q.category == "Greetings" || q.category == "Gratitude" || q.category == "Responses" || q.category == "Identity")
                section1.Add(q);
            else if (q.category == "Requests" || q.category == "Directions" || q.category == "Count")
                section2.Add(q);
            else
                section3.Add(q); // Action Verbs, Linking Verbs, Pronouns, Interrogatives
        }

        // Shuffle sections
        ShuffleList(section1);
        ShuffleList(section2);
        ShuffleList(section3);

        List<AssessmentQuestionData> finalTest = new List<AssessmentQuestionData>();
        
        // Take 15 from Section 1, 15 from Section 2, 20 from Section 3
        finalTest.AddRange(section1.GetRange(0, Mathf.Min(15, section1.Count)));
        finalTest.AddRange(section2.GetRange(0, Mathf.Min(15, section2.Count)));
        finalTest.AddRange(section3.GetRange(0, Mathf.Min(20, section3.Count)));

        return finalTest;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void SetupNPC()
    {
        // Handled dynamically in SetupIntro now
    }

    private IEnumerator IntroSequence()
    {
        HideAllPanels();
        introPanel.SetActive(true);

        // Get player name (assuming you save it in PlayerPrefs, fallback to "Traveler")
        string playerName = PlayerPrefs.GetString("PlayerName", "Traveler"); 

        // Message 1 (Same for both languages, just insert name and color)
        string antingRich = $"<color={antingAntingColorHex}>anting-anting</color>";
        message1Text.text = $"<color={defaultTextColorHex}>You have come a long way, {playerName}! The Great Fading tried to silence our voices, but your {antingRich} glows brighter than ever,</color>";

        // Message 2 & Visuals (Changes based on language)
        if (selectedLanguage == "Ilokano")
        {
            backgroundImage.sprite = ilokanoBackground;
            if (npcImage != null) npcImage.sprite = kalawSprite;
            if (testGroupNpcImage != null) testGroupNpcImage.sprite = kalawSprite;
            
            string crystalRich = $"<color={ilokanoCrystalColorHex}>Ilokano Language Crystal</color>";
            message2Text.text = $"<color={defaultTextColorHex}>Let's see if you can restore the final light to the {crystalRich}!</color>";
        }
        else
        {
            backgroundImage.sprite = cebuanoBackground;
            if (npcImage != null) npcImage.sprite = tiptipSprite;
            if (testGroupNpcImage != null) testGroupNpcImage.sprite = tiptipSprite;
            
            string crystalRich = $"<color={cebuanoCrystalColorHex}>Cebuano Language Crystal</color>";
            message2Text.text = $"<color={defaultTextColorHex}>Let's see if you can restore the final light to the {crystalRich}!</color>";
        }

        yield return null;
    }

    public void StartAssessment()
    {
        introPanel.SetActive(false);

        if (!hasSeenHowToPlay)
        {
            ShowHowToPlay();
            return;
        }

        currentQuestionIndex = 0;
        totalScore = 0;
        ShowNextQuestion();
    }

    public void ShowHowToPlay()
    {
        if (howToPlayGroup != null)
        {
            howToPlayGroup.SetActive(true);
            if (howToPlayGroup.TryGetComponent<UIFadeAnimator>(out var fade)) fade.FadeIn();
            
            if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
        }
    }

    public void CloseHowToPlay()
    {
        if (howToPlayGroup != null)
        {
            if (howToPlayGroup.TryGetComponent<UIFadeAnimator>(out var fade)) fade.FadeOut();
            else howToPlayGroup.SetActive(false);
        }

        if (!hasSeenHowToPlay)
        {
            hasSeenHowToPlay = true;
            // Now actually start the game (triggers Category Intro 1)
            currentQuestionIndex = 0;
            totalScore = 0;
            ShowNextQuestion();
        }
    }

    private void ShowNextQuestion()
    {
        HideAllPanels();

        if (currentQuestionIndex >= allQuestions.Count)
        {
            ShowResults();
            return;
        }

        UpdateProgressUI();
        AssessmentQuestionData currentQ = allQuestions[currentQuestionIndex];
        
        // NPC Dialogue Logic
        if (currentQuestionIndex == 0) ShowChatBubble(GetRandomDialogue(section1StartDialogues));
        else if (currentQuestionIndex == 15) ShowChatBubble(GetRandomDialogue(section2StartDialogues));
        else if (currentQuestionIndex == 30) ShowChatBubble(GetRandomDialogue(section3StartDialogues));
        else ShowChatBubble(GetRandomDialogue(defaultEncourageDialogues));

        // Show Category Intro panel at the start of each section
        if (currentQuestionIndex == 0) { ShowCategoryIntro(1); return; }
        if (currentQuestionIndex == 15) { ShowCategoryIntro(2); return; }
        if (currentQuestionIndex == 30) { ShowCategoryIntro(3); return; }

        // Activate the correct panel based on quiz type
        switch (currentQ.quiz_type)
        {
            case "MC":
                ActivatePanel(multipleChoicePanel);
                SetupMultipleChoice(currentQ);
                break;
            case "FIB":
                ActivatePanel(fillInBlankPanel);
                SetupFillInBlank(currentQ);
                break;
            case "STT":
                ActivatePanel(sttPanel);
                SetupSTT(currentQ);
                break;
            case "SB":
                ActivatePanel(sentenceBuilderPanel);
                SetupSentenceBuilder(currentQ);
                break;
            default:
                Debug.LogWarning("Unknown quiz type: " + currentQ.quiz_type);
                break;
        }
    }

    private string FormatUnderlinedText(string text, string hexColor)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.Contains("<u>"))
        {
            text = text.Replace("<u>", $"<color={hexColor}><u>");
            text = text.Replace("</u>", "</u></color>");
        }
        return text;
    }

    private void UpdateProgressUI()
    {
        if (customProgressBar != null)
        {
            customProgressBar.UpdateProgress(currentQuestionIndex);
        }
    }

    // --- CATEGORY INTRO ---
    private void ShowCategoryIntro(int section)
    {
        SectionIntroData data = section == 1 ? section1Intro : (section == 2 ? section2Intro : section3Intro);

        if (categoryIntroImage != null && data.panelSprite != null)
        {
            categoryIntroImage.sprite = data.panelSprite;
        }
        
        if (startButtonText != null)
        {
            startButtonText.text = data.startButtonLabel;
        }

        // Hide the test group while intro is showing
        if (testGroup) testGroup.SetActive(false);

        // Fade in the dark overlay
        if (categoryIntrosGroup != null)
        {
            categoryIntrosGroup.SetActive(true);
            categoryIntrosGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
        }

        // Safe pop-in for the panel
        if (categoryIntroPanel != null)
        {
            if (_introPanelAnim != null) StopCoroutine(_introPanelAnim);
            _introPanelAnim = StartCoroutine(SafeIntroPanelPop());
        }
    }

    private IEnumerator SafeIntroPanelPop()
    {
        categoryIntroPanel.SetActive(true);
        categoryIntroPanel.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            categoryIntroPanel.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.1f, elapsed / 0.15f);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            categoryIntroPanel.transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, elapsed / 0.15f);
            yield return null;
        }
        categoryIntroPanel.transform.localScale = Vector3.one;
        _introPanelAnim = null;
    }

    // Called by the StartButton inside CategoryIntros
    public void StartSection()
    {
        if (categoryIntrosGroup) categoryIntrosGroup.SetActive(false);

        // Now actually show the question
        AssessmentQuestionData currentQ = allQuestions[currentQuestionIndex];
        switch (currentQ.quiz_type)
        {
            case "MC": ActivatePanel(multipleChoicePanel); SetupMultipleChoice(currentQ); break;
            case "FIB": ActivatePanel(fillInBlankPanel); SetupFillInBlank(currentQ); break;
            case "SB": ActivatePanel(sentenceBuilderPanel); SetupSentenceBuilder(currentQ); break;
            case "STT": ActivatePanel(sttPanel); SetupSTT(currentQ); break;
            default: ActivatePanel(multipleChoicePanel); SetupMultipleChoice(currentQ); break;
        }
    }

    private void ActivatePanel(GameObject target)
    {
        if (multipleChoicePanel) multipleChoicePanel.SetActive(false);
        if (fillInBlankPanel) fillInBlankPanel.SetActive(false);
        if (sentenceBuilderPanel) sentenceBuilderPanel.SetActive(false);
        if (sttPanel) sttPanel.SetActive(false);
        if (target) target.SetActive(true);
        if (testGroup) testGroup.SetActive(true);
    }

    // --- SETUP METHODS FOR PANELS ---
    private void SetupMultipleChoice(AssessmentQuestionData qData) 
    { 
        testGroup.SetActive(true);
        
        string langColor = selectedLanguage == "Ilokano" ? ilokanoCrystalColorHex : cebuanoCrystalColorHex;
        mcChooseText.text = $"Choose the correct <color={langColor}>{selectedLanguage}</color> phrase for the situation.";
        mcQuestionText.text = FormatUnderlinedText(qData.english_prompt, langColor);

        // Build choice list (Correct + up to 3 Wrongs)
        List<string> choices = new List<string>();
        choices.Add(qData.correct_answer);
        
        List<string> shuffledWrongs = new List<string>(qData.wrong_choices);
        ShuffleList(shuffledWrongs);
        
        for (int i = 0; i < Mathf.Min(3, shuffledWrongs.Count); i++)
        {
            choices.Add(shuffledWrongs[i]);
        }
        
        ShuffleList(choices);
        
        // Setup UI Buttons
        mcSelectedIndex = -1;
        if(mcSubmitButton) mcSubmitButton.interactable = false;
        
        for (int i = 0; i < mcChoiceButtons.Length; i++)
        {
            if (i < choices.Count)
            {
                mcChoiceButtons[i].gameObject.SetActive(true);
                mcChoiceButtons[i].interactable = true; // Re-enable if disabled
                mcChoiceTexts[i].text = choices[i];
                mcChoiceButtons[i].image.color = mcDefaultColor;
                
                if (choices[i] == qData.correct_answer)
                {
                    mcCorrectIndex = i;
                }
            }
            else
            {
                mcChoiceButtons[i].gameObject.SetActive(false); // Hide extra buttons
            }
        }
    }

    public void SelectMCChoice(int index)
    {
        mcSelectedIndex = index;
        if(mcSubmitButton) mcSubmitButton.interactable = true;
        
        for (int i = 0; i < mcChoiceButtons.Length; i++)
        {
            if(mcChoiceButtons[i].gameObject.activeSelf)
                mcChoiceButtons[i].image.color = (i == index) ? mcSelectedColor : mcDefaultColor;
        }
    }

    public void SubmitMCAnswer()
    {
        if (mcSelectedIndex == -1) 
        {
            ShowChatBubble(GetRandomDialogue(selectAnAnswerDialogues));
            return;
        }
        
        if(mcSubmitButton) mcSubmitButton.interactable = false;
        foreach(var btn in mcChoiceButtons) btn.interactable = false; // Prevent double clicking
        
        bool isCorrect = (mcSelectedIndex == mcCorrectIndex);
        
        if (isCorrect)
        {
            mcChoiceButtons[mcSelectedIndex].image.color = mcCorrectColor;
            PlaySFX(correctSfx);
            StartCoroutine(ShowCorrectOrWrongPopup(true));
        }
        else
        {
            mcChoiceButtons[mcSelectedIndex].image.color = mcWrongColor;
            mcChoiceButtons[mcCorrectIndex].image.color = mcCorrectColor; // Reveal the correct one
            PlaySFX(wrongSfx);
            StartCoroutine(ShowCorrectOrWrongPopup(false));
        }
        
        OnAnswerSubmitted(isCorrect);
    }
    private void SetupFillInBlank(AssessmentQuestionData qData) 
    { 
        testGroup.SetActive(true);
        
        string langColor = selectedLanguage == "Ilokano" ? ilokanoCrystalColorHex : cebuanoCrystalColorHex;
        fibChooseText.text = $"Choose the correct <color={langColor}>{selectedLanguage}</color> word to complete the sentence.";
        
        fibOriginalQuestion = string.IsNullOrWhiteSpace(qData.native_context) ? "___" : qData.native_context;
        fibQuestionText.text = fibOriginalQuestion;
        string parsedPrompt = qData.english_prompt
            .Replace("{name}", "(your name)")
            .Replace("<username>", "(your name)")
            .Replace("{place}", "(your place)");
        fibTranslationText.text = $"({FormatUnderlinedText(parsedPrompt, langColor)})";
        fibCorrectWord = qData.correct_answer;

        List<string> choices = new List<string>();
        choices.Add(qData.correct_answer);
        
        List<string> shuffledWrongs = new List<string>(qData.wrong_choices);
        ShuffleList(shuffledWrongs);
        
        for (int i = 0; i < Mathf.Min(3, shuffledWrongs.Count); i++)
        {
            choices.Add(shuffledWrongs[i]);
        }
        
        ShuffleList(choices);
        
        fibSelectedIndex = -1;
        if(fibSubmitButton) fibSubmitButton.interactable = false;
        
        for (int i = 0; i < fibChoiceButtons.Length; i++)
        {
            if (i < choices.Count)
            {
                fibChoiceButtons[i].gameObject.SetActive(true);
                fibChoiceButtons[i].interactable = true;
                fibChoiceTexts[i].text = choices[i];
                fibChoiceButtons[i].image.color = mcDefaultColor; // re-use MC colors
                
                if (choices[i] == qData.correct_answer)
                {
                    fibCorrectIndex = i;
                }
            }
            else
            {
                fibChoiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectFIBChoice(int index)
    {
        fibSelectedIndex = index;
        if(fibSubmitButton) fibSubmitButton.interactable = true;
        
        for (int i = 0; i < fibChoiceButtons.Length; i++)
        {
            if(fibChoiceButtons[i].gameObject.activeSelf)
                fibChoiceButtons[i].image.color = (i == index) ? mcSelectedColor : mcDefaultColor;
        }
    }

    public void SubmitFIBAnswer()
    {
        if (fibSelectedIndex == -1) 
        {
            ShowChatBubble(GetRandomDialogue(selectAnAnswerDialogues));
            return;
        }
        
        if(fibSubmitButton) fibSubmitButton.interactable = false;
        foreach(var btn in fibChoiceButtons) btn.interactable = false; 
        
        bool isCorrect = (fibSelectedIndex == fibCorrectIndex);
        
        // Replace the blank (any number of underscores) with the correct word in green
        string coloredAnswer = $"<color=#{ColorUtility.ToHtmlStringRGB(mcCorrectColor)}>{fibCorrectWord}</color>";
        fibQuestionText.text = System.Text.RegularExpressions.Regex.Replace(fibOriginalQuestion, @"_+", coloredAnswer);
        
        if (isCorrect)
        {
            fibChoiceButtons[fibSelectedIndex].image.color = mcCorrectColor;
            PlaySFX(correctSfx);
            StartCoroutine(ShowCorrectOrWrongPopup(true));
        }
        else
        {
            fibChoiceButtons[fibSelectedIndex].image.color = mcWrongColor;
            fibChoiceButtons[fibCorrectIndex].image.color = mcCorrectColor; // Reveal the correct one
            PlaySFX(wrongSfx);
            StartCoroutine(ShowCorrectOrWrongPopup(false));
        }
        
        OnAnswerSubmitted(isCorrect);
    }

    private void SetupSTT(AssessmentQuestionData qData) 
    {
        testGroup.SetActive(true);
        isSTTActive = true;
        isSTTProcessing = false;
        sttTries = 3;
        isRecording = false;
        sttTargetWord = qData.correct_answer;
        
        if (sttMicButton != null) sttMicButton.interactable = true;

        string langColor = selectedLanguage == "Ilokano" ? ilokanoCrystalColorHex : cebuanoCrystalColorHex;
        
        if (qData.english_prompt.Contains("<u>"))
        {
            if (sttReadText) sttReadText.text = $"Translate the underlined word into <color={langColor}>{selectedLanguage}</color> out loud";
            if (sttQuestionText) sttQuestionText.text = $"\"{FormatUnderlinedText(qData.english_prompt, langColor)}\"";
            if (sttInstructionsText) sttInstructionsText.text = $"What is the translation for the underlined word?";
        }
        else
        {
            if (sttReadText) sttReadText.text = $"Read the situation, then say the <color={langColor}>{selectedLanguage}</color> phrase out loud";
            if (sttQuestionText) sttQuestionText.text = $"You want to say: \"<color={langColor}>{qData.english_prompt}</color>\"";
            if (sttInstructionsText) sttInstructionsText.text = $"How do you say it in <color={langColor}>{selectedLanguage}</color>?";
        }
        
        if (sttStatusText) 
        {
            sttStatusText.text = "Tap the mic to speak";
            sttStatusText.color = sttColorNormal;
        }
        if (sttHeardText) sttHeardText.text = "";
        
        if (sttMicButtonImage) sttMicButtonImage.sprite = sttMicNormalSprite;

        // Reset Tries UI
        for (int i = 0; i < sttTriesImages.Length; i++)
        {
            if (sttTriesImages[i] != null)
                sttTriesImages[i].sprite = tryUnusedSprite;
        }
        
        // Init visualizer scales
        if (sttLeftVisualizers != null)
        {
            currentVisualizerScales = new float[sttLeftVisualizers.Length];
            for(int i=0; i < currentVisualizerScales.Length; i++) 
            {
                currentVisualizerScales[i] = 0.2f;
                Vector3 minScale = new Vector3(1f, 0.2f, 1f);
                if (sttLeftVisualizers[i] != null) sttLeftVisualizers[i].localScale = minScale;
                if (sttRightVisualizers != null && i < sttRightVisualizers.Length && sttRightVisualizers[i] != null)
                {
                    sttRightVisualizers[i].localScale = minScale;
                }
            }
        }
    }

    private void OnMicButtonClicked()
    {
        if (!isSTTActive || isSTTProcessing) return;
        
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        isRecording = true;
        if (sttMicButtonImage != null) sttMicButtonImage.sprite = sttMicActiveSprite;
        if (sttStatusText != null) 
        {
            sttStatusText.text = "Listening... Tap Mic to Stop.";
            sttStatusText.color = sttColorListening;
        }
        
        if (SpeechRecorder.Instance != null)
            SpeechRecorder.Instance.StartRecording();
    }

    private void StopRecording()
    {
        isRecording = false;
        isSTTProcessing = true;
        
        if (sttMicButton != null) sttMicButton.interactable = false; // Disable while processing
        if (sttMicButtonImage != null) sttMicButtonImage.sprite = sttMicNormalSprite;
        
        if (sttStatusText != null) 
        {
            sttStatusText.text = "Processing...";
            sttStatusText.color = sttColorProcessing;
        }
        
        if (SpeechRecorder.Instance != null)
        {
            string filePath = SpeechRecorder.Instance.StopRecording();
            if (!string.IsNullOrEmpty(filePath) && GroqWhisperManager.Instance != null)
            {
                GroqWhisperManager.Instance.Transcribe(filePath, 
                    onSuccess: (text) => OnTranscriptionComplete(text),
                    onError: (error) => OnTranscriptionComplete("")
                );
            }
            else
            {
                OnTranscriptionComplete("");
            }
        }
    }

    private void OnTranscriptionComplete(string text)
    {
        if (!isSTTActive) return;

        if (string.IsNullOrEmpty(text) || text.Trim() == "")
        {
            isSTTProcessing = false;
            if (sttMicButton != null) sttMicButton.interactable = true; // Re-enable
            
            if (sttStatusText != null) 
            {
                sttStatusText.text = "Couldn't hear you. Try again!";
                sttStatusText.color = sttColorWrong;
            }
            return;
        }

        if (sttHeardText != null) sttHeardText.text = $"Heard: \"{text}\"";

        // Explicit STT alias override for mopabilin / magpabilin
        string lowerText = text.ToLower();
        if ((sttTargetWord.ToLower() == "mopabilin" && lowerText.Contains("magpabilin")) ||
            (sttTargetWord.ToLower() == "magpabilin" && lowerText.Contains("mopabilin")))
        {
            OnPhraseEvaluated(text, 100f, "success");
            return;
        }

        if (PhraseEvaluator.Instance != null)
        {
            // Evaluate against our exact JSON answer
            PhraseEvaluator.Instance.EvaluateSpeech(sttTargetWord, text, OnPhraseEvaluated);
        }
    }

    private void OnPhraseEvaluated(string transcribedText, float score, string result)
    {
        if (!isSTTActive) return;

        bool isCorrect = (score >= 70f); // Score is 0-100 from EvaluateSpeech
        if (isCorrect)
        {
            isSTTActive = false; // Disable so they can't click mic again
            isSTTProcessing = false;
            
            if (sttStatusText != null)
            {
                sttStatusText.text = "Correct!";
                sttStatusText.color = sttColorCorrect;
            }
            PlaySFX(correctSfx);
            StartCoroutine(ShowCorrectOrWrongPopup(true));
            OnAnswerSubmitted(true);
        }
        else
        {
            sttTries--;
            
            // Update Tries UI
            if (sttTries >= 0 && sttTries < sttTriesImages.Length)
            {
                if (sttTriesImages[sttTries] != null) sttTriesImages[sttTries].sprite = tryUsedSprite;
            }

            if (sttTries <= 0)
            {
                // Out of tries! Fail gracefully
                isSTTActive = false;
                isSTTProcessing = false;
                
                if (sttStatusText != null)
                {
                    sttStatusText.text = $"Correct answer was:\n\"{sttTargetWord}\"";
                    sttStatusText.color = sttColorWrong;
                }
                PlaySFX(wrongSfx);
                
                StartCoroutine(ShowCorrectOrWrongPopup(false));
                OnAnswerSubmitted(false);
            }
            else
            {
                // Try again
                isSTTProcessing = false;
                if (sttMicButton != null) sttMicButton.interactable = true; // Re-enable
                
                if (sttStatusText != null)
                {
                    sttStatusText.text = $"Not quite! You have {sttTries} tries left.";
                    sttStatusText.color = sttColorWrong;
                }
                PlaySFX(wrongSfx);
            }
        }
    }
    
    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            // Shift + P = Skip Entire Assessment
            if ((UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed) && 
                UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[CHEAT] Skipping Entire Assessment!</color>");
                if (isRecording) StopRecording();
                
                // Max out scores
                totalScore = allQuestions.Count;
                convSocialScore = 15;
                convSocialTotal = 15;
                funcNavScore = 15;
                funcNavTotal = 15;
                grammarScore = 20;
                grammarTotal = 20;
                
                // End test
                currentQuestionIndex = allQuestions.Count;
                HideAllPanels();
                ShowResults();
            }
            // P = Skip Current Question
            else if (UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
            {
                if (isRecording) StopRecording();
                Debug.Log("<color=yellow>[CHEAT] Bypassing Question via P key!</color>");
                OnAnswerSubmitted(true, 1f);
            }
        }
#endif

        // Handle STT Visualizers
        if (isSTTActive && sttLeftVisualizers != null && sttLeftVisualizers.Length > 0 && currentVisualizerScales != null)
        {
            float targetScale = 0.2f;
            if (isRecording && SpeechRecorder.Instance != null)
            {
                float vol = SpeechRecorder.Instance.GetMicVolume();
                // Amplify and clamp the volume to make it visible, mapping to 0.2 -> 1.0
                targetScale = 0.2f + Mathf.Clamp01(vol * 50f) * 0.8f;
            }
            
            for (int i = 0; i < sttLeftVisualizers.Length; i++)
            {
                // Add a small offset based on index to create a wave effect
                float delayedTarget = 0.2f;
                if (isRecording)
                {
                    float noise = Mathf.PerlinNoise(Time.time * 5f + i * 0.2f, 0f);
                    delayedTarget = 0.2f + (targetScale - 0.2f) * noise;
                }
                
                currentVisualizerScales[i] = Mathf.Lerp(currentVisualizerScales[i], delayedTarget, Time.deltaTime * sttVisualizerSmoothSpeed);
                
                Vector3 scale = new Vector3(1f, currentVisualizerScales[i], 1f);
                if (sttLeftVisualizers[i] != null) sttLeftVisualizers[i].localScale = scale;
                
                // Mirror to right visualizers if they exist and are same length
                if (sttRightVisualizers != null && i < sttRightVisualizers.Length && sttRightVisualizers[i] != null)
                {
                    sttRightVisualizers[i].localScale = scale;
                }
            }
        }
    }
    private void SetupSentenceBuilder(AssessmentQuestionData qData) 
    {
        testGroup.SetActive(true);
        isCheckingSBAnswer = false;
        // Submit button is always enabled — NPC will react if slots aren't filled

        string langColor = selectedLanguage == "Ilokano" ? ilokanoCrystalColorHex : cebuanoCrystalColorHex;
        
        string parsedPrompt = qData.english_prompt
            .Replace("{name}", "(your name)")
            .Replace("<username>", "(your name)")
            .Replace("{place}", "(your place)");

        if (parsedPrompt.Contains("<u>"))
        {
            sbBuildText.text = $"Translate the underlined word into <color={langColor}>{selectedLanguage}</color>.";
            sbQuestionText.text = $"<color={defaultTextColorHex}>Translate: {FormatUnderlinedText(parsedPrompt, langColor)}</color>";
        }
        else
        {
            sbBuildText.text = $"Build the correct <color={langColor}>{selectedLanguage}</color> sentence.";
            sbQuestionText.text = $"<color={defaultTextColorHex}>Translate: <color={langColor}>{parsedPrompt}</color></color>";
        }

        // Cleanup old blocks/slots
        foreach (Transform child in sbSentenceBox) Destroy(child.gameObject);
        foreach (Transform child in sbWordBoxGroup) Destroy(child.gameObject);

        // Split the correct answer into individual words, preserving {name} for the InputField block
        sbCorrectWords = new List<string>(qData.correct_answer.Trim().Split(' '));
        sbCorrectWords.RemoveAll(w => string.IsNullOrWhiteSpace(w)); // Safety cleanup

        // Spawn one empty slot per word
        for (int i = 0; i < sbCorrectWords.Count; i++)
        {
            Instantiate(wordSlotPrefab, sbSentenceBox);
        }

        // Ensure the submit button is enabled
        if (sbSubmitButton != null) sbSubmitButton.interactable = true;

        // The word BANK is just the correct words shuffled (sentence scramble style)
        // wrong_choices for SB are full sentences, so we extract random distractor words from them
        List<string> shuffledBank = new List<string>(sbCorrectWords);
        
        List<string> potentialDistractors = new List<string>();
        if (qData.wrong_choices != null && qData.wrong_choices.Length > 0)
        {
            foreach (string wrongSentence in qData.wrong_choices)
            {
                string[] words = wrongSentence.Split(' ');
                foreach (string w in words)
                {
                    string cleanWord = w.Trim();
                    bool alreadyInBank = false;
                    foreach(string bankWord in shuffledBank)
                    {
                        if (bankWord.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyInBank = true;
                            break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(cleanWord) && 
                        !alreadyInBank && 
                        !potentialDistractors.Contains(cleanWord))
                    {
                        potentialDistractors.Add(cleanWord);
                    }
                }
            }
            
            // Calculate how many distractors we can add to keep total choices <= 6
            int maxDistractors = Mathf.Max(0, 6 - sbCorrectWords.Count);
            int distractorsToAdd = Mathf.Min(maxDistractors, potentialDistractors.Count);

            ShuffleList(potentialDistractors);
            for (int i = 0; i < distractorsToAdd; i++)
            {
                shuffledBank.Add(potentialDistractors[i]);
            }
            Debug.Log($"[SentenceBuilder] Added {distractorsToAdd} distractors. Total Bank: {shuffledBank.Count}");
        }
        else
        {
            Debug.LogWarning($"[SentenceBuilder] No wrong choices found in JSON for ID: {qData.id}");
        }

        ShuffleList(shuffledBank);

        // Spawn draggable word blocks
        foreach (string word in shuffledBank)
        {
            GameObject blockObj = Instantiate(wordBlockPrefab, sbWordBoxGroup);
            AssessmentWordBlock block = blockObj.GetComponent<AssessmentWordBlock>();
            if (block != null) block.SetWord(word);
        }
    }

    public void OnSBSlotChanged()
    {
        // No button toggling here — button stays always enabled.
        // We just notify so future logic can hook in if needed.
    }

    public void PlaySBDropSfx()
    {
        if (audioSource && sbDropSfx)
        {
            PlaySFX(sbDropSfx);
        }
    }

    public void SubmitSBAnswer()
    {
        if (isCheckingSBAnswer) return;

        // Collect player's answer from slots — NPC reacts if any slot is empty
        List<string> playerWords = new List<string>();
        foreach (Transform slotObj in sbSentenceBox)
        {
            AssessmentWordSlot slot = slotObj.GetComponent<AssessmentWordSlot>();
            if (slot == null || slot.CurrentBlock == null)
            {
                ShowChatBubble(GetRandomDialogue(fillAllSlotsDialogues));
                return;
            }
            
            string currentWord = slot.CurrentBlock.CurrentWord;
            if (string.IsNullOrWhiteSpace(currentWord) || currentWord.ToLower().StartsWith("(type"))
            {
                ShowChatBubble("Please type in your answer in the text block first!");
                return;
            }
            
            playerWords.Add(currentWord);
        }

        isCheckingSBAnswer = true;
        if (sbSubmitButton) sbSubmitButton.interactable = false;

        bool isCorrect = true;
        string expectedName = PlayerPrefs.GetString("PlayerName", "Traveler").ToLower();
        string expectedPlace = PlayerPrefs.GetString("PlayerPlace", "Cebu").ToLower();

        for (int i = 0; i < sbCorrectWords.Count; i++)
        {
            if (i >= playerWords.Count)
            {
                isCorrect = false;
                break;
            }

            string targetWord = sbCorrectWords[i].ToLower().Trim();
            string playerWord = playerWords[i].ToLower().Trim();

            // Validate placeholder InputField blocks (accept ANY typed text)
            if ((targetWord.StartsWith("{") && targetWord.EndsWith("}")) || targetWord == "<username>")
            {
                // Already checked that they typed something in the block above, so accept it!
                continue;
            }
            else if (playerWord != targetWord)
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            PlaySFX(correctSfx);
            StartCoroutine(ShowCorrectOrWrongPopup(true));
        }
        else
        {
            PlaySFX(wrongSfx);
            StartCoroutine(ShowCorrectOrWrongPopup(false));
        }

        OnAnswerSubmitted(isCorrect);
    }

    // --- SUBMISSION LOGIC ---
    public void OnAnswerSubmitted(bool isCorrect, float pointsAwarded = 1f)
    {
        AssessmentQuestionData currentQ = allQuestions[currentQuestionIndex];
        string cat = currentQ.category;
        
        if (cat == "Greetings" || cat == "Gratitude" || cat == "Responses" || cat == "Identity") {
            convSocialTotal += 1f;
            if (isCorrect) convSocialScore += pointsAwarded;
        }
        else if (cat == "Requests" || cat == "Directions" || cat == "Count") {
            funcNavTotal += 1f;
            if (isCorrect) funcNavScore += pointsAwarded;
        }
        else {
            grammarTotal += 1f;
            if (isCorrect) grammarScore += pointsAwarded;
        }

        ShowChatBubble(isCorrect ? GetRandomDialogue(correctAnswerDialogues) : GetRandomDialogue(wrongAnswerDialogues));

        if (isCorrect)
        {
            totalScore += pointsAwarded;
            PlayNPCAnimation(true);
        }
        else
        {
            PlayNPCAnimation(false);
        }

        currentQuestionIndex++;
        
        // Brief pause for NPC reaction before next question
        StartCoroutine(TransitionToNextQuestion());
    }

    private void PlayNPCAnimation(bool isHappy)
    {
        if (npcAnimCoroutine != null) StopCoroutine(npcAnimCoroutine);
        
        Sprite[] framesToPlay = null;
        Sprite defaultSprite = selectedLanguage == "Ilokano" ? kalawSprite : tiptipSprite;

        if (selectedLanguage == "Ilokano")
        {
            framesToPlay = isHappy ? kalawHappyFrames : kalawWrongFrames;
        }
        else
        {
            framesToPlay = isHappy ? tiptipHappyFrames : tiptipWrongFrames;
        }

        // If there are frames, play them. Otherwise, it just stays on the default sprite.
        if (framesToPlay != null && framesToPlay.Length > 0)
        {
            npcAnimCoroutine = StartCoroutine(AnimateNPCSprite(framesToPlay, defaultSprite));
        }
    }

    private IEnumerator AnimateNPCSprite(Sprite[] frames, Sprite defaultSprite)
    {
        foreach (Sprite frame in frames)
        {
            npcImage.sprite = frame;
            yield return new WaitForSeconds(animationFrameRate);
        }
        // Return to default
        npcImage.sprite = defaultSprite;
    }

    private IEnumerator ShowCorrectOrWrongPopup(bool isCorrect)
    {
        if (correctOrWrongImage != null)
        {
            correctOrWrongImage.gameObject.SetActive(true);
            correctOrWrongImage.sprite = isCorrect ? correctSprite : wrongSprite;
            
            // Safe Pop In
            correctOrWrongImage.transform.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                correctOrWrongImage.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.1f, elapsed / 0.15f);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                correctOrWrongImage.transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, elapsed / 0.15f);
                yield return null;
            }
            correctOrWrongImage.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(1.5f);
            correctOrWrongImage.gameObject.SetActive(false);
        }
    }

    private void ShowChatBubble(string text)
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Traveler");
        string parsedText = text.Replace("<username>", playerName);
        
        if (guideDialogueText != null) guideDialogueText.text = parsedText;
        if (chatBubbleTransform != null)
        {
            if (chatBubbleCoroutine != null) StopCoroutine(chatBubbleCoroutine);
            chatBubbleCoroutine = StartCoroutine(ChatBubbleRoutine());
        }
    }

    private string GetRandomDialogue(string[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) return "";
        return dialogues[Random.Range(0, dialogues.Length)];
    }

    private IEnumerator ChatBubbleRoutine()
    {
        chatBubbleTransform.gameObject.SetActive(true);
        chatBubbleTransform.localScale = Vector3.zero;
        
        // Pop In
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            chatBubbleTransform.localScale = Vector3.one * Mathf.Lerp(0f, 1.1f, elapsed / 0.15f);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            chatBubbleTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, elapsed / 0.15f);
            yield return null;
        }
        chatBubbleTransform.localScale = Vector3.one;

        // Wait
        yield return new WaitForSeconds(3f);

        // Pop Out
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            chatBubbleTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0f, elapsed / 0.15f);
            yield return null;
        }
        chatBubbleTransform.localScale = Vector3.zero;
        chatBubbleTransform.gameObject.SetActive(false);
    }

    private IEnumerator TransitionToNextQuestion()
    {
        // Disable interaction during transition
        yield return new WaitForSeconds(1.5f);
        
        PlaySFX(nextQuestionSfx);

        
        // Optional: Check if we are crossing a section boundary (e.g. Q# 22 -> 23)
        // If so, show TransitionPanel for a few seconds.
        
        ShowNextQuestion();
    }

    private void ShowResults()
    {
        HideAllPanels();
        testGroup.SetActive(false);
        if (resultsGroup != null) resultsGroup.SetActive(true);
        if (resultsPanel != null) resultsPanel.SetActive(true);
        
        if (resultsGroup != null)
        {
            if (resultsGroup.TryGetComponent<UIFadeAnimator>(out var fade)) 
            {
                fade.FadeIn();
            }
            else if (resultsGroup.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg.alpha = 1f;
            }
        }

        float totalPerc = (totalScore / (float)allQuestions.Count) * 100f;
        int roundedPerc = Mathf.RoundToInt(totalPerc);
        if (scorePercentageText != null) scorePercentageText.text = $"{roundedPerc}%";

        string pName = PlayerPrefs.GetString("PlayerName", "Traveler");
        if (titleMessageText != null) titleMessageText.text = $"Amazing effort, {pName}!";

        int stars = 0;
        if (roundedPerc >= 20) stars = 1;
        if (roundedPerc >= 40) stars = 2;
        if (roundedPerc >= 60) stars = 3;
        if (roundedPerc >= 75) stars = 4;
        if (roundedPerc >= 90) stars = 5;

        if (stars > 0) PlaySFX(winSfx);
        else if (stars == 0) PlaySFX(loseSfx);

        if (starImages != null)
        {
            for(int i = 0; i < starImages.Length; i++) 
            {
                starImages[i].sprite = (i < stars) ? starFilledSprite : starEmptySprite;
            }
        }
        if (outOfStarsText != null) outOfStarsText.text = $"{stars} out of 5 stars";

        int coins = 10;
        if (roundedPerc >= 40) coins = 30;
        if (roundedPerc >= 60) coins = 60;
        if (roundedPerc >= 75) coins = 100;
        if (roundedPerc >= 90) coins = 150;

        if (coinRewardText != null) coinRewardText.text = coins.ToString();

        pendingRewardCoins = coins;

        // Update category breakdown
        if (convSocialBar != null) convSocialBar.value = (convSocialTotal > 0) ? (convSocialScore / convSocialTotal) : 0f;
        if (convSocialText != null) convSocialText.text = $"{(convSocialTotal > 0 ? Mathf.RoundToInt((convSocialScore / convSocialTotal) * 100f) : 0)}%";

        if (funcNavBar != null) funcNavBar.value = (funcNavTotal > 0) ? (funcNavScore / funcNavTotal) : 0f;
        if (funcNavText != null) funcNavText.text = $"{(funcNavTotal > 0 ? Mathf.RoundToInt((funcNavScore / funcNavTotal) * 100f) : 0)}%";

        if (grammarBar != null) grammarBar.value = (grammarTotal > 0) ? (grammarScore / grammarTotal) : 0f;
        if (grammarText != null) grammarText.text = $"{(grammarTotal > 0 ? Mathf.RoundToInt((grammarScore / grammarTotal) * 100f) : 0)}%";

        PlayerPrefs.Save();
    }

    public void ReturnToMap()
    {
        if (resultsPanel != null && resultsPanel.activeSelf && pendingRewardCoins > 0)
        {
            if (UserProfileManager.Instance != null) _ = UserProfileManager.Instance.AddCoins(pendingRewardCoins);
            pendingRewardCoins = 0;
        }
        PlaySFX(nextQuestionSfx);
        PlayerPrefs.SetInt("FinalAssessment_Completed", 1);
        
        string selectedLanguage = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        
        // Complete the final assessment objective in the database!
        if (ObjectiveManager.Instance != null)
        {
            if (selectedLanguage == "Ilokano")
                ObjectiveManager.Instance.CompleteObjective("ilo_25");
            else if (selectedLanguage == "Cebuano")
                ObjectiveManager.Instance.CompleteObjective("ceb_29");
        }

        PlayerPrefs.Save();
        
        string targetScene = selectedLanguage + "Ending"; // CebuanoEnding or IlokanoEnding
        
        SceneLoader.ResetLoadingFlag(); 
        SceneLoader.targetSceneForLoading = targetScene; 
        SceneLoader.keepBackgroundPersistent = false; 
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
    }

    private void HideAllPanels()
    {
        if (introPanel != null) introPanel.SetActive(false);
        if (multipleChoicePanel != null) multipleChoicePanel.SetActive(false);
        if (fillInBlankPanel != null) fillInBlankPanel.SetActive(false);
        if (sttPanel != null) sttPanel.SetActive(false);
        if (sentenceBuilderPanel != null) sentenceBuilderPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
    }
}
