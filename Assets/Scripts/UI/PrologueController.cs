using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Threading.Tasks;


public enum IntroType
{
    Prologue,
    IlocosIntro,
    CebuIntro
}

public class PrologueController : MonoBehaviour
{
    [Header("Intro Type")]
    public IntroType introType = IntroType.Prologue;

    [Header("Scene Navigation")]
    [Tooltip("The name of the scene to load after the prologue finishes.")]
    public string nextSceneName = "LanguageSelectionScene";

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public float fallbackWaitTime = 5.0f;
    public SceneLoader sceneLoader;

    [Header("Skip Button Settings")]
    public CanvasGroup skipButtonCanvasGroup;
    public float skipButtonDelay = 7f;

    private Coroutine _mainSequenceCoroutine;
    private bool _isSkipping = false;
    private bool _videoFinished = false;

    void Start()
    {
        // Hide the skip button initially
        if (skipButtonCanvasGroup != null)
        {
            skipButtonCanvasGroup.alpha = 0f;
            skipButtonCanvasGroup.interactable = false;
            skipButtonCanvasGroup.blocksRaycasts = false;
        }

        _mainSequenceCoroutine = StartCoroutine(StartPrologueSequence());
    }

    private IEnumerator FadeInSkipButton()
    {
        yield return new WaitForSeconds(skipButtonDelay);

        if (_isSkipping || skipButtonCanvasGroup == null) yield break;

        float fadeTime = 1f; // 1 second smooth fade in
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            skipButtonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            yield return null;
        }

        skipButtonCanvasGroup.interactable = true;
        skipButtonCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator StartPrologueSequence()
    {
        // Start the timer for the skip button
        StartCoroutine(FadeInSkipButton());

        // 1. Fade out BGM (if it exists) so we can hear the video audio

        if (BGMManager.Instance != null)
        {
            Debug.Log("[Prologue] Fading out BGM...");
            BGMManager.Instance.FadeVolume(0f, 1.5f);
        }

        if (videoPlayer != null)
        {
            Debug.Log("[Prologue] Waiting for Video Player...");

            // Don't drop frames to catch up — quality over speed
            videoPlayer.skipOnDrop = false;
            
            // Wait for it to prepare
            if (!videoPlayer.isPrepared)
            {
                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared) yield return null;
            }

            // Use the loopPointReached event — much more reliable than polling isPlaying
            _videoFinished = false;
            videoPlayer.loopPointReached += OnVideoFinished;

            videoPlayer.Play();
            Debug.Log("[Prologue] Video playing...");

            // Wait until the event fires OR we skip
            // Also guard with a time check so a missed event never blocks us
            while (!_videoFinished && !_isSkipping)
            {
                // Safety: if video time is within 0.5s of the end, treat it as done
                if (videoPlayer.length > 0 && videoPlayer.time >= videoPlayer.length - 0.5)
                    break;
                yield return null;
            }

            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        else
        {
            Debug.Log($"[Prologue] No VideoPlayer found. Falling back to {fallbackWaitTime} second timer...");
            float timer = 0;
            while (timer < fallbackWaitTime && !_isSkipping)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
        
        if (!_isSkipping)
        {
            yield return FinishAndLoadNextScene();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        _videoFinished = true;
    }

    public void SkipPrologue()
    {
        if (_isSkipping) return;
        _isSkipping = true;

        if (_mainSequenceCoroutine != null)
        {
            StopCoroutine(_mainSequenceCoroutine);
        }
        
        StartCoroutine(SkipSequence());
    }

    private IEnumerator SkipSequence()
    {
        Debug.Log("[Prologue] Skip requested! Fading out audio...");

        // Just fade the audio out — the LoadingScene handles the visual transition!
        float fadeTime = 0.5f;
        float startVolume = videoPlayer != null ? videoPlayer.GetDirectAudioVolume(0) : 1f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.SetDirectAudioVolume(0, Mathf.Lerp(startVolume, 0f, t));
            yield return null;
        }

        if (videoPlayer != null) videoPlayer.Stop();
        yield return FinishAndLoadNextScene();
    }

    private IEnumerator FinishAndLoadNextScene()
    {
        // 2. Fade BGM back in before leaving
        if (BGMManager.Instance != null && AudioManager.instance != null)
        {
            Debug.Log("[Prologue] Fading BGM back in...");
            BGMManager.Instance.FadeVolume(AudioManager.instance.musicVolume, 2.0f);
        }

        Debug.Log($"[Prologue] Sequence finished for {introType}. Updating progress and loading next scene.");
        
        if (UserProfileManager.Instance != null)
        {
            Task task = null;
            
            switch (introType)
            {
                case IntroType.Prologue:
                    task = UserProfileManager.Instance.SetPrologueSeen(true);
                    break;
                case IntroType.IlocosIntro:
                    task = UserProfileManager.Instance.SetIlocosIntroSeen(true);
                    break;
                case IntroType.CebuIntro:
                    task = UserProfileManager.Instance.SetCebuIntroSeen(true);
                    break;
            }

            if (task != null)
            {
                // Wait for DB update, but with a 5-second timeout so a failed call never blocks us
                float timeout = 5f;
                float elapsed = 0f;
                while (!task.IsCompleted && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                
                if (!task.IsCompleted)
                    Debug.LogWarning($"[PrologueController] DB update timed out for {introType}. Proceeding anyway.");
            }
        }

        if (sceneLoader != null)
        {
            sceneLoader.LoadScene(nextSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}
