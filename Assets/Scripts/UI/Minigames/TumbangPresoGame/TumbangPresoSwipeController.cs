using UnityEngine;
using UnityEngine.InputSystem;

public class TumbangPresoSwipeController : MonoBehaviour
{
    [Header("References")]
    public TumbangPresoHandAnimator handAnimator;
    public TsinelasProjectile projectile;
    
    [Header("Swipe Settings")]
    [Tooltip("How far down on the screen (in pixels) the player must drag to reach 100% bwelo.")]
    public float maxBweloDragDistance = 300f;
    [Tooltip("The minimum drag progress (0 to 1) required before a throw is allowed.")]
    public float minimumBweloThreshold = 0.75f;
    [Tooltip("How far UP (in pixels) the player must flick from the bottom of their bwelo to throw.")]
    public float minimumSwipeUpDistance = 50f;
    
    [Header("Targeting Settings")]
    [Tooltip("The World Y-Position where the Tin Cans are located. The Tsinelas will always stop exactly on this line.")]
    public float canYPosition = 0f;
    [Tooltip("How sensitive the left/right aiming is. Increase this if you want it to fly further left/right with a smaller flick.")]
    public float horizontalSensitivity = 1.0f;
    
    private Vector2 startTouchPosition;
    private Vector2 currentTouchPosition;
    private Vector2 lowestTouchPosition; // Tracks the bottom of the pull-back
    private bool isDragging = false;
    private bool isCocked = false; 
    public bool isInputBlocked = false;
    
    void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (isInputBlocked) return;
        
        var pointer = Pointer.current;
        if (pointer == null) return;

        bool wasPressed = pointer.press.wasPressedThisFrame;
        bool isPressed = pointer.press.isPressed;
        bool wasReleased = pointer.press.wasReleasedThisFrame;
        Vector2 position = pointer.position.ReadValue();

        if (wasPressed)
        {
            startTouchPosition = position;
            lowestTouchPosition = position;
            isDragging = true;
            isCocked = false;
        }
        else if (isPressed && isDragging)
        {
            currentTouchPosition = position;
            
            // Keep track of the lowest point they drag down to
            if (currentTouchPosition.y < lowestTouchPosition.y)
            {
                lowestTouchPosition = currentTouchPosition;
            }
            
            // Calculate how far DOWN they dragged (Y axis) for the Bwelo
            float dragDistance = startTouchPosition.y - currentTouchPosition.y;
            
            if (dragDistance > 0)
            {
                float progress = Mathf.Clamp01(dragDistance / maxBweloDragDistance);
                handAnimator.SetBweloProgress(progress);
                
                if (progress >= minimumBweloThreshold)
                {
                    isCocked = true; // Bwelo condition met!
                }
            }
        }
        else if (wasReleased && isDragging)
        {
            currentTouchPosition = position;
            isDragging = false;
            
            // Prevent auto-fire bug: If you drag outside the Unity Simulator window, 
            // Unity forces a "Release" event. We should cancel the throw if that happens!
            bool isOutsideScreen = currentTouchPosition.x < 0 || currentTouchPosition.x > Screen.width ||
                                   currentTouchPosition.y < 0 || currentTouchPosition.y > Screen.height;
                                   
            if (isOutsideScreen)
            {
                handAnimator.SnapBackToIdle();
                isCocked = false;
                return;
            }
            
            // Calculate how far UP they flicked from the bottom-most point of their pull-back
            float swipeUpDistance = currentTouchPosition.y - lowestTouchPosition.y;
            
            // If they are cocked, they MUST flick upwards towards the cans to throw!
            // If they just let go (swipeUpDistance is 0 or very small), it cancels!
            if (isCocked && swipeUpDistance > minimumSwipeUpDistance)
            {
                ThrowTsinelas();
            }
            else
            {
                // Failed bwelo OR they just let go without flicking up!
                handAnimator.SnapBackToIdle();
                isCocked = false;
            }
        }
    }
    
    private void ThrowTsinelas()
    {
        handAnimator.HideHand();
        
        // 1. Calculate the raw pixel distance of the flick
        float swipeDeltaX = currentTouchPosition.x - lowestTouchPosition.x;
        float swipeDeltaY = currentTouchPosition.y - lowestTouchPosition.y;
        
        // Prevent division by zero
        if (swipeDeltaY <= 0) swipeDeltaY = 0.1f;
        
        // 2. Find the ratio (angle) of the swipe
        float swipeRatio = swipeDeltaX / swipeDeltaY;
        
        // 3. Calculate the real world distance it needs to travel forward (Y-axis)
        float startWorldY = projectile.transform.position.y;
        float worldTravelDistanceY = canYPosition - startWorldY;
        
        // 4. Apply the ratio to the world distance! 
        // Example: If ratio is 1.0 (45 degrees), it travels exactly as far right as it does forward.
        float startWorldX = projectile.transform.position.x;
        float worldTravelDistanceX = worldTravelDistanceY * swipeRatio * horizontalSensitivity;
        
        float finalTargetX = startWorldX + worldTravelDistanceX;
        
        Vector3 finalTargetPos = new Vector3(finalTargetX, canYPosition, 0f);
        
        projectile.Fire(finalTargetPos);
        
        // We do NOT snap back to idle here. The hand must stay out of frame 
        // to maintain the illusion that the tsinelas was thrown!
        // It will snap back later when the projectile resets.
    }
}
