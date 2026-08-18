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
        Debug.Log("[AssessmentHowToPlay] Fetched Language from PlayerPrefs: " + selectedLanguage);

        // 2. Pick the correct array based on language
        // We check against "Ilokano" first, exactly like AssessmentManager does, to avoid trailing space bugs
        Sprite[] activeSprites = (selectedLanguage == "Ilokano") ? ilokanoCards : cebuanoCards;
        Debug.Log("[AssessmentHowToPlay] Active Sprites Length: " + (activeSprites != null ? activeSprites.Length.ToString() : "null"));

        // 3. Prevent errors if you forgot to assign everything
        if (cardImages == null || activeSprites == null) return;

        // 4. Swap the sprites!
        for (int i = 0; i < cardImages.Length; i++)
        {
            if (cardImages[i] == null) continue;

            if (i < activeSprites.Length && activeSprites[i] != null)
            {
                cardImages[i].gameObject.SetActive(true);
                cardImages[i].sprite = activeSprites[i];
            }
            else
            {
                cardImages[i].gameObject.SetActive(false);
            }
        }
    }
}
