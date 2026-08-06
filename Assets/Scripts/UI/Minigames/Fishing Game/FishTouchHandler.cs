using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles touch/click input for FishController objects in world space.
/// Uses Physics2D.Raycast so it works correctly on Android, iOS, and desktop.
/// Uses the new Input System to prevent InvalidOperationExceptions.
/// </summary>
public class FishTouchHandler : MonoBehaviour
{
    [Tooltip("The camera used to cast rays at fish. If left empty, Camera.main is used.")]
    public Camera fishCamera;

    private void Awake()
    {
        if (fishCamera == null)
            fishCamera = Camera.main;
    }

    private void Update()
    {
        if (fishCamera == null)
        {
            fishCamera = Camera.main;
            if (fishCamera == null) return;
        }

        bool wasPressed = false;
        Vector2 screenPosition = Vector2.zero;

        // Check Mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            wasPressed = true;
            screenPosition = Mouse.current.position.ReadValue();
        }
        // Check Touch
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            wasPressed = true;
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (wasPressed)
        {
            FireRaycast(screenPosition);
        }
    }

    private void FireRaycast(Vector2 screenPos)
    {
        bool hitFish = false;
        
        // METHOD 1: Try UI Graphic Raycaster (In case the fish are UI Canvas elements)
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = screenPos
            };
            
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (var result in results)
            {
                FishController fish = result.gameObject.GetComponentInParent<FishController>();
                if (fish != null)
                {
                    Debug.Log($"[FishTouchHandler] UI Raycast hit the fish: {fish.gameObject.name}!");
                    fish.OnFishTapped();
                    hitFish = true;
                    break;
                }
            }
        }

        // METHOD 2: Try 2D Physics Raycaster (In case the fish are World Space Sprites)
        if (!hitFish)
        {
            Ray ray = fishCamera.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit.collider != null)
            {
                FishController fish = hit.collider.GetComponent<FishController>();
                if (fish != null)
                {
                    Debug.Log($"[FishTouchHandler] 2D Physics Raycast hit the fish: {fish.gameObject.name}!");
                    fish.OnFishTapped();
                    hitFish = true;
                }
            }
        }

        if (!hitFish)
        {
            Debug.LogWarning($"[FishTouchHandler] Both UI and 2D Raycasts failed to hit a fish at screen position {screenPos}.");
        }
    }
}
