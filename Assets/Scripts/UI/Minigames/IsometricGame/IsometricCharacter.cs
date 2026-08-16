using System.Collections;
using UnityEngine;

namespace Luminang.UI.Minigames.IsometricGame
{
    /// <summary>
    /// Option B: Character controller that interacts with a single
    /// IsometricSpriteAnimator component and moves the RectTransform.
    /// </summary>
    public class IsometricCharacter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private IsometricSpriteAnimator _animator;
        private IsometricPlayerHeadRenderer _headRenderer;

        [Header("Audio Settings")]
        public AudioSource audioSource;
        public AudioClip footstepClip;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _animator = GetComponentInChildren<IsometricSpriteAnimator>();
            _headRenderer = GetComponentInChildren<IsometricPlayerHeadRenderer>();

            if (_animator == null)
            {
                Debug.LogWarning($"[IsometricCharacter] No IsometricSpriteAnimator found on '{gameObject.name}' or its children!", this);
            }
        }

        /// <summary>
        /// Tells the animator to play the specific animation name (e.g. "Idle", "Walk", "Confused"),
        /// and updates the player's head view angle (Front for Idle, 3/4 for Walk).
        /// </summary>
        public void PlayState(string stateName)
        {
            if (_animator != null)
            {
                _animator.Play(stateName);
            }

            if (_headRenderer != null)
            {
                _headRenderer.SetStateByAnimationName(stateName);
            }
        }

        /// <summary>
        /// Smoothly moves this character's position to the target world position.
        /// Converts the world position to parent-relative local position to ensure
        /// camera panning doesn't disrupt the Lerp.
        /// </summary>
        public IEnumerator MoveTo(Vector3 targetWorldPos, float duration)
        {
            if (transform.parent == null) yield break;

            PlayState("Walk");

            // Play footstep audio loop
            if (audioSource != null && footstepClip != null)
            {
                audioSource.clip = footstepClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            Vector3 localStart = transform.localPosition;
            Vector3 localTarget = transform.parent.InverseTransformPoint(targetWorldPos);

            if (duration <= 0.001f)
            {
                transform.localPosition = localTarget;
                PlayState("Idle");
                if (audioSource != null) audioSource.Stop();
                yield break;
            }

            float elapsed = 0f;

            // Mirror child animator (body) and head renderer X scale depending on walking direction
            float directionX = localTarget.x - localStart.x;
            if (Mathf.Abs(directionX) > 0.01f)
            {
                if (_animator != null)
                {
                    Vector3 scale = _animator.transform.localScale;
                    float originalScaleX = Mathf.Abs(scale.x);
                    scale.x = (directionX < 0) ? -originalScaleX : originalScaleX;
                    _animator.transform.localScale = scale;
                }
                if (_headRenderer != null && _headRenderer.transform != _animator?.transform)
                {
                    Vector3 hScale = _headRenderer.transform.localScale;
                    float origHX = Mathf.Abs(hScale.x);
                    hScale.x = (directionX < 0) ? -origHX : origHX;
                    _headRenderer.transform.localScale = hScale;
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localPosition = Vector3.Lerp(localStart, localTarget, t);
                yield return null;
            }

            transform.localPosition = localTarget;
            PlayState("Idle");

            // Stop footstep audio
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Smoothly moves this character along a multi-point path of world positions.
        /// Always starts from the character's CURRENT position, then walks through all provided
        /// world points in order. Flips sprite horizontally per segment direction.
        /// Time is distributed proportionally to segment distance for uniform speed.
        /// </summary>
        public IEnumerator MoveAlongPath(System.Collections.Generic.List<Vector3> worldPoints, float totalDuration)
        {
            if (transform.parent == null || worldPoints == null || worldPoints.Count < 2) yield break;

            PlayState("Walk");

            // Play footstep audio loop
            if (audioSource != null && footstepClip != null)
            {
                audioSource.clip = footstepClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Build local-space version of the path (relative to parent so panning/zoom doesn't drift)
            // We always prepend the character's CURRENT local position as the true start.
            var localPoints = new System.Collections.Generic.List<Vector3>(worldPoints.Count);
            // First point: character's actual current local pos
            localPoints.Add(transform.localPosition);
            // Remaining points: the path nodes converted to parent-local space
            for (int i = 1; i < worldPoints.Count; i++)
            {
                localPoints.Add(transform.parent.InverseTransformPoint(worldPoints[i]));
            }

            // Calculate total path distance
            float totalDistance = 0f;
            for (int i = 0; i < localPoints.Count - 1; i++)
            {
                totalDistance += Vector3.Distance(localPoints[i], localPoints[i + 1]);
            }

            if (totalDuration <= 0.001f || totalDistance <= 0.001f)
            {
                transform.localPosition = localPoints[localPoints.Count - 1];
                PlayState("Idle");
                if (audioSource != null) audioSource.Stop();
                yield break;
            }

            // Traverse each segment
            for (int i = 0; i < localPoints.Count - 1; i++)
            {
                Vector3 segStart = localPoints[i];
                Vector3 segEnd   = localPoints[i + 1];
                float segDist    = Vector3.Distance(segStart, segEnd);
                if (segDist <= 0.001f) continue;

                float segDuration = (segDist / totalDistance) * totalDuration;

                // Face sprite in the direction of this segment
                float dirX = segEnd.x - segStart.x;
                if (Mathf.Abs(dirX) > 0.01f)
                {
                    if (_animator != null)
                    {
                        Vector3 scale = _animator.transform.localScale;
                        float originalScaleX = Mathf.Abs(scale.x);
                        scale.x = (dirX < 0) ? -originalScaleX : originalScaleX;
                        _animator.transform.localScale = scale;
                    }
                    if (_headRenderer != null && _headRenderer.transform != _animator?.transform)
                    {
                        Vector3 hScale = _headRenderer.transform.localScale;
                        float origHX = Mathf.Abs(hScale.x);
                        hScale.x = (dirX < 0) ? -origHX : origHX;
                        _headRenderer.transform.localScale = hScale;
                    }
                }

                float elapsed = 0f;
                while (elapsed < segDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / segDuration);
                    transform.localPosition = Vector3.Lerp(segStart, segEnd, t);
                    yield return null;
                }

                // Snap exactly to end of segment to avoid floating-point drift
                transform.localPosition = segEnd;
            }

            transform.localPosition = localPoints[localPoints.Count - 1];
            PlayState("Idle");

            if (audioSource != null) audioSource.Stop();
        }
    }
}
