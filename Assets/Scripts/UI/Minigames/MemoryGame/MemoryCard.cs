using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class MemoryCard : MonoBehaviour
{
    [Header("UI References (Root)")]
    public Image cardBackgroundImage; // The main Image component on the root Card
    public Sprite cardBackSprite; // The pattern back design
    public Sprite cardFrontFrameSprite; // The plain white frame for the front

    [Header("UI References (Child)")]
    public GameObject frontIconObject; // The child GameObject (FrontIcon)
    public Image frontIconImage; // The Image component on FrontIcon

    [Header("Visual Feedback")]
    public Color normalColor = new Color(1, 1, 1, 0); // Transparent by default
    public Color matchColor = new Color(0, 1, 0, 0.8f);
    public Color mismatchColor = new Color(1, 0, 0, 0.8f);
    
    private Image generatedGlow;
    private static Sprite proceduralGlowSprite; // Cache it so all 16 cards share 1 texture

    [Header("Flip Settings")]
    public float flipDuration = 0.25f;

    [HideInInspector] public string pairID;
    private Sprite cardFrontVerbSprite; 
    [HideInInspector] public bool isFaceUp = false;
    [HideInInspector] public bool isMatched = false;

    private Button button;
    private Action<MemoryCard> onCardClicked;
    private bool isAnimating = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnCardClicked);
        }

        CreateGlowObject();
    }

    private void CreateGlowObject()
    {
        GameObject glowObj = new GameObject("ProceduralGlow", typeof(RectTransform), typeof(Image));
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.SetAsFirstSibling(); // Render behind card

        generatedGlow = glowObj.GetComponent<Image>();
        
        if (proceduralGlowSprite == null)
        {
            proceduralGlowSprite = GenerateSoftGlowTexture();
        }

        generatedGlow.sprite = proceduralGlowSprite;
        generatedGlow.type = Image.Type.Simple; // Do NOT 9-slice, just stretch the soft blur
        generatedGlow.color = normalColor;

        // Tight padding so the glow sits very close to the card
        RectTransform rt = glowObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-5f, -5f); 
        rt.offsetMax = new Vector2(5f, 5f);
    }

    // Creates a completely soft, blurry texture
    private Sprite GenerateSoftGlowTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Calculate distance from center, but map it to a rounded box shape instead of a perfect circle
                // We use a modified distance formula (x^4 + y^4) for a softer "squircle" shape
                float dx = Mathf.Abs(x - center.x) / radius;
                float dy = Mathf.Abs(y - center.y) / radius;
                float dist = Mathf.Pow(Mathf.Pow(dx, 4) + Mathf.Pow(dy, 4), 0.25f);
                
                float alpha = Mathf.Clamp01(1f - dist);
                
                // Tight, sharp exponential fade (glow)
                alpha = Mathf.Pow(alpha, 3.5f); 
                
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        
        // Return a Simple sprite (no 9-slicing vectors)
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
    }

    public void Setup(string id, Sprite verbSprite, Action<MemoryCard> clickCallback)
    {
        pairID = id;
        cardFrontVerbSprite = verbSprite;
        onCardClicked = clickCallback;
        
        // Reset state for new round
        isFaceUp = false;
        isMatched = false;
        
        // Set to Back visually
        cardBackgroundImage.sprite = cardBackSprite;
        frontIconObject.SetActive(false);
        if (frontIconImage != null) frontIconImage.sprite = cardFrontVerbSprite;
        ClearFeedback();

        transform.localScale = Vector3.one;
    }

    private void OnCardClicked()
    {
        if (isAnimating || isFaceUp || isMatched) return;
        onCardClicked?.Invoke(this);
    }

    public void FlipFaceUp()
    {
        if (isFaceUp || isAnimating) return;
        StartCoroutine(FlipCoroutine(true));
    }

    public void FlipFaceDown()
    {
        if (!isFaceUp || isAnimating) return;
        StartCoroutine(FlipCoroutine(false));
    }

    public void ForceFaceUp()
    {
        isFaceUp = true;
        isAnimating = false;
        cardBackgroundImage.sprite = cardFrontFrameSprite;
        frontIconObject.SetActive(true);
        if (generatedGlow != null) generatedGlow.sprite = cardBackgroundImage.sprite;
    }

    private IEnumerator FlipCoroutine(bool faceUp)
    {
        isAnimating = true;

        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = new Vector3(0f, 1f, 1f);

        while (elapsed < flipDuration / 2f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / (flipDuration / 2f));
            yield return null;
        }

        cardBackgroundImage.sprite = faceUp ? cardFrontFrameSprite : cardBackSprite;
        frontIconObject.SetActive(faceUp);
        
        // Ensure the glow shape matches whatever is currently showing
        if (generatedGlow != null) generatedGlow.sprite = cardBackgroundImage.sprite;
        
        isFaceUp = faceUp;

        elapsed = 0f;
        while (elapsed < flipDuration / 2f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(endScale, startScale, elapsed / (flipDuration / 2f));
            yield return null;
        }

        transform.localScale = Vector3.one;
        isAnimating = false;
    }

    public void SetMatched()
    {
        isMatched = true;
    }

    public void SetFeedback(bool isMatch)
    {
        if (generatedGlow != null)
        {
            generatedGlow.color = isMatch ? matchColor : mismatchColor;
        }
    }

    public void ClearFeedback()
    {
        if (generatedGlow != null)
        {
            generatedGlow.color = normalColor;
        }
    }
}
