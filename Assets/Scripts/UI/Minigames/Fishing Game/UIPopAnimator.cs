using UnityEngine;
using System.Collections;

/// <summary>
/// Attach this to any panel (WinPanel, LosePanel, HowToPlayPanel).
/// Call PopIn() whenever you want the panel to animate in.
/// IL2CPP-safe: the coroutine runs ON the panel, so it can never
/// touch a disabled object.
/// </summary>
public class UIPopAnimator : MonoBehaviour
{
    [Tooltip("Total duration of the pop-in bounce (seconds).")]
    public float duration = 0.3f;

    private float _elapsed = 0f;
    private bool _isAnimating = false;

    private void OnDisable()
    {
        _isAnimating = false;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Call this right after SetActive(true) to play the pop-in animation.
    /// </summary>
    public void PopIn()
    {
        if (!gameObject.activeInHierarchy) return;
        
        transform.localScale = Vector3.zero;
        _elapsed = 0f;
        _isAnimating = true;
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.deltaTime;
        float half = duration * 0.5f;

        if (_elapsed < half)
        {
            float t = _elapsed / half;
            transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.1f, t);
        }
        else if (_elapsed < duration)
        {
            float t = (_elapsed - half) / half;
            transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, t);
        }
        else
        {
            transform.localScale = Vector3.one;
            _isAnimating = false;
        }
    }
}
