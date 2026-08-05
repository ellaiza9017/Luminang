using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class PlayerInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI coinsText;
    public Image profileImage; // For URL photos
    public RawImage portraitRawImage; // Still keep this to show the downloaded texture if needed
    public TextMeshProUGUI progressPercentageText;
    public Slider progressSlider;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("How far off-screen to the left the panel should start (-500 usually works well)")]
    public float offScreenX = -500f;

    [Tooltip("How long to wait before sliding in (e.g., waiting for clouds to open)")]
    public float entranceDelay = 2.5f;

    private RectTransform rectTransform;
    private Vector2 targetPosition; // The "on-screen" position (e.g., X=50)
    private Vector2 hiddenPosition; // The "off-screen" position
    
    private Coroutine slideCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Save the current position in the editor as our "target" on-screen position
        targetPosition = rectTransform.anchoredPosition;
        
        // Calculate the hidden position
        hiddenPosition = new Vector2(offScreenX, targetPosition.y);
        
        // Immediately snap it off-screen so it's ready
        rectTransform.anchoredPosition = hiddenPosition;
    }

    private void Start()
    {
        // 1. Fill the data from our profile
        UpdatePanelData();

        // 2. Animate it sliding in after the cloud delay!
        StartCoroutine(DelayedShow());
    }

    public void UpdatePanelData()
    {
        if (UserProfileManager.Instance == null || UserProfileManager.Instance.CurrentProfile == null)
        {
            Debug.LogWarning("[PlayerInfoPanel] No user profile found to display.");
            return;
        }

        var profile = UserProfileManager.Instance.CurrentProfile;

        // Set Username
        if (usernameText != null) 
            usernameText.text = string.IsNullOrEmpty(profile.Username) ? "Unknown Player" : profile.Username;

        // Set Coins
        if (coinsText != null)
            coinsText.text = profile.Coins.ToString("N0") + " Coins"; // e.g. 1,500 Coins

        // Progress can be connected dynamically later
        if (progressPercentageText != null) progressPercentageText.text = "0%"; 
        if (progressSlider != null) progressSlider.value = 0f;

        // Handle Profile Picture
        HandleProfilePicture(profile);
    }

    private void HandleProfilePicture(ProfileModel profile)
    {
        // Hide both initially
        if (profileImage != null) profileImage.gameObject.SetActive(false);
        if (portraitRawImage != null) portraitRawImage.gameObject.SetActive(false);

        // Check for a real URL (starts with http)
        if (!string.IsNullOrEmpty(profile.AvatarUrl) && profile.AvatarUrl.ToLower().StartsWith("http"))
        {
            if (profileImage != null) profileImage.gameObject.SetActive(true);
            StartCoroutine(DownloadProfileImage(profile.AvatarUrl));
        }
        else
        {
            // If no URL, we can show a default avatar or just keep it hidden
            Debug.Log("[PlayerInfoPanel] No avatar URL found.");
        }
    }

    private IEnumerator DownloadProfileImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            if (profileImage != null) profileImage.sprite = sprite;
        }
        else
        {
            Debug.LogError("[PlayerInfoPanel] Failed to download avatar: " + request.error);
        }
    }

    private IEnumerator DelayedShow()
    {
        yield return new WaitForSeconds(entranceDelay);
        _hasDoneInitialShow = true;
        
        // If we happen to be in dialogue right when the delay ends, stay hidden
        if (DialogueManager.Instance == null || !DialogueManager.Instance.IsInDialogue)
        {
            Show();
        }
    }

    private bool _isShowing = false; // Starts false since it begins off-screen
    private bool _hasDoneInitialShow = false;

    /// <summary>
    /// Slides the panel in from the left.
    /// </summary>
    public void Show()
    {
        if (_isShowing) return;
        _isShowing = true;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideRoutine(rectTransform.anchoredPosition, targetPosition));
    }

    /// <summary>
    /// Slides the panel back off-screen to the left.
    /// </summary>
    public void Hide()
    {
        if (!_isShowing) return;
        _isShowing = false;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideRoutine(rectTransform.anchoredPosition, hiddenPosition));
    }

    private void Update()
    {
        if (!_hasDoneInitialShow) return; // Wait until initial start delay is over

        // Auto-hide during dialogue
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsInDialogue) 
                Hide();
            else 
                Show();
        }
    }


    private IEnumerator SlideRoutine(Vector2 startPos, Vector2 endPos)
    {
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            
            // Apply the curve for a smooth "snap" effect
            float curveT = slideCurve.Evaluate(t);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, curveT);
            yield return null;
        }

        // Ensure it reaches the exact final position
        rectTransform.anchoredPosition = endPos;
    }
    /// <summary>
    /// Called when the user clicks on the player info panel.
    /// </summary>
    public void OnPanelClicked()
    {
        SceneNavigationManager.LoadCustomization();
    }
}
