using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SariSariWordBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public TextMeshProUGUI wordText;
    public string CurrentWord { get; private set; }
    
    [HideInInspector]
    public SariSariWordSlot currentSlot;
    
    [HideInInspector]
    public bool isInputBlock = false;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Canvas parentCanvas;
    
    private TMP_InputField inputField;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Try to get an InputField if one exists
        inputField = GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }
    }
    
    private void OnInputFieldValueChanged(string newText)
    {
        if (isInputBlock)
        {
            SetWordRaw(newText);
            // Optional: You could update UI state here if needed
            if (currentSlot != null && SariSariGameManager.Instance != null)
            {
                SariSariGameManager.Instance.CheckSentenceState();
            }
        }
    }

    public void SetWord(string word)
    {
        if (word.StartsWith("{") && word.EndsWith("}"))
        {
            isInputBlock = true;
            string placeholderName = word.Trim('{', '}');
            CurrentWord = $"(Type {placeholderName}..)";
            
            if (inputField != null) 
            {
                inputField.enabled = true;
                inputField.interactable = false; // Not editable in the bank!
                inputField.text = CurrentWord;
            }
            else
            {
                SetWordRaw(CurrentWord);
            }
        }
        else
        {
            isInputBlock = false;
            CurrentWord = word;
            
            if (inputField != null) 
            {
                inputField.enabled = false; // Disable completely so it doesn't wipe normal text!
            }
            SetWordRaw(word);
        }
    }

    // Called when dropped into a slot
    public void OnPlacedInSlot()
    {
        if (isInputBlock && inputField != null)
        {
            inputField.interactable = true; // Editable now!
            if (inputField.text.StartsWith("(Type"))
            {
                inputField.text = ""; // Clear placeholder
            }
        }
    }

    // Called when removed from a slot
    public void OnRemovedFromSlot()
    {
        if (isInputBlock && inputField != null)
        {
            inputField.interactable = false; // No longer editable in the bank!
        }
    }

    // Helper method to actually apply the text and resize the box
    private void SetWordRaw(string word)
    {
        CurrentWord = word;
        if (wordText != null) 
        {
            wordText.text = word;
            
            // Bypass Unity Layout Group Jank: Force the exact pixel width mathematically!
            wordText.ForceMeshUpdate();
            float exactTextWidth = wordText.preferredWidth;
            rectTransform.sizeDelta = new Vector2(exactTextWidth + 80f, 75f); // 80px total padding
            
            // Force the text component to perfectly fit inside the new frame size
            RectTransform textRect = wordText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f); // 10px left padding
            textRect.offsetMax = new Vector2(-10f, 0f); // 10px right padding
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SariSariGameManager.Instance != null && SariSariGameManager.Instance.isCheckingAnswer) return;

        if (currentSlot != null)
        {
            // If it's an input block, tapping it should NOT return it to the bank.
            // The TMP_InputField component will automatically handle the typing!
            if (isInputBlock) return;
        
            // Remove from slot
            currentSlot.ClearSlot();
            currentSlot = null;
            OnRemovedFromSlot(); // Update interactability
            
            // Return to word bank
            transform.SetParent(SariSariGameManager.Instance.wordBoxGroup);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (SariSariGameManager.Instance != null && SariSariGameManager.Instance.isCheckingAnswer) return;

        // If we picked it up from a slot, tell the slot it's empty now!
        if (currentSlot != null)
        {
            currentSlot.ClearSlot();
            currentSlot = null;
        }

        originalParent = transform.parent;
        originalPosition = rectTransform.position;

        // Move to canvas root so it renders on top of everything
        transform.SetParent(parentCanvas.transform, true);
        
        // Disable raycasts so the drop event can detect the slot underneath
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;
        
        // Temporarily disable input field while dragging so it doesn't intercept drops
        if (isInputBlock && inputField != null) inputField.enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (SariSariGameManager.Instance != null && SariSariGameManager.Instance.isCheckingAnswer) return;

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        
        if (isInputBlock && inputField != null) inputField.enabled = true;

        // If it didn't get dropped on a slot, return to original position
        if (transform.parent == parentCanvas.transform)
        {
            if (originalParent != null && originalParent.GetComponent<SariSariWordSlot>() != null)
            {
                // It was picked up from a slot, so return it to the word bank!
                transform.SetParent(SariSariGameManager.Instance.wordBoxGroup);
            }
            else
            {
                // It was picked up from the word bank, return it there
                transform.SetParent(originalParent);
                rectTransform.position = originalPosition;
            }
            
            OnRemovedFromSlot();
            
            // Check sentence state in case we removed a word
            if (SariSariGameManager.Instance != null)
            {
                SariSariGameManager.Instance.CheckSentenceState();
            }
        }
    }
}
