using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MemoryDraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TextMeshProUGUI wordText;
    [HideInInspector] public string id;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private Canvas rootCanvas;
    private bool isLocked = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Setup(string phraseId, string word)
    {
        id = phraseId;
        if (wordText != null) wordText.text = word;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // Bring to front so it renders over everything while dragging
        transform.SetParent(rootCanvas.transform); 
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        // Follow the mouse/finger exactly depending on Canvas settings
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = eventData.position;
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(rootCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint);
            rectTransform.position = worldPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;
        
        // If it was not dropped on a valid slot, return to original position
        if (transform.parent == rootCanvas.transform)
        {
            ResetPosition();
        }
    }

    public void ResetPosition()
    {
        UnlockWord(); // Make sure it's unlocked when returned
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }

    public void LockWord()
    {
        isLocked = true;
        if (canvasGroup != null) 
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 1.0f; // Fix the blurriness
        }
    }

    public void UnlockWord()
    {
        isLocked = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }
}
