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

    private Coroutine _anim;

    /// <summary>
    /// Call this right after SetActive(true) to play the pop-in animation.
    /// </summary>
    public void PopIn()
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(DoPopIn());
    }

    private IEnumerator DoPopIn()
    {
        transform.localScale = Vector3.zero;

        float half = duration * 0.5f;
        float elapsed = 0f;

        // Scale up to 1.1x
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.1f, t);
            yield return null;
        }

        // Settle back to exactly 1x
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, t);
            yield return null;
        }

        transform.localScale = Vector3.one;
        _anim = null;
    }
}
