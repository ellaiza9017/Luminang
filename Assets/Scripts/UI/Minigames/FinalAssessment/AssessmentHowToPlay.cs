using UnityEngine;
using UnityEngine.UI;

public class AssessmentHowToPlay : MonoBehaviour
{
    [Header("--- UI Cards ---")]
    [Tooltip("Drag the 6 Image components of your cards here.")]
    public Image[] cardImages;

    [Header("--- Ilokano Sprites ---")]
    [Tooltip("Drag the 6 Ilokano card sprites here in order.")]
    public Sprite[] ilokanoCards;

    [Header("--- Cebuano Sprites ---")]
    [Tooltip("Drag the 6 Cebuano card sprites here in order.")]
    public Sprite[] cebuanoCards;

    private void OnEnable()
    {
        UpdateCards();
    }

    public void UpdateCards()
    {
        // 1. Get the language exactly like AssessmentManager does
        string selectedLanguage = PlayerPrefs.GetString("SelectedLanguage", "Ilokano");
        
#if UNITY_EDITOR
        selectedLanguage = "Ilokano"; // FORCE ILOKANO FOR TESTING (matches AssessmentManager cheat)
#endif

        // 2. Pick the correct array based on language
        Sprite[] activeSprites = (selectedLanguage == "Cebuano") ? cebuanoCards : ilokanoCards;

        // 3. Prevent errors if you forgot to assign everything
        if (cardImages == null || activeSprites == null) return;

        // 4. Swap the sprites!
        for (int i = 0; i < cardImages.Length; i++)
        {
            // Only swap if we actually have a sprite for this slot
            if (i < activeSprites.Length && activeSprites[i] != null && cardImages[i] != null)
            {
                cardImages[i].sprite = activeSprites[i];
            }
        }
    }
}
