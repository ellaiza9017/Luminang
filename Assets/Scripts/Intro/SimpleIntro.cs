using UnityEngine;
using UnityEngine.SceneManagement;

public class SampleIntro : MonoBehaviour
{
    [Header("Groups")]
    public CanvasGroup iconGroup;
    public CanvasGroup nameGroup;

    [Header("Rects")]
    public RectTransform iconRect;
    public RectTransform nameRect;

    [Header("Animation Speeds")]
    public float fadeSpeed = 2f;
    public float expandSpeed = 4f;
    public float moveSpeed = 3f;

    [Header("Layout Settings")]
    public float finalSpacing = 200f;
    public float finalNameWidth = 500f;

    [Header("Timing")]
    public float holdTime = 2f;

    [Header("Mode")]
    public bool useScaleInsteadOfWidth = false; // 🔥 switch mode

    private float timer;
    private bool fadingOut = false;
    private bool sceneLoading = false;

    private Vector2 iconStartPos;
    private Vector2 nameStartPos;

    void Start()
    {
        // Reset alpha
        iconGroup.alpha = 0f;
        nameGroup.alpha = 0f;

        // Save starting positions
        iconStartPos = iconRect.anchoredPosition;
        nameStartPos = nameRect.anchoredPosition;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!fadingOut)
        {
            FadeIn();
            ExpandName();
            MoveApart();

            if (timer >= holdTime)
            {
                fadingOut = true;
            }
        }
        else
        {
            FadeOut();
        }
    }

    void FadeIn()
    {
        iconGroup.alpha = Mathf.MoveTowards(iconGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
        nameGroup.alpha = Mathf.MoveTowards(nameGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
    }

    void ExpandName()
    {
        if (useScaleInsteadOfWidth)
        {
            // 🔥 MAS RECOMMENDED (visible agad)
            float targetScaleX = 1.5f;

            Vector3 scale = nameRect.localScale;
            scale.x = Mathf.Lerp(scale.x, targetScaleX, Time.deltaTime * expandSpeed);
            nameRect.localScale = scale;
        }
        else
        {
            // ⚠️ depende sa image mo (kung may padding, di halata)
            float newWidth = Mathf.Lerp(
                nameRect.sizeDelta.x,
                finalNameWidth,
                Time.deltaTime * expandSpeed
            );

            nameRect.sizeDelta = new Vector2(newWidth, nameRect.sizeDelta.y);
        }
    }

    void MoveApart()
    {
        Vector2 iconTarget = new Vector2(-finalSpacing, iconStartPos.y);
        Vector2 nameTarget = new Vector2(finalSpacing, nameStartPos.y);

        iconRect.anchoredPosition = Vector2.Lerp(
            iconRect.anchoredPosition,
            iconTarget,
            Time.deltaTime * moveSpeed
        );

        nameRect.anchoredPosition = Vector2.Lerp(
            nameRect.anchoredPosition,
            nameTarget,
            Time.deltaTime * moveSpeed
        );
    }

    void FadeOut()
    {
        iconGroup.alpha = Mathf.MoveTowards(iconGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        nameGroup.alpha = Mathf.MoveTowards(nameGroup.alpha, 0f, Time.deltaTime * fadeSpeed);

        if (!sceneLoading && iconGroup.alpha <= 0f && nameGroup.alpha <= 0f)
        {
            sceneLoading = true;
            
            // ALWAYS go to LoadingResourcesScene first.
            // If download is needed -> it shows the download UI, then goes to MainLoadingScene.
            // If no download needed -> it skips instantly and goes to MainLoadingScene.
            // This way the planned flow is: TeamBA -> LoadingResourcesScene -> MainLoadingScene
            if (TransitionOverlay.Instance != null)
            {
                TransitionOverlay.Instance.StartTransition("LoadingResourcesScene");
            }
            else
            {
                SceneManager.LoadScene("LoadingResourcesScene");
            }
        }
    }
}