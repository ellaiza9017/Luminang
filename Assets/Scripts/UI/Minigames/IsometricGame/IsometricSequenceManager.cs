using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Luminang.UI.Minigames.IsometricGame
{
    /// <summary>
    /// Master controller for the Intro Sequence in the Isometric Minigame.
    /// Controls the title reveal, black overlays, and UI Rodrick entrance.
    ///
    /// HOW TO SETUP IN EDITOR:
    ///   1. Create empty "SequenceManager" under GameGroup. Attach this script.
    ///   2. Position WoodHeader and UI Rodrick at their FINAL resting positions.
    ///      The script reads those positions on Start and uses them as the target.
    ///   3. Assign all references below in the Inspector.
    /// </summary>
    public class IsometricSequenceManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────
        // Inspector – WoodHeader Title Reveal
        // ─────────────────────────────────────────────────────────────────

        [Header("Wood Header Title Reveal")]
        [Tooltip("The WoodHeader RectTransform.")]
        public RectTransform woodHeader;

        [Tooltip("The black overlay Image that dims the screen during the title reveal.")]
        public Image woodHeaderBlackOverlay;

        [Tooltip("How dark the overlay is during the title reveal (0 = transparent, 1 = fully black).")]
        [Range(0f, 1f)]
        public float headerOverlayAlpha = 0.75f;

        [Tooltip("The scale of the WoodHeader when centered. 1 = its normal editor size. 1.5 = 50% bigger.")]
        public Vector2 headerCenteredScale = new Vector2(1.5f, 1.5f);

        [Tooltip("How long the title stays centered before sliding up (seconds).")]
        public float titleHoldDuration = 2f;

        [Tooltip("How long the slide-up animation takes (seconds).")]
        public float headerSlideDuration = 1f;

        // ─────────────────────────────────────────────────────────────────
        // Inspector – Rodrick Intro Dialogue
        // ─────────────────────────────────────────────────────────────────

        [Header("Rodrick Intro Dialogue")]
        [Tooltip("The intro-only Rodrick Image (NOT the game Rodrick in WorldContainer).")]
        public RectTransform dialogueRodrick;

        [Tooltip("The black overlay Image shown behind Rodrick during his dialogue.")]
        public Image dialogueBlackOverlay;

        [Tooltip("How dark the overlay is during Rodrick's dialogue.")]
        [Range(0f, 1f)]
        public float dialogueOverlayAlpha = 0.65f;

        [Tooltip("The IsometricIntroController on the ChatBubble.")]
        public IsometricIntroController introController;

        [Tooltip("How long it takes for Rodrick to slide in from off-screen (seconds).")]
        public float rodrickSlideDuration = 0.5f;

        [Tooltip("How far off-screen Rodrick starts (canvas pixels below the screen).")]
        public float rodrickStartOffsetY = 1200f;

        // ─────────────────────────────────────────────────────────────────
        // Inspector – Errand Paper & HowToPlay
        // ─────────────────────────────────────────────────────────────────

        [Header("Errand Paper Intro")]
        [Tooltip("The Errand Paper RectTransform.")]
        public RectTransform errandPaper;

        [Tooltip("Where the paper sits during dialogue line 3 (focused position).")]
        public Vector2 errandPaperFocusPos = new Vector2(200f, 0f); // Default to slightly right of center

        [Tooltip("How big the paper is during the focus phase.")]
        public Vector3 errandPaperFocusScale = new Vector3(3f, 3f, 1f);

        [Tooltip("How long the paper takes to slide in/around.")]
        public float errandPaperSlideDuration = 0.5f;

        [Header("Audio Settings")]
        public AudioSource audioSource;
        [Tooltip("SFX played when the errand paper slides in or out.")]
        public AudioClip errandPaperSlideClip;

        [Header("How To Play Popup")]
        [Tooltip("The parent GameObject containing the How To Play instructions.")]
        public GameObject howToPlayGroup;

        [Tooltip("The button used to close the How To Play screen and start the game.")]
        public Button closeHowToPlayButton;

        [Tooltip("The Patience Bar CanvasGroup to fade in.")]
        public CanvasGroup patienceBarGroup;

        [Header("Intro to Game Transition")]
        [Tooltip("The Player script in the world.")]
        public IsometricCharacter transitionPlayer;

        [Tooltip("The Rodrick script in the world.")]
        public IsometricCharacter transitionRodrick;

        [Tooltip("Where the player snaps to start the game.")]
        public RectTransform transitionPlayerWaypoint;

        [Tooltip("Where Rodrick snaps to start the game.")]
        public RectTransform transitionRodrickWaypoint;

        [Tooltip("A black full-screen overlay for the fade transition.")]
        public Image transitionOverlay;

        [Tooltip("Distance characters walk forward before the screen fades out.")]
        public float walkForwardDistance = 150f;

        [Tooltip("Duration of the walking forward part.")]
        public float transitionWalkDuration = 1.5f;

        [Tooltip("Duration of the fade in/out.")]
        public float transitionFadeDuration = 0.5f;

        // ─────────────────────────────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────────────────────────────

        private Vector2 _headerTargetPos;
        private Vector3 _headerOriginalScale;
        private Vector2 _rodrickTargetPos;
        private Vector2 _errandPaperHUDPos;
        private Vector3 _errandPaperHUDScale;

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────

        private void Start()
        {
            // Cache the FINAL resting positions/scales as set in the Editor
            if (woodHeader != null)
            {
                _headerTargetPos    = woodHeader.anchoredPosition;
                _headerOriginalScale = woodHeader.localScale;
            }

            if (dialogueRodrick != null)
                _rodrickTargetPos = dialogueRodrick.anchoredPosition;

            // --- Set up initial state ---

            // WoodHeader: start centered and big
            if (woodHeader != null)
            {
                woodHeader.anchoredPosition = Vector2.zero;
                woodHeader.localScale       = new Vector3(headerCenteredScale.x, headerCenteredScale.y, 1f);
            }

            // Rodrick: start off-screen below
            if (dialogueRodrick != null)
                dialogueRodrick.anchoredPosition = _rodrickTargetPos + new Vector2(0f, -rodrickStartOffsetY);

            // Header overlay: visible
            SetOverlayAlpha(woodHeaderBlackOverlay, headerOverlayAlpha, true);

            // Dialogue overlay: hidden
            SetOverlayAlpha(dialogueBlackOverlay, 0f, false);

            // Errand Paper: Cache its HUD position & scale, then start off-screen to the right
            if (errandPaper != null)
            {
                _errandPaperHUDPos = errandPaper.anchoredPosition;
                _errandPaperHUDScale = errandPaper.localScale;
                errandPaper.anchoredPosition = new Vector2(3000f, errandPaperFocusPos.y);
            }

            // How To Play: hidden initially
            if (howToPlayGroup != null)
                howToPlayGroup.SetActive(false);

            // Patience Bar: start transparent
            if (patienceBarGroup != null)
                patienceBarGroup.alpha = 0f;

            // Transition Overlay: ensure it starts transparent and enabled
            if (transitionOverlay != null)
            {
                transitionOverlay.gameObject.SetActive(true);
                var c = transitionOverlay.color;
                c.a = 0f;
                transitionOverlay.color = c;
            }

            // Listen for specific dialogue lines
            if (introController != null)
                introController.OnLineStarted.AddListener(OnDialogueLineStarted);

            StartCoroutine(PlaySequence());
        }

        // ─────────────────────────────────────────────────────────────────
        // Main sequence
        // ─────────────────────────────────────────────────────────────────

        private IEnumerator PlaySequence()
        {
            // == PHASE 1: Hold the title in the center ==
            yield return new WaitForSeconds(titleHoldDuration);

            // == PHASE 2: Slide header up + shrink it + fade out overlay ==
            float elapsed = 0f;
            while (elapsed < headerSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t    = Mathf.Clamp01(elapsed / headerSlideDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic (smooth deceleration)

                // Slide position: center → target
                if (woodHeader != null)
                {
                    woodHeader.anchoredPosition = Vector2.Lerp(Vector2.zero, _headerTargetPos, ease);

                    // Shrink scale: centered big → original editor scale
                    woodHeader.localScale = Vector3.Lerp(
                        new Vector3(headerCenteredScale.x, headerCenteredScale.y, 1f),
                        _headerOriginalScale,
                        ease
                    );
                }

                // Fade overlay: opaque → transparent
                if (woodHeaderBlackOverlay != null)
                {
                    float alpha = Mathf.Lerp(headerOverlayAlpha, 0f, ease);
                    var c = woodHeaderBlackOverlay.color;
                    c.a = alpha;
                    woodHeaderBlackOverlay.color = c;
                }

                yield return null;
            }

            // Make sure everything is exactly at target
            if (woodHeader != null)
            {
                woodHeader.anchoredPosition = _headerTargetPos;
                woodHeader.localScale       = _headerOriginalScale;
            }
            SetOverlayAlpha(woodHeaderBlackOverlay, 0f, false);

            // Small pause before Rodrick slides in
            yield return new WaitForSeconds(0.15f);

            // == PHASE 3: Fade in dialogue overlay + Rodrick slides up ==
            SetOverlayAlpha(dialogueBlackOverlay, 0f, true); // Enable but transparent first

            elapsed = 0f;
            Vector2 rodrickStart = dialogueRodrick != null
                ? dialogueRodrick.anchoredPosition
                : Vector2.zero;

            while (elapsed < rodrickSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t    = Mathf.Clamp01(elapsed / rodrickSlideDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (dialogueRodrick != null)
                    dialogueRodrick.anchoredPosition = Vector2.Lerp(rodrickStart, _rodrickTargetPos, ease);

                if (dialogueBlackOverlay != null)
                {
                    var c = dialogueBlackOverlay.color;
                    c.a = Mathf.Lerp(0f, dialogueOverlayAlpha, ease);
                    dialogueBlackOverlay.color = c;
                }

                yield return null;
            }

            if (dialogueRodrick != null)
                dialogueRodrick.anchoredPosition = _rodrickTargetPos;
            SetOverlayAlpha(dialogueBlackOverlay, dialogueOverlayAlpha, true);

            // == PHASE 4: Start dialogue ==
            if (introController != null)
            {
                introController.OnIntroComplete.AddListener(OnDialogueComplete);
                introController.StartIntro();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Dialogue Tracking
        // ─────────────────────────────────────────────────────────────────

        private void OnDialogueLineStarted(int index)
        {
            // Index 3 is: "and she wrote all these directions in the local language!"
            if (index == 3)
            {
                StartCoroutine(SlideErrandPaperTo(
                    new Vector2(3000f, errandPaperFocusPos.y), 
                    errandPaperFocusPos,
                    _errandPaperHUDScale,
                    errandPaperFocusScale
                ));
            }
        }

        private IEnumerator SlideErrandPaperTo(Vector2 startPos, Vector2 endPos, Vector3 startScale, Vector3 endScale)
        {
            if (errandPaper == null) yield break;

            if (audioSource != null && errandPaperSlideClip != null)
            {
                audioSource.PlayOneShot(errandPaperSlideClip);
            }

            float elapsed = 0f;
            while (elapsed < errandPaperSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t    = Mathf.Clamp01(elapsed / errandPaperSlideDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic

                errandPaper.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
                errandPaper.localScale = Vector3.Lerp(startScale, endScale, ease);
                yield return null;
            }

            errandPaper.anchoredPosition = endPos;
            errandPaper.localScale = endScale;
        }

        // ─────────────────────────────────────────────────────────────────
        // Dialogue complete → exit sequence
        // ─────────────────────────────────────────────────────────────────

        private void OnDialogueComplete()
        {
            if (introController != null)
            {
                introController.OnIntroComplete.RemoveListener(OnDialogueComplete);
                introController.OnLineStarted.RemoveListener(OnDialogueLineStarted);
            }

            StartCoroutine(ExitSequence());
        }

        private IEnumerator ExitSequence()
        {
            // == PHASE 5: Slide Rodrick out + fade overlay ==
            float elapsed = 0f;
            Vector2 rodrickStart = dialogueRodrick != null
                ? dialogueRodrick.anchoredPosition
                : Vector2.zero;
            Vector2 rodrickEnd = rodrickStart + new Vector2(0f, -rodrickStartOffsetY);

            while (elapsed < rodrickSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t    = Mathf.Clamp01(elapsed / rodrickSlideDuration);
                float ease = t * t; // Ease-in quad (accelerate out)

                if (dialogueRodrick != null)
                    dialogueRodrick.anchoredPosition = Vector2.Lerp(rodrickStart, rodrickEnd, ease);

                if (dialogueBlackOverlay != null)
                {
                    var c = dialogueBlackOverlay.color;
                    c.a = Mathf.Lerp(dialogueOverlayAlpha, 0f, ease);
                    dialogueBlackOverlay.color = c;
                }

                yield return null;
            }

            SetOverlayAlpha(dialogueBlackOverlay, 0f, false);
            if (dialogueRodrick != null) dialogueRodrick.gameObject.SetActive(false);

            // Play slide SFX when paper returns to HUD
            if (audioSource != null && errandPaperSlideClip != null)
            {
                audioSource.PlayOneShot(errandPaperSlideClip);
            }

            // Slide Errand Paper from Focus to its original HUD position and scale, and fade in patience bar
            float slideElapsed = 0f;
            Vector2 startPos = errandPaperFocusPos;
            Vector2 endPos = _errandPaperHUDPos;
            Vector3 startScale = errandPaperFocusScale;
            Vector3 endScale = _errandPaperHUDScale;

            while (slideElapsed < errandPaperSlideDuration)
            {
                slideElapsed += Time.deltaTime;
                float t    = Mathf.Clamp01(slideElapsed / errandPaperSlideDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic

                if (errandPaper != null)
                {
                    errandPaper.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
                    errandPaper.localScale = Vector3.Lerp(startScale, endScale, ease);
                }

                if (patienceBarGroup != null)
                {
                    patienceBarGroup.alpha = ease;
                }
                yield return null;
            }

            if (errandPaper != null)
            {
                errandPaper.anchoredPosition = endPos;
                errandPaper.localScale = endScale;
            }
            if (patienceBarGroup != null) patienceBarGroup.alpha = 1f;

            // Show How To Play screen
            if (howToPlayGroup != null)
            {
                howToPlayGroup.SetActive(true);
                if (closeHowToPlayButton != null)
                {
                    // Ensure we don't double-register if played twice
                    closeHowToPlayButton.onClick.RemoveListener(StartGameplay);
                    closeHowToPlayButton.onClick.AddListener(StartGameplay);
                }
                else
                {
                    Debug.LogWarning("[IsometricSequenceManager] closeHowToPlayButton is not assigned! Game will not start properly.", this);
                }
            }
            else
            {
                // Fallback if no HowToPlayGroup is assigned
                StartGameplay();
            }
        }

        /// <summary>
        /// Call this to dismiss the How To Play screen and transition into gameplay.
        /// </summary>
        public void CloseHowToPlay()
        {
            StartGameplay();
        }

        private void StartGameplay()
        {
            if (howToPlayGroup != null) howToPlayGroup.SetActive(false);
            if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.RemoveListener(StartGameplay);

            StartCoroutine(RunTransitionToGameplay());
        }

        private IEnumerator RunTransitionToGameplay()
        {
            // 1. Play walk states
            if (transitionPlayer != null) transitionPlayer.PlayState("Walk");
            if (transitionRodrick != null) transitionRodrick.PlayState("Walk");

            // Cache RectTransforms before the loop — prevents per-frame GetComponent calls on Android
            RectTransform playerRT  = transitionPlayer  != null ? transitionPlayer.GetComponent<RectTransform>()  : null;
            RectTransform rodrickRT = transitionRodrick != null ? transitionRodrick.GetComponent<RectTransform>() : null;

            Vector2 playerStart  = playerRT  != null ? playerRT.anchoredPosition  : Vector2.zero;
            Vector2 rodrickStart = rodrickRT != null ? rodrickRT.anchoredPosition : Vector2.zero;

            // Move them forward (downwards) while fading screen to black
            float elapsed = 0f;
            while (elapsed < transitionWalkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionWalkDuration);

                if (playerRT  != null) playerRT.anchoredPosition  = playerStart  + new Vector2(0f, -walkForwardDistance * t);
                if (rodrickRT != null) rodrickRT.anchoredPosition = rodrickStart + new Vector2(0f, -walkForwardDistance * t);

                // Fade screen to black over the latter half
                if (transitionOverlay != null)
                {
                    float fadeT = Mathf.Clamp01((elapsed - (transitionWalkDuration - transitionFadeDuration)) / transitionFadeDuration);
                    var c = transitionOverlay.color;
                    c.a = fadeT;
                    transitionOverlay.color = c;
                }

                yield return null;
            }

            // 2. Ensure screen is fully black, snap characters to target waypoints
            if (transitionOverlay != null)
            {
                var c = transitionOverlay.color;
                c.a = 1f;
                transitionOverlay.color = c;
            }

            if (transitionPlayer != null && transitionPlayerWaypoint != null)
            {
                transitionPlayer.transform.position = transitionPlayerWaypoint.position;
                transitionPlayer.PlayState("Idle");
            }
            if (transitionRodrick != null && transitionRodrickWaypoint != null)
            {
                transitionRodrick.transform.position = transitionRodrickWaypoint.position;
                transitionRodrick.PlayState("Idle");
            }

            // Small pause while screen is black
            yield return new WaitForSeconds(0.2f);

            // 3. Fade screen back to clear
            elapsed = 0f;
            while (elapsed < transitionFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionFadeDuration);

                if (transitionOverlay != null)
                {
                    var c = transitionOverlay.color;
                    c.a = 1f - t;
                    transitionOverlay.color = c;
                }

                yield return null;
            }

            if (transitionOverlay != null)
            {
                var c = transitionOverlay.color;
                c.a = 0f;
                transitionOverlay.color = c;
            }

            // Start the gameplay manager scenarios here!
            if (IsometricGameplayManager.Instance != null)
            {
                IsometricGameplayManager.Instance.StartGame();
            }
            else
            {
                Debug.LogWarning("[IsometricSequenceManager] IsometricGameplayManager.Instance is null! Cannot start scenarios.");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetOverlayAlpha(Image overlay, float alpha, bool active)
        {
            if (overlay == null) return;
            overlay.gameObject.SetActive(active);
            var c = overlay.color;
            c.a   = alpha;
            overlay.color = c;
        }
    }
}
