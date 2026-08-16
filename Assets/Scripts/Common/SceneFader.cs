using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("UI Elements")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.15f; // Super fast for that 'instant' feel

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure we have a CanvasGroup and the Premium Overlay
        if (fadeCanvasGroup == null)
        {
            GameObject fadeObj = new GameObject("FadeOverlay");
            fadeObj.transform.SetParent(this.transform);
            
            Canvas canvas = fadeObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            
            fadeObj.AddComponent<GraphicRaycaster>();
            fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            
            GameObject imageObj = new GameObject("FlashOverlay");
            imageObj.transform.SetParent(fadeObj.transform);
            Image img = imageObj.AddComponent<Image>();
            
            // Using black for the fade instead of the bright white flash
            img.color = Color.black; 
            
            RectTransform rect = imageObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FadeIn();
    }

    public void FadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(fadeCanvasGroup.alpha, 0f));
    }

    public void FadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(fadeCanvasGroup.alpha, 1f));
    }

    public IEnumerator FadeOutCoroutine()
    {
        yield return Fade(fadeCanvasGroup.alpha, 1f);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = endAlpha;
        fadeCanvasGroup.blocksRaycasts = (endAlpha > 0.1f);
    }
}
