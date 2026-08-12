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
    private float _elapsed = 0f;
    private bool _isAnimating = false;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
    }

    private void OnDisable()
    {
        _isAnimating = false;
        if (_cg != null) _cg.alpha = 1f; // Default to fully visible when disabled
    }

    /// <summary>
    /// Call this right after SetActive(true) to fade the panel in.
    /// </summary>
    public void FadeIn()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_cg == null) _cg = GetComponent<CanvasGroup>();
        
        _cg.alpha = 0f;
        _elapsed = 0f;
        _isAnimating = true;
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.deltaTime;
        _cg.alpha = Mathf.Clamp01(_elapsed / duration);

        if (_elapsed >= duration)
        {
            _cg.alpha = 1f;
            _isAnimating = false;
        }
    }
}
