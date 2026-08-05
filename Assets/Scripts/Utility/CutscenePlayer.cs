using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Plays an optional cut‑scene video. If no video is assigned it simply
/// waits a short fallback duration so the gameplay flow does not stall.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class CutscenePlayer : MonoBehaviour
{
    [Tooltip("Assign a VideoClip for the cutscene. Leave empty for fallback pause.")]
    public VideoClip videoClip;

    // Fallback wait time in seconds when no video is set.
    public float fallbackDuration = 2f;

    private VideoPlayer _vp;
    private bool _isSkipping = false;

    private void Awake()
    {
        _vp = GetComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.waitForFirstFrame = true;
        _vp.isLooping = false;
    }

    /// <summary>
    /// Play the assigned video or wait for the fallback duration.
    /// Use via: <c>yield return StartCoroutine(cutscenePlayer.Play());</c>
    /// </summary>
    public IEnumerator Play()
    {
        _isSkipping = false;
        
        if (videoClip != null)
        {
            _vp.clip = videoClip;
            _vp.Prepare();
            while (!_vp.isPrepared && !_isSkipping) yield return null;
            
            if (!_isSkipping)
            {
                _vp.Play();
                
                // Wait a split second for Unity engine to spin up the video
                yield return new WaitForSeconds(0.1f);
                
                while (_vp.isPlaying && !_isSkipping) yield return null;
            }
        }
        else
        {
            Debug.Log("[CutscenePlayer] No video assigned – using fallback pause.");
            float timer = 0;
            while (timer < fallbackDuration && !_isSkipping)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
        
        // Wait for the skip fade out to finish before yielding control back
        while (_isSkipping && _vp.isPlaying) yield return null;
    }

    /// <summary>
    /// Call this from a UI Button to skip the current cutscene.
    /// </summary>
    public void Skip()
    {
        if (_isSkipping) return;
        _isSkipping = true;
        StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator FadeOutAndStop()
    {
        // Just fade audio — let the SceneLoader/LoadingScene handle the visual transition
        float fadeTime = 0.5f;
        float startVolume = _vp != null ? _vp.GetDirectAudioVolume(0) : 1f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            if (_vp != null && _vp.isPlaying)
                _vp.SetDirectAudioVolume(0, Mathf.Lerp(startVolume, 0f, t));
            yield return null;
        }

        if (_vp != null) _vp.Stop();
    }
}
