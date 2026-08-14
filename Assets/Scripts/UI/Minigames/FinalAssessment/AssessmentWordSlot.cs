using UnityEngine;
using UnityEngine.EventSystems;

public class AssessmentWordSlot : MonoBehaviour, IDropHandler
{
    public AssessmentWordBlock CurrentBlock { get; private set; }

    private RectTransform rectTransform;
    private Vector2 defaultSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultSize = rectTransform.sizeDelta; // Save the empty slot size
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            AssessmentWordBlock droppedBlock = eventData.pointerDrag.GetComponent<AssessmentWordBlock>();
            if (droppedBlock != null)
            {
                if (CurrentBlock == null)
                {
                    CurrentBlock = droppedBlock;
                    droppedBlock.currentSlot = this;

                    droppedBlock.transform.SetParent(transform);

                    // Resize slot to exactly match the block's width so nothing overlaps
                    RectTransform blockRect = droppedBlock.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(blockRect.sizeDelta.x, rectTransform.sizeDelta.y);

                    // Force anchors/pivot to center then snap to center of slot
                    blockRect.anchorMin = new Vector2(0.5f, 0.5f);
                    blockRect.anchorMax = new Vector2(0.5f, 0.5f);
                    blockRect.pivot = new Vector2(0.5f, 0.5f);
                    blockRect.localPosition = Vector3.zero;

                    // Force the SentenceBox layout to recalculate immediately (safe, synchronous call)
                    RectTransform parentRect = rectTransform.parent as RectTransform;
                    if (parentRect != null)
                        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

                    // Tell the block it was placed (enables typing for {template} blocks)
                    droppedBlock.OnPlacedInSlot();

                    // Hide the dotted line outline!
                    UnityEngine.UI.Graphic graphic = GetComponent<UnityEngine.UI.Graphic>();
                    if (graphic != null) graphic.enabled = false;

                    // Notify the AssessmentManager to check if all slots are filled and play SFX
                    if (AssessmentManager.Instance != null)
                    {
                        AssessmentManager.Instance.PlaySBDropSfx();
                        AssessmentManager.Instance.OnSBSlotChanged();
                    }
                }
            }
        }
    }

    public void ClearSlot()
    {
        CurrentBlock = null;

        // Revert slot back to its default empty size
        if (rectTransform != null)
            rectTransform.sizeDelta = defaultSize;

        // Show the dotted line outline again!
        UnityEngine.UI.Graphic graphic = GetComponent<UnityEngine.UI.Graphic>();
        if (graphic != null) graphic.enabled = true;
    }
}
