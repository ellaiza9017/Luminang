using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ReactionCardAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The sprite frames that make up the animation")]
    public Sprite[] animationFrames;
    
    [Tooltip("How fast the animation plays (Frames Per Second)")]
    public float framesPerSecond = 12f;
    
    [Tooltip("Delay in seconds before the animation starts playing")]
    public float startDelay = 0f;
    
    [Tooltip("Should the animation loop indefinitely?")]
    public bool loopAnimation = false;

    [Tooltip("Should the animation play automatically when the object is enabled?")]
    public bool playOnEnable = false;

    private Image targetImage;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            PlayAnimation();
        }
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    /// <summary>
    /// Call this method from other scripts to trigger the animation
    /// </summary>
    public void PlayAnimation()
    {
        if (animationFrames == null || animationFrames.Length == 0)
        {
            Debug.LogWarning("[ReactionCardAnimator] No animation frames assigned!");
            return;
        }

        // Ensure we have the image component (in case Play is called before Awake)
        if (targetImage == null) targetImage = GetComponent<Image>();

        StopAnimation();
        animationCoroutine = StartCoroutine(AnimateRoutine());
    }

    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator AnimateRoutine()
    {
        // 1. Wait for the initial delay
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 2. Calculate time between frames
        float timeBetweenFrames = 1f / framesPerSecond;

        // 3. Play frames
        int currentFrame = 0;
        
        while (true)
        {
            targetImage.sprite = animationFrames[currentFrame];
            
            yield return new WaitForSeconds(timeBetweenFrames);

            currentFrame++;

            // Check if we reached the end of the frames
            if (currentFrame >= animationFrames.Length)
            {
                if (loopAnimation)
                {
                    currentFrame = 0; // Restart if looping
                }
                else
                {
                    break; // Stop if not looping
                }
            }
        }
    }

    // ==========================================
    // EDITOR TESTER (The "3 dots" context menu)
    // ==========================================
    
    [ContextMenu("Test Animation")]
    private void EditorTestAnimation()
    {
        // Unity allows testing coroutines in Play Mode via the context menu.
        // If we are in Edit Mode, we'll log a warning since coroutines only run in Play Mode.
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ReactionCardAnimator] Please enter Play Mode to test the animation!");
            return;
        }

        Debug.Log("[ReactionCardAnimator] Testing Animation...");
        PlayAnimation();
    }
}
