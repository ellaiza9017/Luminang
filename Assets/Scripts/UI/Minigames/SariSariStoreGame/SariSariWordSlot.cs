using UnityEngine;
using UnityEngine.EventSystems;

public class SariSariWordSlot : MonoBehaviour, IDropHandler
{
    public SariSariWordBlock CurrentBlock { get; private set; }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            SariSariWordBlock droppedBlock = eventData.pointerDrag.GetComponent<SariSariWordBlock>();
            if (droppedBlock != null)
            {
                if (CurrentBlock == null)
                {
                    CurrentBlock = droppedBlock;
                    droppedBlock.currentSlot = this;
                    
                    droppedBlock.transform.SetParent(transform);
                    
                    // Snap perfectly to the center regardless of RectTransform pivots!
                    droppedBlock.GetComponent<RectTransform>().position = transform.position;
                    
                    // Tell the block it was placed!
                    droppedBlock.OnPlacedInSlot();
                    
                    // Hide the dotted line outline!
                    UnityEngine.UI.Graphic graphic = GetComponent<UnityEngine.UI.Graphic>();
                    if (graphic != null) graphic.enabled = false;

                    // Update visual state (optional, no longer automatically submits!)
                    if (SariSariGameManager.Instance != null)
                    {
                        SariSariGameManager.Instance.CheckSentenceState();
                        SariSariGameManager.Instance.PlayDropSfx();
                    }
                }
            }
        }
    }

    public void ClearSlot()
    {
        CurrentBlock = null;
        
        // Show the dotted line outline again!
        UnityEngine.UI.Graphic graphic = GetComponent<UnityEngine.UI.Graphic>();
        if (graphic != null) graphic.enabled = true;
    }
}
