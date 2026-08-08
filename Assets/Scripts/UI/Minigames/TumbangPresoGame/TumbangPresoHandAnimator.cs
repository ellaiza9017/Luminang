using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TumbangPresoHandAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Drag all your hand 'bwelo' sprites here in order")]
    public Sprite[] throwFrames;
    public float framesPerSecond = 12f;
    
    [Header("Movement Settings")]
    [Tooltip("How much should the hand move on X and Y during the throw?")]
    public Vector2 positionOffset = new Vector2(0, 50f);
    
    [Header("Rotation Settings")]
    [Tooltip("How many degrees should it rotate left during the throw? (Positive = Left, Negative = Right)")]
    public float rotationOffsetZ = 15f;
    
    [Header("Components")]
    [Tooltip("Assign an Image for Canvas UI, or a SpriteRenderer if using 2D World space")]
    public Image handImage;
    public SpriteRenderer handSpriteRenderer;
    
    private Coroutine currentAnimation;
    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private void Awake()
    {
        if (handImage == null) handImage = GetComponent<Image>();
        if (handSpriteRenderer == null) handSpriteRenderer = GetComponent<SpriteRenderer>();
        
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
    }

    /// <summary>
    /// Manually sets the wind-up (bwelo) progress based on the player's swipe drag.
    /// </summary>
    /// <param name="progress">0.0 (Idle) to 1.0 (Fully cocked)</param>
    public void SetBweloProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (throwFrames == null || throwFrames.Length == 0) return;

        SetVisibility(true);

        // Find which frame to show using RoundToInt so the final frame is easier to hit
        int frameIndex = Mathf.RoundToInt(progress * (throwFrames.Length - 1));
        SetSprite(throwFrames[frameIndex]);

        // Calculate where the hand should rotate and move to based on progress
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, rotationOffsetZ);
        Vector3 targetPosition = initialPosition + (Vector3)positionOffset;

        transform.localRotation = Quaternion.Lerp(initialRotation, targetRotation, progress);
        transform.localPosition = Vector3.Lerp(initialPosition, targetPosition, progress);
    }

    public void SnapBackToIdle()
    {
        SetBweloProgress(0f);
    }

    public void HideHand()
    {
        SetVisibility(false);
    }

    // THE 3 DOTS MENU TEST BUTTON! 
    [ContextMenu("Test Throw Animation")]
    public void TestThrowAnimation()
    {
        if (Application.isPlaying)
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateHandAuto());
        }
    }

    private IEnumerator AnimateHandAuto()
    {
        if (throwFrames == null || throwFrames.Length == 0) yield break;

        SetVisibility(true);
        float delay = 1f / framesPerSecond;

        for (int i = 0; i < throwFrames.Length; i++)
        {
            float progress = throwFrames.Length > 1 ? (float)i / (throwFrames.Length - 1) : 1f;
            SetBweloProgress(progress);
            yield return new WaitForSeconds(delay);
        }

        HideHand();
        SnapBackToIdle();
    }

    private void SetSprite(Sprite sprite)
    {
        if (handImage != null) handImage.sprite = sprite;
        else if (handSpriteRenderer != null) handSpriteRenderer.sprite = sprite;
    }

    private void SetVisibility(bool isVisible)
    {
        if (handImage != null) handImage.enabled = isVisible;
        else if (handSpriteRenderer != null) handSpriteRenderer.enabled = isVisible;
    }
}
