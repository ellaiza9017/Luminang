using UnityEngine;
using UnityEngine.SceneManagement;

public class SampleIntro : MonoBehaviour
{
    public CanvasGroup iconGroup;
    public CanvasGroup nameGroup;

    public RectTransform iconRect;
    public RectTransform nameRect;

    public float fadeSpeed = 2f;
    public float expandSpeed = 4f;
    public float moveSpeed = 3f;

    public float finalSpacing = 130f;
    public float finalNameWidth = 350f;

    public float holdTime = 2f; // how long logos stay visible

    private float timer;
    private bool fadingOut = false;
    private bool sceneLoading = false;

    void Update()
    {
        timer += Time.deltaTime;

        // ======================
        // 1️⃣ INTRO ANIMATION
        // ======================
        if (!fadingOut)
        {
            // Fade in icon
            if (iconGroup.alpha < 1f)
                iconGroup.alpha += Time.deltaTime * fadeSpeed;

            // Fade in name
            if (nameGroup.alpha < 1f)
                nameGroup.alpha += Time.deltaTime * fadeSpeed;

            // Expand name width
            if (nameRect.sizeDelta.x < finalNameWidth)
            {
                float newWidth = Mathf.Lerp(
                    nameRect.sizeDelta.x,
                    finalNameWidth,
                    Time.deltaTime * expandSpeed
                );

                nameRect.sizeDelta = new Vector2(newWidth, nameRect.sizeDelta.y);
            }

            // Slide apart
            iconRect.anchoredPosition = Vector2.Lerp(
                iconRect.anchoredPosition,
                new Vector2(-finalSpacing, 0),
                Time.deltaTime * moveSpeed
            );

            nameRect.anchoredPosition = Vector2.Lerp(
                nameRect.anchoredPosition,
                new Vector2(finalSpacing, 0),
                Time.deltaTime * moveSpeed
            );

            // After hold time → start fade out
            if (timer >= holdTime)
            {
                fadingOut = true;
            }
        }
        // ======================
        // 2️⃣ FADE OUT
        // ======================
        else
        {
            iconGroup.alpha -= Time.deltaTime * fadeSpeed;
            nameGroup.alpha -= Time.deltaTime * fadeSpeed;

            // Once fully invisible → load immediately
            if (!sceneLoading && iconGroup.alpha <= 0f && nameGroup.alpha <= 0f)
            {
                sceneLoading = true;
                SceneManager.LoadScene("Unity_LoadingScene");
            }
        }
    }
}