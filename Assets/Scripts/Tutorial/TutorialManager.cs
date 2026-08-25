using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image tutorialDisplayImage; // The picture that changes
    public TextMeshProUGUI tutorialDescriptionText; // The text that changes
    public Button nextButton;
    public Button previousButton;
    
    [Header("Tutorial Slides")]
    [Tooltip("Drag all your tutorial slides here in order")]
    public TutorialSlide[] tutorialSlides;
    
    [System.Serializable]
    public class TutorialSlide
    {
        public Sprite image;
        [TextArea(3, 10)]
        public string description;
    }
    
    [Header("Pagination Indicator")]
    [Tooltip("Drag the Images acting as dots here, in order from left to right")]
    public Image[] pageDots; 
    public Sprite dotActiveSprite;
    public Sprite dotInactiveSprite;

    [Header("Button States")]
    [Tooltip("The sprite used for the Next button normally")]
    public Sprite standardNextSprite;
    [Tooltip("The sprite used for the Next button on the last page")]
    public Sprite doneSprite;

    [Header("Animations")]
    public float fadeDuration = 0.3f; // Time in seconds to fade out and in

    [Header("Transitions")]
    public GameObject smallLoadingPrefab;
    public string nextSceneName = "Calle_Crisologo";

    private int _currentIndex = 0;
    private Coroutine _fadeCoroutine;

    void Start()
    {
        // Listen to UI button clicks
        if (nextButton != null)
            nextButton.onClick.AddListener(NextSlide);
            
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousSlide);

        // Check if slides are assigned
        if (tutorialSlides == null || tutorialSlides.Length == 0)
        {
            Debug.LogWarning("Tutorial Manager: No tutorial slides assigned!");
            return;
        }

        // Initialize UI State (but skip animation for the very first page)
        UpdateTutorialUI(false);
    }

    public void NextSlide()
    {
        if (_currentIndex < tutorialSlides.Length - 1)
        {
            _currentIndex++;
            UpdateTutorialUI();
        }
        else
        {
            // Tutorial Finished!
            Debug.Log("[Tutorial] Finished! Starting async transition...");
            StartCoroutine(TransitionToGame());
        }
    }

    private IEnumerator TransitionToGame()
    {
        Debug.Log("[Tutorial] Finished! Updating progress...");

        if (UserProfileManager.Instance != null)
        {
            // Wait for the task to finish before moving scenes
            var task = UserProfileManager.Instance.SetTutorialCompleted(true);
            yield return new WaitUntil(() => task.IsCompleted);
        }

        Debug.Log("[Tutorial] Transitioning to Game via SceneLoader...");

        string finalSceneToLoad = nextSceneName;
        
        // PhraseEvaluator might not exist in the TutorialScene, so we check PlayerPrefs directly
        int savedRegion = PlayerPrefs.GetInt("SelectedRegion", 0); // Default to 0 (Ilokano)
        
        if (savedRegion == (int)RegionMode.Ilokano)
            finalSceneToLoad = "Calle_Crisologo";
        else if (savedRegion == (int)RegionMode.Cebuano)
            finalSceneToLoad = "Magellan_s_Cross";

        // Find the SceneLoader in the scene and use it
        var loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadScene(finalSceneToLoad);
        }
        else
        {
            // Fallback: Addressable scenes CANNOT be loaded directly via SceneManager.
            // Create a temporary SceneLoader to correctly route through the LoadingScene.
            Debug.Log("[Tutorial] No SceneLoader found. Creating a temporary one to load the Addressable scene.");
            GameObject tempObj = new GameObject("TempSceneLoader");
            SceneLoader tempLoader = tempObj.AddComponent<SceneLoader>();
            tempLoader.LoadScene(finalSceneToLoad);
        }
        
        yield break;
    }

    public void PreviousSlide()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            UpdateTutorialUI();
        }
    }

    private void UpdateTutorialUI(bool animate = true)
    {
        // 1. Update the Main Picture
        if (tutorialDisplayImage != null && tutorialSlides.Length > 0)
        {
            if (animate && gameObject.activeInHierarchy)
            {
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeTransition(_currentIndex));
            }
            else
            {
                tutorialDisplayImage.sprite = tutorialSlides[_currentIndex].image;
                if (tutorialDescriptionText != null) tutorialDescriptionText.text = tutorialSlides[_currentIndex].description;
                
                Color c = tutorialDisplayImage.color;
                c.a = 1f;
                tutorialDisplayImage.color = c;
            }
        }

        // 2. Hide or Show Previous Button (Hide on first page)
        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(_currentIndex > 0);
        }

        // 4. Update Next/Done Button Sprite Seamlessly
        if (nextButton != null && standardNextSprite != null && doneSprite != null)
        {
            // If on the last slide, show 'Done', else show 'Next'
            nextButton.image.sprite = (_currentIndex == tutorialSlides.Length - 1) ? doneSprite : standardNextSprite;
        }

        // 3. Update the Dots
        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] != null)
            {
                // If this dot matches the current index, make it Active. Otherwise, Inactive.
                pageDots[i].sprite = (i == _currentIndex) ? dotActiveSprite : dotInactiveSprite;
            }
        }
    }

    private IEnumerator FadeTransition(int newIndex)
    {
        float elapsed = 0f;
        Color c = tutorialDisplayImage.color;
        float halfDuration = fadeDuration / 2f;

        // Fade Out the old image
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
            tutorialDisplayImage.color = c;
            yield return null;
        }

        // Swap Sprite and Text when fully transparent
        tutorialDisplayImage.sprite = tutorialSlides[newIndex].image;
        if (tutorialDescriptionText != null) tutorialDescriptionText.text = tutorialSlides[newIndex].description;

        // Fade In the new image
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
            tutorialDisplayImage.color = c;
            yield return null;
        }

        // Ensure it's fully visible at the end
        c.a = 1f;
        tutorialDisplayImage.color = c;
    }
}
