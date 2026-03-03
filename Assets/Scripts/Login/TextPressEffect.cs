using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TextPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private TMP_Text textComponent;
    private float originalSize;

    void Start()
    {
        textComponent = GetComponent<TMP_Text>();
        originalSize = textComponent.fontSize;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        textComponent.fontSize = originalSize - 2f; // smaller when pressed
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        textComponent.fontSize = originalSize;
    }
}