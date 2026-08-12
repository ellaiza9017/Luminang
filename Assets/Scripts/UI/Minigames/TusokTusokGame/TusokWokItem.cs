using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TusokWokItem : MonoBehaviour, IPointerClickHandler
{
    public enum FoodType { Fishball, Kwekkwek, Kikiam, Hotdog }
    public FoodType foodType;

    [Tooltip("The sprite to represent this food item on the stick")]
    public Sprite stickSprite;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        // Add button component if missing, though typically we just use IPointerClickHandler
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Don't allow clicking food if it's a recall round!
        if (TusokTusokGameManager.Instance != null && TusokTusokGameManager.Instance.IsRecallRound())
        {
            return;
        }

        // Prevent interacting during STT
        if (TusokTusokGameManager.Instance != null && TusokTusokGameManager.Instance.isSTTPhaseActive)
        {
            return;
        }

        if (TusokStickManager.Instance != null && TusokStickManager.Instance.CanAddItem())
        {
            // Try to add to the stick
            bool added = TusokStickManager.Instance.AddFoodToStick(this);
            if (added)
            {
                // Disable this item in the wok visually, or just leave it since the user said 
                // "we have 16 product per group... only 1 will go"
                // Actually the user said if they click on it, it goes to the stick. 
                // If they want it to physically disappear from the tray, we disable it.
                gameObject.SetActive(false);
            }
        }
    }

    public void ResetToTray()
    {
        // When it gets popped off the stick, it comes back
        gameObject.SetActive(true);
    }
}
