using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GreetingQuizData
{
    public string id;
    public string english;
    public string scenario;
}

public class QuestionData
{
    public GreetingQuizData greeting;
    public bool isScenario;
}

public class FishingQuizManager : MonoBehaviour
{
    public static FishingQuizManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI baitCountText;
    public TextMeshProUGUI roundCountText;

    [Header("Feedback Popup")]
    public UnityEngine.UI.Image feedbackImage;
    public Sprite correctSprite;
    public Sprite wrongSprite;
    public float feedbackDuration = 1.5f;

    [Header("Sound Effects")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSFX;
    public AudioClip winPanelSFX;
    public AudioClip losePanelSFX;
    public AudioClip correctAnswerSFX; // Plays 2 seconds after catching the right fish

    [Header("Win/Lose UI")]
    public GameObject winOrLoseGroup;
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI winCoinsText;
    public TextMeshProUGUI loseCoinsText;
    public UnityEngine.UI.Image[] winStars; // Assign the 5 star images from WinPanel's StarsGroup
    public Sprite activeStarSprite;
    public Sprite inactiveStarSprite;

    [Header("How To Play UI")]
    public GameObject howToPlayGroup;
    public GameObject howToPlayPanel;

    [Header("Screen Shake")]
    [Tooltip("Drag the root Canvas or a parent RectTransform to shake on wrong answer.")]
    public RectTransform shakeTarget;
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 18f;

    [Header("Game Settings")]
    public int totalBaits = 20;
    public int totalRounds = 15;
    
    [HideInInspector] public int currentBaits;
    private int currentRound;
    
    private List<GreetingQuizData> allGreetings = new List<GreetingQuizData>();
    private List<QuestionData> questionPool = new List<QuestionData>();

    private GreetingQuizData currentTarget;
    private bool isAskingScenario; // false = ask direct translation, true = ask scenario

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LoadQuizData();

        // Show How To Play screen first, DO NOT start game yet
        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            // Find all buttons in the group (so we catch buttons that are siblings to the panel)
            var buttons = howToPlayGroup.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            UnityEngine.UI.Button closeBtn = null;
            foreach (var btn in buttons)
            {
                if (btn.name.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0 || btn.name.IndexOf("X", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    closeBtn = btn;
                    break;
                }
            }
            if (closeBtn == null && buttons.Length > 0) closeBtn = buttons[buttons.Length - 1]; // Fallback

            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseHowToPlay);
            }

