using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FishingSequenceManager : MonoBehaviour
{
    public static FishingSequenceManager Instance;

    [Header("UI & References")]
    [Tooltip("Drag your Catch Button here")]
    public Button catchButton;
    [Tooltip("Drag the main PlayerBody object (with FishingPlayerAnimator) here")]
    public FishingPlayerAnimator playerAnimator;
    [Tooltip("Drag the REFLECTION PlayerBody object (with FishingPlayerAnimator) here")]
    public FishingPlayerAnimator reflectionAnimator;

    [Header("Fishing Line (UI Image - must be inside Canvas!)")]
    [Tooltip("Drag a UI Image here to act as the string. Make it a thin white rectangle in the Canvas.")]
    public RectTransform lineImage;

    [Header("Hook (UI Image - must be inside Canvas!)")]
    [Tooltip("Drag the hook's UI Image component here. The script will move it and show/hide it automatically.")]
    public Image hookImage; // Just drag the hook Image component here — RectTransform is grabbed automatically!

    [Tooltip("An empty RectTransform placed at the tip of the rod (also inside the Canvas)")]
    public RectTransform rodTip;

    [Header("Sound Effects")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSFX;  // Plays when Catch Button is clicked
    public AudioClip throwRodSFX;     // Plays when the hook is cast into the water
    public AudioClip fishCatchSFX;    // Plays when fish is near boat (reverse anim)

    [Header("Sequence Settings")]
    public float hookCastSpeed = 800f;
    public float reelInSpeed = 500f;
    [Tooltip("How many seconds before the player animation ends should the line start casting. Default 0.2 = cast starts on the last couple of frames.")]
    public float castLeadTime = 0.2f;
    [Tooltip("How close the fish needs to be to the boat before the pull-back animation triggers.")]
    public float reverseAnimTriggerDistance = 200f;

    public FishController currentlySelectedFish;  // public so FishController can check it
    private bool isFishingSequenceActive = false;

    void Awake()
    {
        Instance = this;

        if (catchButton != null)
        {
            catchButton.interactable = false;
            catchButton.onClick.AddListener(StartFishingSequence);
        }

        // Hide string and hook at the start
        if (lineImage != null) lineImage.gameObject.SetActive(false);
        if (hookImage != null) hookImage.gameObject.SetActive(false);

        // Set the line's pivot to the LEFT edge so it only stretches toward the fish, not backward!
        if (lineImage != null) lineImage.pivot = new Vector2(0f, 0.5f);
    }

    public void SelectFish(FishController fish)
    {
        if (isFishingSequenceActive) return;

        currentlySelectedFish = fish;

        if (catchButton != null)
            catchButton.interactable = true;
    }

    public void DeselectFish()
    {
        currentlySelectedFish = null;

        if (catchButton != null)
            catchButton.interactable = false;
    }

    public void StartFishingSequence()
    {
        if (currentlySelectedFish == null || isFishingSequenceActive) return;

        // NEW: Prevent casting if game is over or we are out of baits!
        if (FishingQuizManager.Instance != null && 
           (FishingQuizManager.Instance.currentBaits <= 0 || FishingQuizManager.Instance.winOrLoseGroup.activeSelf)) 
        {
            return;
        }

        isFishingSequenceActive = true;

        if (sfxSource != null && buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX);

        if (catchButton != null) catchButton.interactable = false;
        if (FishTooltip.Instance != null) FishTooltip.Instance.HideTooltip();

        if (playerAnimator != null)
        {
            // Start the player animation
            StartCoroutine(playerAnimator.PlayAnimationOnce(null));
            if (reflectionAnimator != null)
                StartCoroutine(reflectionAnimator.PlayAnimationOnce(null));

            // Calculate how long the animation takes, then start casting just before it ends!
            float animDuration = 0f;
            if (playerAnimator.fishingFrames != null && playerAnimator.fishingFrames.Length > 0)
                animDuration = playerAnimator.fishingFrames.Length / playerAnimator.framesPerSecond;

            float waitBeforeCast = Mathf.Max(0f, animDuration - castLeadTime);
            StartCoroutine(StartCastAfterDelay(waitBeforeCast));
        }
        else
        {
            // No animator assigned, cast immediately
            StartCoroutine(CastAndReelSequence());
        }
    }

    IEnumerator StartCastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sfxSource != null && throwRodSFX != null) sfxSource.PlayOneShot(throwRodSFX);
        StartCoroutine(CastAndReelSequence());
    }

    private void OnPlayerAnimationFinished()
    {
        // Kept for compatibility
        StartCoroutine(CastAndReelSequence());
    }

    IEnumerator CastAndReelSequence()
    {
        if (rodTip == null || currentlySelectedFish == null)
        {
            Debug.LogError("FishingSequenceManager is missing the Rod Tip or a selected fish!");
            isFishingSequenceActive = false;
            yield break;
        }

        // Freeze the fish
        currentlySelectedFish.isCaught = true;

        // Always snap the hook back to the rod tip first before showing it!
        // This fixes the bug where the hook doesn't appear on the 2nd, 3rd, etc. catches.
        if (hookImage != null)
        {
            ((RectTransform)hookImage.transform).position = rodTip.position;
            hookImage.gameObject.SetActive(true);
        }
        if (lineImage != null) lineImage.gameObject.SetActive(true);

        // Use world position so all objects (regardless of parent) share the same coordinate space!
        Vector3 hookCurrentWorldPos = rodTip.position;
        Vector3 targetWorldPos = currentlySelectedFish.transform.position;

        // --- PHASE 1: Cast hook OUT to the fish ---
        while (Vector3.Distance(hookCurrentWorldPos, targetWorldPos) > 2f)
        {
            targetWorldPos = currentlySelectedFish.transform.position;
            hookCurrentWorldPos = Vector3.MoveTowards(hookCurrentWorldPos, targetWorldPos, hookCastSpeed * Time.deltaTime);

            // Move the hook sprite using world position
            if (hookImage != null) ((RectTransform)hookImage.transform).position = hookCurrentWorldPos;

            // Stretch and rotate the line image to connect rod tip to hook
            DrawLine(rodTip.position, hookCurrentWorldPos);

            yield return null;
        }

        // --- PHASE 2: Reel fish IN to the boat ---
        bool reverseAnimTriggered = false;

        // Remember the fish's original scale so we can shrink it smoothly!
        Vector3 fishOriginalScale = currentlySelectedFish != null ? currentlySelectedFish.transform.localScale : Vector3.one;
        float totalReelDistance = Vector3.Distance(hookCurrentWorldPos, rodTip.position);

        while (Vector3.Distance(hookCurrentWorldPos, rodTip.position) > 2f)
        {
            hookCurrentWorldPos = Vector3.MoveTowards(hookCurrentWorldPos, rodTip.position, reelInSpeed * Time.deltaTime);

            if (hookImage != null) ((RectTransform)hookImage.transform).position = hookCurrentWorldPos;

            // Move AND shrink the fish as it travels to the boat!
            if (currentlySelectedFish != null)
            {
                currentlySelectedFish.transform.position = hookCurrentWorldPos;

                // t goes from 0 (fish just hooked) to 1 (fish at boat)
                float distanceLeft = Vector3.Distance(hookCurrentWorldPos, rodTip.position);
                float t = 1f - Mathf.Clamp01(distanceLeft / totalReelDistance);
                currentlySelectedFish.transform.localScale = Vector3.Lerp(fishOriginalScale, Vector3.zero, t);
            }

            DrawLine(rodTip.position, hookCurrentWorldPos);

            // When the fish is close enough to the boat, fire the reverse animation!
            if (!reverseAnimTriggered && Vector3.Distance(hookCurrentWorldPos, rodTip.position) < reverseAnimTriggerDistance)
            {
                reverseAnimTriggered = true;
                if (sfxSource != null && fishCatchSFX != null) sfxSource.PlayOneShot(fishCatchSFX);
                if (playerAnimator != null)
                {
                    StartCoroutine(playerAnimator.PlayAnimationReverse(null));
                    if (reflectionAnimator != null)
                        StartCoroutine(reflectionAnimator.PlayAnimationReverse(null));
                }
            }

            yield return null;
        }

        // --- PHASE 3: Fish reached the boat! Clean up. ---
        if (lineImage != null) lineImage.gameObject.SetActive(false);
        if (hookImage != null) hookImage.gameObject.SetActive(false);
        
        // Let the Quiz Manager handle what happens to the fish and the round progression
        if (currentlySelectedFish != null && FishingQuizManager.Instance != null)
        {
            FishingQuizManager.Instance.OnFishCaught(currentlySelectedFish);
        }
        else if (currentlySelectedFish != null)
        {
            // Fallback if no Quiz Manager exists in the scene
            currentlySelectedFish.gameObject.SetActive(false);
        }

        currentlySelectedFish = null;
        isFishingSequenceActive = false;
    }

    // Stretches and rotates the UI Image between two WORLD positions
    void DrawLine(Vector3 fromWorld, Vector3 toWorld)
    {
        if (lineImage == null || lineImage.parent == null) return;

        // Position the LEFT EDGE of the line at the rod tip.
        // Because the pivot is (0, 0.5), the line only grows TOWARD the fish, never backward!
        lineImage.position = fromWorld;

        // Convert world positions to the parent's local space to get the true scaled distance!
        Vector3 localFrom = lineImage.parent.InverseTransformPoint(fromWorld);
        Vector3 localTo = lineImage.parent.InverseTransformPoint(toWorld);
        float localDistance = Vector3.Distance(localFrom, localTo);

        // Stretch its width to match the exact local distance
        lineImage.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, localDistance);

        // Rotate to point from rod tip to hook
        Vector3 direction = toWorld - fromWorld;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineImage.localEulerAngles = new Vector3(0, 0, angle);
    }
}
