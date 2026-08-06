using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Toggle))]
public class CustomizationTab : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Sprite to use when this tab is selected")]
    public Sprite activeSprite;
    [Tooltip("Sprite to use when this tab is not selected")]
    public Sprite inactiveSprite;
    
    [Header("Sizing Settings")]
    public float activeHeight = 165f;
    public float inactiveHeight = 135f;

    [Header("Animation Settings")]
    public float transitionDuration = 0.2f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References")]
    public Image targetImage;
    [Tooltip("The ScrollView or Panel that should open when this tab is active")]
    public GameObject targetPanel;
    
    private RectTransform rectTransform;
    private Toggle toggle;
    private Coroutine transitionCoroutine;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        rectTransform = GetComponent<RectTransform>();
        
        if (targetImage == null) 
            targetImage = GetComponent<Image>();

        toggle.onValueChanged.AddListener(OnTabChanged);
    }

    void Start()
    {
        // Set initial state instantly
        UpdateVisuals(toggle.isOn, true);
    }

    private void OnTabChanged(bool isOn)
    {
        UpdateVisuals(isOn, false);
    }

    private void UpdateVisuals(bool isOn, bool instant)
    {
        // Change the sprite
        if (targetImage != null && (isOn ? activeSprite : inactiveSprite) != null)
        {
            targetImage.sprite = isOn ? activeSprite : inactiveSprite;
        }

        // Change the height
        float targetHeight = isOn ? activeHeight : inactiveHeight;

        // Open/Close the target panel
        if (targetPanel != null)
        {
            targetPanel.SetActive(isOn);
        }

        if (instant)
        {
            SetHeight(targetHeight);
        }
        else
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(AnimateHeight(targetHeight));
        }
    }

    private IEnumerator AnimateHeight(float targetHeight)
    {
        float startHeight = rectTransform.sizeDelta.y;
        float elapsed = 0;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            SetHeight(Mathf.Lerp(startHeight, targetHeight, t));
            yield return null;
        }

        SetHeight(targetHeight);
        transitionCoroutine = null;
    }

    private void SetHeight(float height)
    {
        if (rectTransform != null)
        {
            Vector2 size = rectTransform.sizeDelta;
            size.y = height;
            rectTransform.sizeDelta = size;
        }
    }
    
    // Optional: Update visuals when the script is enabled/disabled or in editor
    private void OnValidate()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (toggle == null) toggle = GetComponent<Toggle>();
    }
}
