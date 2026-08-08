using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TumbangPresoCanController : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Drag all the falling frames for the Tin Can here")]
    public Sprite[] fallFrames;
    public float framesPerSecond = 12f;
    
    [Header("UI Panel Settings")]
    [Tooltip("Drag the CanvasGroup of the hovering Choice Panel here so it can fade out when the can falls")]
    public CanvasGroup textPanelCanvasGroup;
    [Tooltip("Drag the TextMeshPro text inside the hovering Choice Panel here")]
    public TMPro.TextMeshProUGUI choiceText;
    
    [HideInInspector]
    public bool isCorrectAnswer = false;
    [HideInInspector]
    public string feedbackText = "";
    
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private Collider2D canCollider;
    [HideInInspector]
    public bool hasFallen = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        canCollider = GetComponent<Collider2D>();
    }

    public void FallDown()
    {
        if (hasFallen) return; // Prevent falling twice if hit multiple times
        hasFallen = true;
        
        // Disable the collider so it can't be hit again this round
        if (canCollider != null)
        {
            canCollider.enabled = false;
        }

        StartCoroutine(PlayFallAnimation());
    }

    private IEnumerator PlayFallAnimation()
    {
        if (fallFrames == null || fallFrames.Length == 0) yield break;

        float delay = 1f / framesPerSecond;
        float totalDuration = fallFrames.Length * delay;
        float elapsedTime = 0f;
        
        for (int i = 0; i < fallFrames.Length; i++)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = fallFrames[i];
            if (uiImage != null) uiImage.sprite = fallFrames[i];
            
            // Fade out the text panel over the course of the animation
            if (textPanelCanvasGroup != null)
            {
                elapsedTime += delay;
                textPanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / totalDuration);
            }
            
            yield return new WaitForSeconds(delay);
        }
        
        // Ensure it is completely invisible
        if (textPanelCanvasGroup != null)
        {
            textPanelCanvasGroup.alpha = 0f;
        }
    }
    
    public void ResetCan()
    {
        hasFallen = false;
        
        if (canCollider != null)
        {
            canCollider.enabled = true;
        }
        
        if (fallFrames != null && fallFrames.Length > 0)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = fallFrames[0];
            if (uiImage != null) uiImage.sprite = fallFrames[0];
        }
        
        if (textPanelCanvasGroup != null)
        {
            textPanelCanvasGroup.alpha = 1f;
        }
    }
}
