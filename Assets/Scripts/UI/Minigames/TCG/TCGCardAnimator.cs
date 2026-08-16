using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Luminang.UI.Minigames
{
    public class TCGCardAnimator : MonoBehaviour
    {
        public static TCGCardAnimator Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Moves a card from deck to slot and flips it face up while preserving custom scale and following an arc trajectory.
        /// Uses World Position to ensure perfect positioning across different Canvas parents and anchors.
        /// </summary>
        public IEnumerator ThrowAndFlipCard(RectTransform card, Vector3 fromWorldPos, Vector3 toWorldPos, float duration, Sprite frontSprite, Vector3 targetScale = default, float arcHeight = 35f, System.Action onComplete = null, Vector3 targetEulerAngles = default)
        {
            Vector3 finalScale = targetScale == Vector3.zero ? card.localScale : targetScale;
            if (finalScale == Vector3.zero) finalScale = Vector3.one;

            card.position = fromWorldPos;
            card.localScale = finalScale;
            card.localEulerAngles = targetEulerAngles;

            float elapsed = 0f;
            Image cardImage = card.GetComponent<Image>();
            bool flipped = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth step curve for a relaxed, polished card throw
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // World position with parabolic upward arc
                Vector3 currentPos = Vector3.Lerp(fromWorldPos, toWorldPos, smoothT);
                currentPos.y += Mathf.Sin(smoothT * Mathf.PI) * arcHeight;
                card.position = currentPos;

                // Scale X to simulate 3D card flip
                Vector3 currentScale = finalScale;
                if (smoothT < 0.5f)
                {
                    // Flip edge-on (from frontScale.x to 0)
                    float flipT = smoothT / 0.5f;
                    currentScale.x = Mathf.Lerp(finalScale.x, 0f, flipT);
                }
                else
                {
                    // Swap sprite at edge-on point
                    if (!flipped)
                    {
                        flipped = true;
                        if (cardImage != null && frontSprite != null)
                        {
                            cardImage.sprite = frontSprite;
                        }
                    }

                    // Flip out (from 0 to frontScale.x)
                    float flipT = (smoothT - 0.5f) / 0.5f;
                    currentScale.x = Mathf.Lerp(0f, finalScale.x, flipT);
                }

                card.localScale = currentScale;
                yield return null;
            }

            card.position = toWorldPos;
            card.localScale = finalScale;

            onComplete?.Invoke();
        }

        /// <summary>
        /// Smoothly moves a card from one world position to another.
        /// </summary>
        public IEnumerator MoveCard(RectTransform card, Vector3 fromWorldPos, Vector3 toWorldPos, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                card.position = Vector3.Lerp(fromWorldPos, toWorldPos, smoothT);
                yield return null;
            }
            card.position = toWorldPos;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Shakes a card on a wrong choice.
        /// </summary>
        public IEnumerator ShakeCard(RectTransform card, float duration = 0.4f, float magnitude = 15f)
        {
            Vector3 originalPos = card.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offsetX = Random.Range(-magnitude, magnitude);
                float offsetY = Random.Range(-magnitude, magnitude);
                card.position = originalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            card.position = originalPos;
        }

        /// <summary>
        /// Adds or activates an Outline component on the card to glow a specific color, then fades it out.
        /// </summary>
        public IEnumerator GlowOutline(RectTransform card, Color glowColor, float duration = 1.0f)
        {
            Outline outline = card.GetComponent<Outline>();
            if (outline == null)
            {
                outline = card.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = glowColor;
            outline.effectDistance = new Vector2(8f, 8f);
            outline.enabled = true;

            float elapsed = 0f;
            Color startColor = glowColor;
            Color endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                outline.effectColor = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            outline.enabled = false;
        }

        /// <summary>
        /// Bouncy pop-in animation for panels (HowToPlay, Win, Lose).
        /// </summary>
        public IEnumerator PopIn(Transform panel, float duration = 0.3f, System.Action onComplete = null)
        {
            panel.gameObject.SetActive(true);
            panel.localScale = Vector3.zero;

            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                panel.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                panel.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, t);
                yield return null;
            }

            panel.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Pop-out animation to shrink panels away.
        /// </summary>
        public IEnumerator PopOut(Transform panel, float duration = 0.2f, System.Action onComplete = null)
        {
            float elapsed = 0f;
            Vector3 startScale = panel.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                panel.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            panel.localScale = Vector3.zero;
            panel.gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
