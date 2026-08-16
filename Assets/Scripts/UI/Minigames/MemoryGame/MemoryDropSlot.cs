using UnityEngine;
using UnityEngine.EventSystems;

public class MemoryDropSlot : MonoBehaviour, IDropHandler
{
    private MemoryGameManager gameManager;
    private string targetPhraseId;

    private void Start()
    {
        gameManager = FindObjectOfType<MemoryGameManager>();
    }

    public void SetTarget(string phraseId)
    {
        targetPhraseId = phraseId;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;
        
        MemoryDraggableWord draggableWord = dropped.GetComponent<MemoryDraggableWord>();

        if (draggableWord != null)
        {
            // Visually snap to the slot perfectly
            draggableWord.transform.SetParent(transform.parent);
            draggableWord.GetComponent<RectTransform>().position = GetComponent<RectTransform>().position;
            
            HideVisuals();
            
            // Lock the word so the user can't pull it out while the Coroutine runs
            draggableWord.LockWord();

            // Check if it's the right word
            if (draggableWord.id == targetPhraseId)
            {
                gameManager.OnDragDropSuccess(draggableWord);
            }
            else
            {
                gameManager.OnDragDropFail(draggableWord);
            }
        }
    }

    public void HideVisuals()
    {
        if (TryGetComponent<UnityEngine.UI.Image>(out var img)) img.enabled = false;
        foreach (var t in GetComponentsInChildren<TMPro.TextMeshProUGUI>()) t.enabled = false;
    }

    public void ShowVisuals()
    {
        if (TryGetComponent<UnityEngine.UI.Image>(out var img)) img.enabled = true;
        foreach (var t in GetComponentsInChildren<TMPro.TextMeshProUGUI>()) t.enabled = true;
    }
}
