using UnityEngine;

/// <summary>
/// Ensures the Main Camera can raycast to 2D colliders via the EventSystem.
/// The actual clicking is now handled natively by IPointerClickHandler on the fishes themselves.
/// </summary>
public class FishTouchHandler : MonoBehaviour
{
    private void Awake()
    {
        // 100% Bulletproof Fix: Add Physics2DRaycaster to the camera so the EventSystem 
        // treats the 2D fish colliders exactly like UI Buttons!
        if (gameObject.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
        {
            gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
        }
    }
}
