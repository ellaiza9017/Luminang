using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SariSariNPC : MonoBehaviour
{
    [Header("Visuals")]
    public Image npcImage;
    
    [Header("Dialogue UI")]
    public GameObject chatBubbleObj;
    public TextMeshProUGUI dialogueText;
    
    private Sprite currentIdle;
    private Sprite currentHappy;
    private Sprite currentWrong;

    // Animation settings
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;
        }
        
        if (chatBubbleObj != null)
        {
            // If the user manually set a scale override in the Inspector, use it!
            if (baseBubbleScale.sqrMagnitude > 0.01f)
            {
                // Prevent accidental flattening! If they typed -1 in X but forgot Y and Z, fix it to 1!
                originalBubbleScale = new Vector3(
                    baseBubbleScale.x != 0 ? baseBubbleScale.x : 1f,
                    baseBubbleScale.y != 0 ? baseBubbleScale.y : 1f,
                    baseBubbleScale.z != 0 ? baseBubbleScale.z : 1f
                );
            }
            else
            {
                Vector3 currentScale = chatBubbleObj.transform.localScale;
                
                // Safely enforce a minimum readable size of 1 on all axes, while preserving any custom negative flips!
                originalBubbleScale = new Vector3(
                    Mathf.Sign(currentScale.x) * Mathf.Max(Mathf.Abs(currentScale.x), 1f),
                    Mathf.Sign(currentScale.y) * Mathf.Max(Mathf.Abs(currentScale.y), 1f),
                    Mathf.Sign(currentScale.z) * Mathf.Max(Mathf.Abs(currentScale.z), 1f)
                );
            }
        }
    }

    public void SetSprites(Sprite idle, Sprite happy, Sprite wrong)
    {
        currentIdle = idle;
        currentHappy = happy;
        currentWrong = wrong;
        
        SetIdle();
    }

    public void SetIdle()
    {
        if (currentIdle != null) npcImage.sprite = currentIdle;
    }

    [Header("Bubble Settings")]
    [Tooltip("Leave as 0,0,0 to auto-detect original scale from the Inspector.")]
    public Vector3 baseBubbleScale = Vector3.zero;

    private string currentNativeText = "";
    private string currentEnglishText = "";
    private bool isTranslated = false;
    private Vector3 originalBubbleScale = Vector3.one;


    public void ShowDialogue(string nativeText, string englishText)
    {
        currentNativeText = nativeText;
        currentEnglishText = englishText;
        isTranslated = false; // Always default to native when showing new dialogue

        if (chatBubbleObj != null) 
        {
            chatBubbleObj.SetActive(true);
            StopCoroutine("HideBubbleRoutine");
            StopCoroutine("PopBubbleRoutine");
            StartCoroutine("PopBubbleRoutine");
        }
        if (dialogueText != null) dialogueText.text = nativeText;
    }

    public void ToggleTranslation()
    {
        isTranslated = !isTranslated;
        if (dialogueText != null)
        {
            dialogueText.text = isTranslated ? currentEnglishText : currentNativeText;
        }
    }

    public void HideDialogue()
    {
        if (chatBubbleObj != null && chatBubbleObj.activeInHierarchy) 
        {
            StopCoroutine("PopBubbleRoutine");
            StopCoroutine("HideBubbleRoutine");
            StartCoroutine("HideBubbleRoutine");
        }
    }

    private IEnumerator PopBubbleRoutine()
    {
        Transform bubble = chatBubbleObj.transform;
        bubble.localScale = Vector3.zero;
        
        float duration = 0.3f;
        float elapsed = 0f;
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float t2 = t - 1f;
            float ease = 1f + c3 * (t2 * t2 * t2) + c1 * (t2 * t2);
            bubble.localScale = originalBubbleScale * ease;
            yield return null;
        }
        bubble.localScale = originalBubbleScale;
    }

    private IEnumerator HideBubbleRoutine()
    {
        Transform bubble = chatBubbleObj.transform;
        float elapsed = 0f;
        float duration = 0.15f;
        Vector3 startScale = bubble.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bubble.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        bubble.localScale = Vector3.zero;
        chatBubbleObj.SetActive(false);
    }

    public void SetHappy()
    {
        if (currentHappy != null) 
            npcImage.sprite = currentHappy;
        else 
            SetIdle(); // Fallback
    }

    public void SetWrong()
    {
        if (currentWrong != null) 
            npcImage.sprite = currentWrong;
        else 
            SetIdle(); // Fallback
    }

    // --- ANIMATIONS ---

    public void PlaySlideInAnimation(float offsetX = -500f, float duration = 0.5f)
    {
        if (rectTransform == null) return;
        StopCoroutine("SlideInRoutine");
        StopCoroutine("PopInRoutine");
        StartCoroutine(SlideInRoutine(offsetX, duration));
    }

    public void PlayPopInAnimation(float duration = 0.4f)
    {
        if (rectTransform == null) return;
        StopCoroutine("SlideInRoutine");
        StopCoroutine("PopInRoutine");
        StartCoroutine(PopInRoutine(duration));
    }

    private IEnumerator SlideInRoutine(float offsetX, float duration)
    {
        Vector2 startPos = originalPosition + new Vector2(offsetX, 0f);
        Vector2 endPos = originalPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Simple ease out cubic
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            yield return null;
        }
        rectTransform.anchoredPosition = endPos;
    }

    private IEnumerator PopInRoutine(float duration)
    {
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = originalScale;
        float elapsed = 0f;
        
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Standard easeOutBack formula (no fractional powers to avoid NaN)
            float t2 = t - 1f;
            float ease = 1f + c3 * (t2 * t2 * t2) + c1 * (t2 * t2);
            
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, ease);
            yield return null;
        }
        rectTransform.localScale = endScale;
    }
}