            howToPlayGroup.SetActive(true);
            StartCoroutine(AnimatePanelIn(howToPlayPanel.transform));
        }
        else
        {
            // Fallback just in case you haven't assigned it in the Inspector yet
            StartGame();
        }
    }

    void LoadQuizData()
    {
        // Load the greetings scenario data
        TextAsset jsonFile = Resources.Load<TextAsset>("Greetings"); // We'll put it in Resources for easy loading, or read from Data path
        // Actually, let's read from the exact path we created it at:
        string path = Path.Combine(Application.dataPath, "Data/Minigames/FishingGame/Greetings.json");
        if (File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            string wrappedJson = "{\"items\":" + jsonString + "}";
            GreetingDataWrapper wrapper = JsonUtility.FromJson<GreetingDataWrapper>(wrappedJson);
            if (wrapper != null && wrapper.items != null)
            {
                allGreetings = new List<GreetingQuizData>(wrapper.items);
                
                // Build the pool of all possible questions (Translation + Scenario for each)
                questionPool.Clear();
                foreach (var g in allGreetings)
                {
                    questionPool.Add(new QuestionData { greeting = g, isScenario = false });
                    questionPool.Add(new QuestionData { greeting = g, isScenario = true });
                }

                // Shuffle the pool so they are completely random and non-repeating
                for (int i = 0; i < questionPool.Count; i++)
                {
                    QuestionData temp = questionPool[i];
                    int randomIndex = Random.Range(i, questionPool.Count);
                    questionPool[i] = questionPool[randomIndex];
                    questionPool[randomIndex] = temp;
                }
            }
        }
        else
        {
            Debug.LogError("Greetings.json not found at " + path);
        }
    }

    private class GreetingDataWrapper
    {
        public GreetingQuizData[] items;
    }

    public void StartGame()
    {
        currentBaits = totalBaits;
        currentRound = 1;
        UpdateHUD();
        
        if (feedbackImage != null)
        {
            feedbackImage.gameObject.SetActive(false); // Hide popup on start
        }

        NextRound();
    }

    // Call this from the "X" Button OnClick()
    public void CloseHowToPlay()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);
        if (howToPlayPanel != null)
        {
            StartCoroutine(AnimatePanelOut(howToPlayPanel.transform, () => 
            {
                if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
                StartGame(); // Actually start the game after the animation finishes!
            }));
        }
        else
        {
            if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
            StartGame();
        }
    }

    void NextRound()
    {
        if (currentRound > totalRounds || questionPool.Count == 0)
        {
            Debug.Log("Game Won! You finished all rounds.");
            // Trigger Win Screen
            return;
        }

        // Pop the next random question from the pool
        QuestionData currentQuestion = questionPool[0];
        questionPool.RemoveAt(0);

        currentTarget = currentQuestion.greeting;
        isAskingScenario = currentQuestion.isScenario;

        if (isAskingScenario)
        {
            titleText.text = "Catch the phrase you use:";
            questionText.text = currentTarget.scenario;
        }
        else
        {
            titleText.text = "Catch the fish that means:";
            questionText.text = currentTarget.english;
        }

        UpdateHUD();
    }

    public void OnFishCaught(FishController caughtFish)
    {
        if (caughtFish == null) return;
        
        // Prevent catching more fish if we are out of baits or game is over
        if (currentBaits <= 0 || winOrLoseGroup.activeSelf) return;

        // Deduct 1 bait for EVERY catch attempt
        currentBaits--;
        UpdateHUD();

        // Check if the caught fish's ID matches the target ID
        bool isCorrect = (caughtFish.assignedId == currentTarget.id);

        // Always restore the fish back to the pond — we have 8 fish but 15 rounds!
        RestoreFishToPond(caughtFish);

        if (isCorrect)
        {
            Debug.Log("Correct Fish Caught! Starting STT...");
            ShowFeedback(true);
            StartCoroutine(PlayCorrectAnswerSFXDelayed());
            
            // Start STT instead of immediately advancing the round!
            Debug.Log($"[FishingQuizManager] FishingSTTManager.Instance is: {(FishingSTTManager.Instance == null ? "NULL - falling back!" : "FOUND - starting STT")}");
            if (FishingSTTManager.Instance != null)
            {
                FishingSTTManager.Instance.StartSTT(caughtFish);
            }
            else
            {
                // Fallback if STT is not attached
                Debug.LogWarning("[FishingQuizManager] FishingSTTManager not found in scene! Skipping STT and advancing round. Please attach FishingSTTManager to a GameObject.");
                CompleteSTTAndAdvanceRound();
            }
        }
        else
        {
            Debug.Log("Wrong Fish! Screen Shake.");
            ShowFeedback(false);
            StartCoroutine(ShakeScreen());

            if (currentBaits <= 0)
            {
                ShowLoseScreen();
            }
        }
    }

    // Snaps the fish instantly back to where it started — no slow drift!
    void RestoreFishToPond(FishController fish)
    {
        if (fish == null) return;

        fish.transform.position = fish.spawnPosition;  // Teleport back to spawn
        fish.transform.localScale = fish.spawnScale;    // Restore original scale (preserves mirror!)
        fish.isCaught = false;                          // Unfreeze swimming
        fish.gameObject.SetActive(true);
    }

    private System.Collections.IEnumerator PlayCorrectAnswerSFXDelayed()
    {
        yield return new WaitForSeconds(1f);
        if (uiAudioSource != null && correctAnswerSFX != null)
            uiAudioSource.PlayOneShot(correctAnswerSFX);
    }

    private System.Collections.IEnumerator ShakeScreen()
    {
        if (shakeTarget == null) yield break;

        // Vibrate on mobile
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif

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
    public void CompleteSTTAndAdvanceRound()
    {
        currentRound++;

        if (currentBaits <= 0 && currentRound <= totalRounds)
        {
            ShowLoseScreen();
        }
        else if (currentRound > totalRounds)
        {
            ShowWinScreen();
        }
        else
        {
            NextRound();
        }
    }

    public void CompleteSTTAndFailRound()
    {
        // They failed the STT 3 times. We do not advance the round.
        // They must try to catch the correct fish again (bait is already gone).
        
        if (currentBaits <= 0)
        {
            ShowLoseScreen();
        }
    }

    private void ShowWinScreen()
    {
        Debug.Log("Game Won! Showing Win Screen.");
        winOrLoseGroup.SetActive(true);
        winPanel.SetActive(true);
        losePanel.SetActive(false);

        // Calculate Stars based on remaining baits (Total Baits is 20, 15 rounds = min 15 baits used)
        int baitsUsed = totalBaits - currentBaits;
        int stars = 0;
        int coinsEarned = 0;

        if (baitsUsed <= 15) { stars = 5; coinsEarned = 50; }
        else if (baitsUsed == 16) { stars = 4; coinsEarned = 40; }
        else if (baitsUsed == 17) { stars = 3; coinsEarned = 30; }
        else if (baitsUsed == 18) { stars = 2; coinsEarned = 20; }
        else { stars = 1; coinsEarned = 10; }

        // Update star sprites visually
        for (int i = 0; i < winStars.Length; i++)
        {
            if (winStars[i] != null)
            {
                winStars[i].sprite = (i < stars) ? activeStarSprite : inactiveStarSprite;
                winStars[i].gameObject.SetActive(true); // Make sure they are visible!
            }
        }

        winCoinsText.text = $"+{coinsEarned}";

        // Save coins and minigame win state
        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
        PlayerPrefs.SetInt("FishingMinigameWon", 1); 
        PlayerPrefs.Save();

        if (uiAudioSource != null && winPanelSFX != null) uiAudioSource.PlayOneShot(winPanelSFX);
        StartCoroutine(AnimatePanelIn(winPanel.transform));
    }

    private void ShowLoseScreen()
    {
        Debug.Log("Game Over! Showing Lose Screen.");
        winOrLoseGroup.SetActive(true);
        winPanel.SetActive(false);
        losePanel.SetActive(true);

        // Consolation prize for losing
        int coinsEarned = 2;
        loseCoinsText.text = $"+{coinsEarned}";

        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
        PlayerPrefs.SetInt("FishingMinigameWon", 0);
        PlayerPrefs.Save();

        if (uiAudioSource != null && losePanelSFX != null) uiAudioSource.PlayOneShot(losePanelSFX);
        StartCoroutine(AnimatePanelIn(losePanel.transform));
    }

    private System.Collections.IEnumerator AnimatePanelIn(Transform panelTransform)
    {
        panelTransform.localScale = Vector3.zero;
        
        float duration = 0.3f;
        float elapsed = 0f;

        // Pop up and overshoot slightly
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out back (bouncy overshoot)
            float scale = 1 + 0.15f * Mathf.Sin(t * Mathf.PI); // Bounces up to 1.15
            panelTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.1f, t);
            yield return null;
        }

        // Settle back to exactly 1
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            panelTransform.localScale = Vector3.Lerp(Vector3.one * 1.1f, Vector3.one, elapsed / 0.1f);
            yield return null;
        }

        panelTransform.localScale = Vector3.one;
    }

    private System.Collections.IEnumerator AnimatePanelOut(Transform panelTransform, System.Action onComplete = null)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = panelTransform.localScale;

        // Shrink down quickly
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panelTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        panelTransform.localScale = Vector3.zero;
        onComplete?.Invoke();
    }

    // Call this from the "Try Again" Button OnClick()
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Call this from the WinPanel's "Continue" Button OnClick()
    public void ContinueToNextObjective()
    {
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene");
        SceneManager.LoadScene(prevScene);
    }

    // Call this from the LosePanel's "Quit" Button OnClick()
    public void QuitToPreviousScene()
    {
        string prevScene = PlayerPrefs.GetString("PreviousScene", "LanguageSelectionScene");
        SceneManager.LoadScene(prevScene);
    }

    void UpdateHUD()
    {
        if (baitCountText != null) baitCountText.text = currentBaits + "/" + totalBaits;
        if (roundCountText != null) roundCountText.text = currentRound + "/" + totalRounds;
    }

    private void ShowFeedback(bool isCorrect)
    {
        if (feedbackImage == null) return;

        // Set the right sprite
        feedbackImage.sprite = isCorrect ? correctSprite : wrongSprite;
        
        // Stop any running animations on it
        StopAllCoroutines();
        StartCoroutine(AnimateFeedbackPopup());
    }

    private System.Collections.IEnumerator AnimateFeedbackPopup()
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
        yield return new WaitForSeconds(feedbackDuration);

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
}
