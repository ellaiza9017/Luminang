using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Luminang.UI.Minigames.IsometricGame
{
    /// <summary>
    /// Controls dialogue bubbles and typewriter text in the Directions / Isometric Minigame.
    ///
    /// FEATURES:
    ///   - Speech bubble that pops/inflates from a small point with a bounce.
    ///   - Typewriter text effect with customizable character tick sounds.
    ///   - Tap anywhere to advance to the next line or skip the typewriter.
    ///   - Automatically inherits global bubble pop & typing sounds from IsometricGameplayManager.
    /// </summary>
    public class IsometricIntroController : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────

        [Header("Bubble Visual")]
        [Tooltip("The RectTransform of the speech bubble image. Scale-pops in on show.")]
        public RectTransform bubbleRoot;

        [Tooltip("The TMP text component inside the bubble.")]
        public TextMeshProUGUI dialogueText;

        [Tooltip("Optional small icon shown when the player can tap to continue.")]
        public GameObject tapToContinueIndicator;

        [Header("Typewriter Settings")]
        [Tooltip("Seconds between each character. 0.03 is typical.")]
        [Range(0.01f, 0.2f)]
        public float typingSpeed = 0.03f;

        [Header("Bubble Pop-In Animation")]
        [Tooltip("Duration of the bubble scale-up animation in seconds.")]
        public float popDuration = 0.3f;

        [Tooltip("Overshoot scale for the bounce feel (e.g. 1.12 = 12% overshoot).")]
        public float popOvershoot = 1.12f;

        [Header("Dialogue Lines")]
        [Tooltip("All lines Rodrick says in order. Supports \\n for line breaks.")]
        public List<string> dialogueLines = new List<string>
        {
            "Hoy! Ikaw na bag-o dinhi?",
            "Sunod-sunod ta, okay? Ipakita ko nimo ang dalan.",
            "Paminaw sa akong instruksyon!"
        };

        [Header("Audio Settings (Local Overrides)")]
        [Tooltip("The AudioSource to play bubble and typing sounds. Auto-finds one if left empty.")]
        public AudioSource audioSource;
        [Tooltip("Optional: Local pop SFX. If empty, uses global bubblePopClip from GameplayManager.")]
        public AudioClip bubblePopClip;
        [Tooltip("Optional: Local typing SFX. If empty, uses global bubbleTypingClip from GameplayManager.")]
        public AudioClip typingClip;
        [Tooltip("Optional: Local tap SFX. If empty, uses global bubbleTapAdvanceClip from GameplayManager.")]
        public AudioClip tapAdvanceClip;

        [Header("Events")]
        [Tooltip("Invoked after the last dialogue line is advanced past.")]
        public UnityEngine.Events.UnityEvent OnIntroComplete;

        [Tooltip("Invoked when a specific dialogue line starts typing. Passes the line index (0-based).")]
        public UnityEngine.Events.UnityEvent<int> OnLineStarted;

        // ─────────────────────────────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────────────────────────────

        private int       _currentLine        = 0;
        private bool      _isTyping           = false;
        private bool      _skipTyping         = false;
        private bool      _waitingForTap      = false;
        private bool      _introStarted       = false;
        private Coroutine _typeCoroutine;
        private Coroutine _popCoroutine;

        // Saved in Awake so pop-in always restores the editor scale, not a hardcoded (1,1,1)
        private Vector3 _originalBubbleScale = Vector3.one;

        // Reused across TypeLine calls — avoids per-character string allocations on mobile
        private readonly StringBuilder _typewriterBuffer = new StringBuilder(256);

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = GetComponentInParent<AudioSource>();
                if (audioSource == null)
                    audioSource = FindFirstObjectByType<AudioSource>();
            }

            if (bubbleRoot != null)
            {
                _originalBubbleScale = bubbleRoot.localScale;
                bubbleRoot.localScale = Vector3.zero;
                bubbleRoot.gameObject.SetActive(false);
            }
            if (tapToContinueIndicator != null)
                tapToContinueIndicator.SetActive(false);
        }

        private void Start()
        {
            if (dialogueText != null)
            {
                dialogueText.enableAutoSizing = true;
                dialogueText.fontSizeMin = 18f;
                dialogueText.fontSizeMax = 32f;
            }
        }

        private void Update()
        {
            if (!_introStarted) return;

            bool tapped = false;

            #if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                tapped = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                tapped = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                tapped = true;
            }
            #endif

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                tapped = true;
            }
            #endif

            if (!tapped) return;

            // Play tap advance sound
            AudioClip tapClip = GetTapAdvanceClip();
            if (audioSource != null && tapClip != null)
            {
                audioSource.PlayOneShot(tapClip);
            }

            if (_isTyping)
            {
                // First tap while typing: skip to full text
                _skipTyping = true;
            }
            else if (_waitingForTap)
            {
                // Second tap after typing finishes: advance to next line
                AdvanceLine();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Audio Fallback Helpers (Inherits from GameplayManager if local is null)
        // ─────────────────────────────────────────────────────────────────

        private AudioClip GetBubblePopClip()
        {
            if (bubblePopClip != null) return bubblePopClip;
            if (IsometricGameplayManager.Instance != null) return IsometricGameplayManager.Instance.bubblePopClip;
            return null;
        }

        private AudioClip GetTypingClip()
        {
            if (typingClip != null) return typingClip;
            if (IsometricGameplayManager.Instance != null) return IsometricGameplayManager.Instance.bubbleTypingClip;
            return null;
        }

        private AudioClip GetTapAdvanceClip()
        {
            if (tapAdvanceClip != null) return tapAdvanceClip;
            if (IsometricGameplayManager.Instance != null) return IsometricGameplayManager.Instance.bubbleTapAdvanceClip;
            return null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins the intro sequence. Call this from your game manager after the scene loads.
        /// </summary>
        public void StartIntro()
        {
            if (_introStarted) return;
            _introStarted = true;

            if (dialogueLines == null || dialogueLines.Count == 0)
            {
                Debug.LogWarning("[IsometricIntroController] No dialogue lines configured. Firing OnIntroComplete immediately.", this);
                OnIntroComplete?.Invoke();
                return;
            }

            _currentLine = 0;
            ShowBubble();
        }

        /// <summary>
        /// Displays a single line of text automatically (no clicking required).
        /// Pops in, types the text, holds for a duration, and pops out.
        /// </summary>
        public void ShowDialogueAuto(string line, float holdDuration, Action onComplete)
        {
            gameObject.SetActive(true);
            bubbleRoot.gameObject.SetActive(true);
            if (tapToContinueIndicator != null) tapToContinueIndicator.SetActive(false);
            if (dialogueText != null) dialogueText.text = "";

            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            _popCoroutine = StartCoroutine(PopInBubble(() =>
            {
                if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
                _typeCoroutine = StartCoroutine(TypeLineAuto(line, holdDuration, onComplete));
            }));
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private void ShowBubble()
        {
            if (bubbleRoot == null) return;

            bubbleRoot.gameObject.SetActive(true);
            if (dialogueText != null) dialogueText.text = "";

            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            _popCoroutine = StartCoroutine(PopInBubble(() => DisplayLine(_currentLine)));
        }

        private void DisplayLine(int index)
        {
            if (dialogueLines == null || index >= dialogueLines.Count) return;

            string line = dialogueLines[index];
            if (dialogueText != null) dialogueText.text = "";

            if (tapToContinueIndicator != null) tapToContinueIndicator.SetActive(false);

            // Notify sequence manager that this specific line has started
            OnLineStarted?.Invoke(index);

            if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
            _typeCoroutine = StartCoroutine(TypeLine(line));
        }

        private void AdvanceLine()
        {
            _waitingForTap = false;
            _currentLine++;

            if (_currentLine >= dialogueLines.Count)
            {
                // All lines done
                HideBubble();
                return;
            }

            DisplayLine(_currentLine);
        }

        private void HideBubble()
        {
            if (tapToContinueIndicator != null) tapToContinueIndicator.SetActive(false);
            
            StartCoroutine(PopOutBubble(() =>
            {
                _introStarted = false;
                OnIntroComplete?.Invoke();
            }));
        }

        // ─────────────────────────────────────────────────────────────────
        // Coroutines
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Inflates the bubble from scale 0 → 1 with a quick overshoot bounce.
        /// </summary>
        private IEnumerator PopInBubble(Action onComplete)
        {
            if (bubbleRoot == null) yield break;

            AudioClip popSfx = GetBubblePopClip();
            if (audioSource != null && popSfx != null)
            {
                audioSource.PlayOneShot(popSfx);
            }

            float elapsed = 0f;
            Vector3 target = _originalBubbleScale;

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);

                float normalizedScale = Mathf.Sin(t * Mathf.PI * 0.5f);
                float overshootScale  = Mathf.Lerp(0f, popOvershoot, normalizedScale);
                if (t > 0.7f)
                    overshootScale = Mathf.Lerp(popOvershoot, 1f, (t - 0.7f) / 0.3f);

                bubbleRoot.localScale = target * overshootScale;
                yield return null;
            }

            bubbleRoot.localScale = target;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Deflates the bubble from scale 1 → 0.
        /// </summary>
        private IEnumerator PopOutBubble(Action onComplete)
        {
            if (bubbleRoot == null) { onComplete?.Invoke(); yield break; }

            float elapsed  = 0f;
            float duration = popDuration * 0.6f;
            Vector3 startScale = _originalBubbleScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = 1f - Mathf.Clamp01(elapsed / duration);
                float eased = t * t;
                bubbleRoot.localScale = startScale * eased;
                yield return null;
            }

            bubbleRoot.localScale = Vector3.zero;
            bubbleRoot.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Typewriter effect: reveals text character by character.
        /// Tapping during typing skips to the full text.
        /// </summary>
        private IEnumerator TypeLine(string line)
        {
            _isTyping   = true;
            _skipTyping = false;

            if (dialogueText != null)
            {
                _typewriterBuffer.Clear();
                dialogueText.text = "";

                AudioClip typeSfx = GetTypingClip();
                int charCount = 0;
                foreach (char c in line)
                {
                    if (_skipTyping)
                    {
                        dialogueText.text = line;
                        _typewriterBuffer.Clear();
                        break;
                    }

                    _typewriterBuffer.Append(c);
                    dialogueText.text = _typewriterBuffer.ToString();

                    if (!char.IsWhiteSpace(c) && audioSource != null && typeSfx != null)
                    {
                        if (charCount % 2 == 0)
                        {
                            audioSource.PlayOneShot(typeSfx);
                        }
                    }
                    charCount++;

                    yield return new WaitForSecondsRealtime(typingSpeed);
                }
            }

            _isTyping      = false;
            _skipTyping    = false;
            _waitingForTap = true;

            if (tapToContinueIndicator != null)
                tapToContinueIndicator.SetActive(true);
        }

        private IEnumerator TypeLineAuto(string line, float holdDuration, Action onComplete)
        {
            _isTyping = true;
            _skipTyping = false;

            if (dialogueText != null)
            {
                _typewriterBuffer.Clear();
                dialogueText.text = "";

                AudioClip typeSfx = GetTypingClip();
                int charCount = 0;
                foreach (char c in line)
                {
                    _typewriterBuffer.Append(c);
                    dialogueText.text = _typewriterBuffer.ToString();

                    if (!char.IsWhiteSpace(c) && audioSource != null && typeSfx != null)
                    {
                        if (charCount % 2 == 0)
                        {
                            audioSource.PlayOneShot(typeSfx);
                        }
                    }
                    charCount++;

                    yield return new WaitForSecondsRealtime(typingSpeed);
                }
            }

            _isTyping = false;

            yield return new WaitForSecondsRealtime(holdDuration);

            yield return StartCoroutine(PopOutBubble(() =>
            {
                onComplete?.Invoke();
            }));
        }
    }
}
