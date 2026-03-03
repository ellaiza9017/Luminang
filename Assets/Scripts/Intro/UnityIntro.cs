using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityIntro : MonoBehaviour
{
    public CanvasGroup unityLogoGroup;
    public CanvasGroup textGroup;

    public float fadeSpeed = 2f;
    public float displayTime = 4f;     // total time before fade out
    public float textVisibleTime = 1f; // text stays visible for 1 second

    private float timer;
    private bool fadingOut = false;
    private bool sceneLoading = false;

    void Update()
    {
        timer += Time.deltaTime;

        if (!fadingOut)
        {
            // Fade in logo
            if (unityLogoGroup.alpha < 1f)
                unityLogoGroup.alpha += Time.deltaTime * fadeSpeed;

            // Show text 1 second before fade out
            if (timer >= displayTime - textVisibleTime)
            {
                if (textGroup.alpha < 1f)
                    textGroup.alpha += Time.deltaTime * fadeSpeed;
            }

            // Start fade out
            if (timer >= displayTime)
            {
                fadingOut = true;
            }
        }
        else
        {
            // Fade both out together
            unityLogoGroup.alpha -= Time.deltaTime * fadeSpeed;
            textGroup.alpha -= Time.deltaTime * fadeSpeed;

            if (!sceneLoading && unityLogoGroup.alpha <= 0f)
            {
                sceneLoading = true;
                SceneManager.LoadScene("MainLoadingScene");
            }
        }
    }
}