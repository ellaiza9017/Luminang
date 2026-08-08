using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TsinelasProjectile : MonoBehaviour
{
    [Header("Flight Settings")]
    [Tooltip("How long it takes to reach the target (in seconds)")]
    public float flightDuration = 0.5f;
    [Tooltip("How small the tsinelas gets when it flies away to simulate depth")]
    public float targetScale = 0.2f; 
    
    [Header("Hit Detection")]
    [Tooltip("How wide the circle is that checks for hit cans. Increase this if hits aren't registering.")]
    public float hitRadius = 1f;
    [Tooltip("The Physics Layer that your Tin Cans are on. Make sure your cans are on this layer!")]
    public LayerMask canLayerMask; 
    
    public AudioSource uiAudioSource;
    
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    
    private void Awake()
    {
        initialPosition = transform.position;
        initialScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        
        // Hide until fired
        HideVisuals();
    }
    
    private void HideVisuals()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (uiImage != null) uiImage.enabled = false;
    }
    
    private void ShowVisuals()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (uiImage != null) uiImage.enabled = true;
    }
    
    public void Fire(Vector3 targetWorldPosition)
    {
        ShowVisuals();
        
        // Start from exactly where the hand is
        transform.position = initialPosition;
        transform.localScale = initialScale;
        
        
        TumbangPresoGameManager gameManager = FindFirstObjectByType<TumbangPresoGameManager>();
        if (gameManager != null)
        {
            gameManager.PlayThrowSFX();
        }
        
        StartCoroutine(FlyRoutine(targetWorldPosition));
    }
    
    private IEnumerator FlyRoutine(Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 endScale = initialScale * targetScale;
        
        // Force it to maintain its original Z depth. We simulate 3D depth purely with Scale!
        // If it moves on the Z axis, the camera's perspective might make it look massive.
        targetPosition.z = startPosition.z;
        
        while (elapsedTime < flightDuration)
        {
            float progress = elapsedTime / flightDuration;
            // Add a simple ease-out curve for natural physics (slows down at the end)
            float easeProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, easeProgress);
            transform.localScale = Vector3.Lerp(initialScale, endScale, easeProgress);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Reached target!
        transform.position = targetPosition;
        transform.localScale = endScale;
        
        // Let the impact logic decide how long to wait (bounce vs simple miss delay)
        yield return StartCoroutine(CheckImpact(targetPosition));
        
        ResetProjectile();
    }
    
    private IEnumerator CheckImpact(Vector3 targetPos)
    {
        // 1. Draw an invisible circle at the landing spot to find all Cans on the Can Layer
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, hitRadius, canLayerMask);
        
        if (hits.Length > 0)
        {
            Collider2D closestCan = null;
            float minDistance = float.MaxValue;
            
            // 2. Overlap Resolution: If we hit multiple cans, find the one closest to the center (most touch)
            foreach (Collider2D hit in hits)
            {
                float distance = Vector2.Distance(targetPos, hit.bounds.center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestCan = hit;
                }
            }
            
            if (closestCan != null)
            {
                Debug.Log("Hit Can: " + closestCan.gameObject.name);
                
                // 3. Trigger the Can falling animation
                TumbangPresoCanController canController = closestCan.GetComponent<TumbangPresoCanController>();
                if (canController != null)
                {
                    canController.FallDown();
                }
                
                // 4. Notify GameManager to handle scoring and logic!
                TumbangPresoGameManager gameManager = FindFirstObjectByType<TumbangPresoGameManager>();
                if (gameManager != null && canController != null)
                {
                    gameManager.OnCanHit(canController);
                }
                
                // 5. Play a visual bounce effect off the can!
                yield return StartCoroutine(BounceEffect());
            }
        }
        else
        {
            Debug.Log("Missed! The Tsinelas didn't hit any cans.");
            // Just wait a little so the player sees they missed
            yield return new WaitForSeconds(0.4f);
        }
    }
    
    private IEnumerator BounceEffect()
    {
        // Simple bounce back
        Vector3 currentPos = transform.position;
        Vector3 bouncePos = currentPos + new Vector3(0, -1f, 0); // Bounce down slightly simulating falling
        
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Arc curve for the bounce
            float curve = Mathf.Sin(t * Mathf.PI);
            transform.position = Vector3.Lerp(currentPos, bouncePos, t) + new Vector3(0, curve * 0.8f, 0);
            
            // Add a small rotation spin when it bounces
            transform.Rotate(0, 0, 360f * Time.deltaTime);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset rotation before hiding
        transform.rotation = Quaternion.identity;
    }
    
    private void ResetProjectile()
    {
        HideVisuals();
        transform.position = initialPosition;
        transform.localScale = initialScale;
        
        // Make the hand reappear holding a new tsinelas for the next round!
        TumbangPresoHandAnimator hand = FindFirstObjectByType<TumbangPresoHandAnimator>();
        if (hand != null)
        {
            hand.SnapBackToIdle();
        }
    }
}
