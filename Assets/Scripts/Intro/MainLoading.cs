using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainLoading : MonoBehaviour
{
    public UnityEngine.UI.Image loadingFill;
    public TextMeshProUGUI loadingText;

    public string sceneToLoad = "LoginScene";
    public float minimumLoadTime = 7f;
    public float smoothSpeed = 3f;

    private float displayedProgress = 0f;

    void Start()
    {
        StartCoroutine(LoadAsyncScene());
    }

    IEnumerator LoadAsyncScene()
    {
        float timer = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(timer / minimumLoadTime);

            // Target progress = whichever is smaller
            float targetProgress = Mathf.Min(realProgress, timeProgress);

            // Smoothly move displayed progress toward target
            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                smoothSpeed * Time.deltaTime
            );

            // Apply to BOTH fill and text
            loadingFill.fillAmount = displayedProgress;

            int percent = Mathf.RoundToInt(displayedProgress * 100f);
            loadingText.text = "Loading Assets... " + percent + "%";

            // Activate scene only when both complete
            if (realProgress >= 1f && timer >= minimumLoadTime && displayedProgress >= 1f)
            {
                yield return new WaitForSeconds(0.2f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}