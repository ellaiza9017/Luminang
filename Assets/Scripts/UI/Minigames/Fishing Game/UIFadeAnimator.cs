using UnityEngine;
using System.Collections;

/// <summary>
/// Attach this to any panel that should FADE IN (like a dim background overlay).
/// Requires a CanvasGroup component on the same GameObject.
/// Call FadeIn() right after SetActive(true).
/// IL2CPP-safe: coroutine lives on the same object it animates.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIFadeAnimator : MonoBehaviour
{
    [Tooltip("How long the fade-in takes in seconds.")]
    public float duration = 0.25f;

    private CanvasGroup _cg;
    private Coroutine _anim;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Call this right after SetActive(true) to fade the panel in.
    /// </summary>
    public void FadeIn()
    {
        if (_cg == null) _cg = GetComponent<CanvasGroup>();
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(DoFadeIn());
    }

    private IEnumerator DoFadeIn()
    {
        _cg.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        _cg.alpha = 1f;
        _anim = null;
    }
}
