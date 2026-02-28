using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityIntro : MonoBehaviour
{
    public CanvasGroup unityLogoGroup;
    public float fadeSpeed = 2f;
    public float displayTime = 2f;

    private float timer;
    private bool fadingOut = false;
    private bool sceneLoading = false;

    void Update()
    {
        timer += Time.deltaTime;

        if (!fadingOut)
        {
            if (unityLogoGroup.alpha < 1f)
                unityLogoGroup.alpha += Time.deltaTime * fadeSpeed;

            if (timer >= displayTime)
                fadingOut = true;
        }
        else
        {
            unityLogoGroup.alpha -= Time.deltaTime * fadeSpeed;

            if (!sceneLoading && unityLogoGroup.alpha <= 0f)
            {
                sceneLoading = true;
                SceneManager.LoadScene("MainLoadingScene"); // your actual game scene
            }
        }
    }
}