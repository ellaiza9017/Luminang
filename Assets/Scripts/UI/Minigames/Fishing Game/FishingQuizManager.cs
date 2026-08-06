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
    
#if UNITY_EDITOR
    [Header("--- EDITOR DEBUG (hidden in build) ---")]
    public int currentBaits;
    public int currentRound;
#else
    [HideInInspector] public int currentBaits;
    private int currentRound;
#endif
    
    private List<GreetingQuizData> allGreetings = new List<GreetingQuizData>();
    private List<QuestionData> questionPool = new List<QuestionData>();

    private GreetingQuizData currentTarget;
    private bool isAskingScenario; // false = ask direct translation, true = ask scenario

    void Awake()
    {
        Instance = this;
    }

#if UNITY_EDITOR
    void Update()
    {
        // EDITOR CHEATS — stripped out of APK builds completely
        // Press W → instantly trigger Win Screen
        // Press L → instantly trigger Lose Screen
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame) ShowWinScreen();
        if (kb.lKey.wasPressedThisFrame) ShowLoseScreen();
    }
#endif

    void Start()
    {
        LoadQuizData();

        // Show How To Play screen first, DO NOT start game yet
        if (howToPlayGroup != null && howToPlayPanel != null)
        {
            howToPlayGroup.SetActive(true);
            howToPlayGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
            howToPlayPanel.GetComponent<UIPopAnimator>()?.PopIn();
        }
        else
        {
            // Fallback just in case you haven't assigned it in the Inspector yet
            StartGame();
        }
    }

    void LoadQuizData()
    {
        // Load the greetings scenario data from the Resources folder (mobile friendly)
        TextAsset jsonFile = Resources.Load<TextAsset>("Greetings");
        
        if (jsonFile != null)
        {
            string jsonString = jsonFile.text;
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
            Debug.LogError("Greetings.json not found in any Resources folder! Make sure it is inside a folder named 'Resources'.");
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

    // Fallback: if any star reference is missing, search for them under WinPanel automatically
    private void ValidateStars()
    {
        if (winPanel == null) return;
        bool anyNull = winStars == null || winStars.Length == 0;
        if (!anyNull)
        {
            foreach (var s in winStars) if (s == null) { anyNull = true; break; }
        }
        if (!anyNull) return; // All fine, skip

        Debug.LogWarning("[WinScreen] Some star references are missing — searching for them automatically under WinPanel...");
        var images = winPanel.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        var found = new System.Collections.Generic.List<UnityEngine.UI.Image>();
        foreach (var img in images)
        {
            string n = img.gameObject.name.ToLower();
            if (n.Contains("star")) found.Add(img);
        }
        if (found.Count > 0)
        {
            winStars = found.ToArray();
            Debug.Log($"[WinScreen] Auto-recovered {winStars.Length} star Image(s) from WinPanel children.");
        }
        else
        {
            Debug.LogError("[WinScreen] Could not auto-find any star Images under WinPanel! Make sure they are named with 'Star' in them.");
        }
    }

    private void ShowWinScreen()
    {
        Debug.Log("Game Won! Showing Win Screen.");
        if (winOrLoseGroup != null)
        {
            winOrLoseGroup.SetActive(true);
            winOrLoseGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
        }
        if (winPanel != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);

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
        ValidateStars(); // Auto-recover if references are missing
        Debug.Log($"[WinScreen] baitsUsed={baitsUsed}, stars={stars}, winStars.Length={winStars?.Length ?? -1}, activeSprite={activeStarSprite?.name}, inactiveSprite={inactiveStarSprite?.name}");
        if (winStars != null)
        {
            for (int i = 0; i < winStars.Length; i++)
            {
                if (winStars[i] != null)
                {
                    Sprite chosen = (i < stars) ? activeStarSprite : inactiveStarSprite;
                    winStars[i].sprite = chosen;
                    winStars[i].gameObject.SetActive(true);
                    Debug.Log($"[WinScreen] Star[{i}] → {chosen?.name ?? "NULL sprite!"}");
                }
                else
                {
                    Debug.LogWarning($"[WinScreen] Star[{i}] is NULL in the array!");
                }
            }
        }

        if (winCoinsText != null)
        {
            winCoinsText.text = $"+{coinsEarned}";
        }

        // Save coins and minigame win state
        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
        PlayerPrefs.SetInt("FishingMinigameWon", 1); 
        PlayerPrefs.Save();

        if (uiAudioSource != null && winPanelSFX != null) uiAudioSource.PlayOneShot(winPanelSFX);
        winPanel?.GetComponent<UIPopAnimator>()?.PopIn();
    }

    private void ShowLoseScreen()
    {
        Debug.Log("Game Over! Showing Lose Screen.");
        if (winOrLoseGroup != null)
        {
            winOrLoseGroup.SetActive(true);
            winOrLoseGroup.GetComponent<UIFadeAnimator>()?.FadeIn();
        }
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);

        // Consolation prize for losing
        int coinsEarned = 2;
        if (loseCoinsText != null)
        {
            loseCoinsText.text = $"+{coinsEarned}";
        }

        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinsEarned);
        PlayerPrefs.SetInt("FishingMinigameWon", 0);
        PlayerPrefs.Save();

        if (uiAudioSource != null && losePanelSFX != null) uiAudioSource.PlayOneShot(losePanelSFX);
        losePanel?.GetComponent<UIPopAnimator>()?.PopIn();
    }

    // Call this from the "X" Button OnClick()
    public void CloseHowToPlay()
    {
        if (uiAudioSource != null && buttonClickSFX != null) uiAudioSource.PlayOneShot(buttonClickSFX);
        
        if (howToPlayPanel != null) howToPlayPanel.transform.localScale = Vector3.zero; // INSTANT SNAP
        if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
        
        StartGame(); 
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
