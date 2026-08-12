using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TusokStickItem : MonoBehaviour, IPointerClickHandler
{
    public TusokWokItem originalWokItem { get; private set; }
    public Image itemImage; // The UI image component that shows the food sprite

    private void Awake()
    {
        if (itemImage == null)
            itemImage = GetComponent<Image>();
    }

    public void Initialize(TusokWokItem wokItem)
    {
        originalWokItem = wokItem;
        if (itemImage != null && wokItem != null && wokItem.stickSprite != null)
        {
            itemImage.sprite = wokItem.stickSprite;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Prevent interacting during STT
        if (TusokTusokGameManager.Instance != null && TusokTusokGameManager.Instance.isSTTPhaseActive)
        {
            return;
        }

        // Tell the stick manager we clicked this item
        if (TusokStickManager.Instance != null)
        {
            TusokStickManager.Instance.RemoveFoodFromStick(this);
        }
    }
}
