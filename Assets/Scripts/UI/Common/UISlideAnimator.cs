using UnityEngine;
using System.Collections;

/// <summary>
/// Animates a UI element by sliding it in from a chosen direction and fading it.
/// Attach this to any UI element (like the HomeButton). 
/// It requires a CanvasGroup to handle the fading.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UISlideAnimator : MonoBehaviour
{
    public enum SlideDirection { Left, Right, Top, Bottom }
    
    [Header("Animation Settings")]
    [Tooltip("Which direction should the UI element slide IN from?")]
    public SlideDirection slideFrom = SlideDirection.Left;
    
    [Tooltip("How far off-screen should it start?")]
    public float slideDistance = 300f;
    
    [Tooltip("How long should the animation take?")]
    public float duration = 0.5f;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Coroutine activeRoutine;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized) return;
        
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalPosition = rectTransform.anchoredPosition;
        
        isInitialized = true;
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        Show();
    }

    public void Show()
    {
        if (!gameObject.activeInHierarchy) return;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(AnimateIn());
    }

    public void Close()
    {
        if (!gameObject.activeInHierarchy) return;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(AnimateOut());
    }

    private Vector2 GetOffsetPosition()
    {
        switch (slideFrom)
        {
            case SlideDirection.Left: return originalPosition + new Vector2(-slideDistance, 0);
            case SlideDirection.Right: return originalPosition + new Vector2(slideDistance, 0);
            case SlideDirection.Top: return originalPosition + new Vector2(0, slideDistance);
            case SlideDirection.Bottom: return originalPosition + new Vector2(0, -slideDistance);
            default: return originalPosition;
        }
    }

    // Smooth ease-out math for a slick entrance
    private float EaseOutQuart(float x)
    {
        return 1f - Mathf.Pow(1f - x, 4f);
    }

    // Smooth ease-in math for a slick exit
    private float EaseInQuart(float x)
    {
        return x * x * x * x;
    }

    private IEnumerator AnimateIn()
    {
        float elapsed = 0;
        Vector2 startPos = GetOffsetPosition();
        
        rectTransform.anchoredPosition = startPos;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            // Apply the smooth ease out
            float curveT = EaseOutQuart(t);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, originalPosition, curveT);
            canvasGroup.alpha = t; // linear fade looks best
            
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        canvasGroup.alpha = 1f;
        activeRoutine = null;
    }

    private IEnumerator AnimateOut()
    {
        float elapsed = 0;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = GetOffsetPosition();
        float startAlpha = canvasGroup.alpha;
        
        // Exits should be slightly faster than entrances so the UI feels snappy
        float outDuration = duration * 0.7f; 

        while (elapsed < outDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / outDuration;
            
            // Apply the smooth ease in
            float curveT = EaseInQuart(t);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curveT);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            
            yield return null;
        }

        rectTransform.anchoredPosition = endPos;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        activeRoutine = null;
    }
}
