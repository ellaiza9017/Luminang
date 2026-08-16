using UnityEngine;
using UnityEngine.UI;

namespace Luminang.UI.Minigames.IsometricGame
{
    /// <summary>
    /// Canvas-space camera follow for the Isometric/Directions minigame.
    ///
    /// Since everything is on a UI Canvas (no real 3D camera movement),
    /// this script pans a "WorldContainer" RectTransform so the midpoint
    /// between the Player and Rodrick stays centered on screen.
    ///
    /// HOW TO SET UP IN THE EDITOR:
    ///   1. Create an empty GameObject called "WorldContainer" inside GameGroup.
    ///   2. Make Background, Rodrick (parent), and Player (parent) children of WorldContainer.
    ///   3. Keep the HUD elements (WoodHeader, Round, MenuButton) OUTSIDE WorldContainer.
    ///   4. Attach this script to an empty GameObject called "IsometricCamera" (or any name).
    ///   5. Assign all references in the Inspector.
    ///
    /// MOBILE SAFETY:
    ///   - No heap allocations in LateUpdate.
    ///   - Uses Canvas.scaleFactor so it works on all screen sizes.
    ///   - Smooth lerp with configurable speed.
    /// </summary>
    [DisallowMultipleComponent]
    public class IsometricCameraFollow : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────

        [Header("World References")]
        [Tooltip("The RectTransform that contains Background + all characters. This is what gets panned.")]
        public RectTransform worldContainer;

        [Tooltip("The Canvas this UI lives on. Required for scaleFactor.")]
        public Canvas canvas;

        [Header("Follow Targets")]
        [Tooltip("The Player's RectTransform (the parent, not the inner Image).")]
        public RectTransform playerTransform;

        [Tooltip("Rodrick's RectTransform (the parent, not the inner Image).")]
        public RectTransform rodrickTransform;

        [Header("NPC Target (optional)")]
        [Tooltip("Active NPC target to include in the camera focus.")]
        public RectTransform activeNpcTransform;

        [Header("Follow Settings")]
        [Tooltip("How quickly the camera pans to the target. Higher = snappier.")]
        [Range(1f, 30f)]
        public float smoothSpeed = 8f;

        [Tooltip("0 = center on Rodrick, 0.5 = true midpoint, 1 = center on Player.")]
        [Range(0f, 1f)]
        public float playerBias = 0.6f;

        [Header("Zoom Settings")]
        [Tooltip("Zoom level of the camera. 1 = normal, 2 = 2x zoom in, 0.5 = zoom out.")]
        [Range(0.1f, 5f)]
        public float zoomLevel = 1.2f;

        [Header("Bounds (optional)")]
        [Tooltip("If true, clamps panning so the world edges are not revealed.")]
        public bool clampToBounds = true;

        [Tooltip("Half-size of the background in canvas pixels (X = horizontal, Y = vertical). " +
                 "Set this to half the Background Image's Width and Height.")]
        public Vector2 worldHalfSize = new Vector2(1920f, 1080f);

        [Header("NPC Pull Weight")]
        [Tooltip("How strongly the camera pulls toward the active NPC. 0 = ignore NPC, 1 = lock on NPC.")]
        [Range(0f, 1f)]
        public float npcPullWeight = 0.35f;

        // ─────────────────────────────────────────────────────────────────
        // Runtime camera override (set by GameplayManager for specific scenes)
        // ─────────────────────────────────────────────────────────────────

        // When non-null, the camera smoothly transitions toward this zoom + offset
        // and smoothly returns to inspector defaults when cleared.
        private bool   _hasOverride       = false;
        private float  _overrideZoom      = 1f;
        private float  _overrideYOffset   = 0f;   // extra canvas-space Y shift applied AFTER pan calculation
        private float  _overrideNpcPull   = 0.35f;

        // Current interpolated values (lerped toward either override or defaults each frame)
        private float _currentZoom      = -1f;   // -1 sentinel → snap to zoomLevel on first frame
        private float _currentYOffset   = 0f;
        private float _currentNpcPull   = 0.35f;

        [Tooltip("Speed at which zoom/offset overrides transition in and out.")]
        public float overrideTransitionSpeed = 3f;

        // ─────────────────────────────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────────────────────────────

        private Vector2 _targetPos;
        private RectTransform _canvasRect;
        private bool _followEnabled = true;

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (worldContainer == null)
            {
                Debug.LogError("[IsometricCameraFollow] worldContainer is not assigned!", this);
                enabled = false;
                return;
            }

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[IsometricCameraFollow] No Canvas found!", this);
                    enabled = false;
                    return;
                }
            }

            _canvasRect = canvas.GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            if (!_followEnabled) return;
            if (playerTransform == null && rodrickTransform == null) return;

            // ── Smoothly interpolate override values toward their targets ──
            float snap = Time.deltaTime * overrideTransitionSpeed;
            float targetZoom    = _hasOverride ? _overrideZoom    : zoomLevel;
            float targetYOff    = _hasOverride ? _overrideYOffset : 0f;
            float targetNpcPull = _hasOverride ? _overrideNpcPull : npcPullWeight;

            // First-frame snap so there's no slide-in from a stale value
            if (_currentZoom < 0f)
            {
                _currentZoom    = targetZoom;
                _currentYOffset = targetYOff;
                _currentNpcPull = targetNpcPull;
            }
            else
            {
                _currentZoom    = Mathf.Lerp(_currentZoom,    targetZoom,    snap);
                _currentYOffset = Mathf.Lerp(_currentYOffset, targetYOff,    snap);
                _currentNpcPull = Mathf.Lerp(_currentNpcPull, targetNpcPull, snap);
            }

            // 1. Compute the weighted midpoint in world-container local space
            Vector2 playerLocal  = GetLocalPos(playerTransform);
            Vector2 rodrickLocal = GetLocalPos(rodrickTransform);

            Vector2 midpoint;
            if (playerTransform != null && rodrickTransform != null)
                midpoint = Vector2.Lerp(rodrickLocal, playerLocal, playerBias);
            else if (playerTransform != null)
                midpoint = playerLocal;
            else
                midpoint = rodrickLocal;

            // If there is an active NPC, pull the camera focus toward them
            if (activeNpcTransform != null)
            {
                Vector2 npcLocal = GetLocalPos(activeNpcTransform);
                midpoint = Vector2.Lerp(midpoint, npcLocal, _currentNpcPull);
            }

            // 2. Apply zoom
            worldContainer.localScale = new Vector3(_currentZoom, _currentZoom, 1f);

            // 3. Pan the world so the midpoint sits at canvas center, then apply any Y offset
            _targetPos = -midpoint * _currentZoom;
            _targetPos.y += _currentYOffset;   // positive = shift world UP = camera looks more south

            // 4. Clamp so we don't see beyond the world edges
            if (clampToBounds && _canvasRect != null)
            {
                Vector2 screenHalf = _canvasRect.sizeDelta * 0.5f;
                float maxX = Mathf.Max(0f, (worldHalfSize.x * _currentZoom) - screenHalf.x);
                float maxY = Mathf.Max(0f, (worldHalfSize.y * _currentZoom) - screenHalf.y);
                _targetPos.x = Mathf.Clamp(_targetPos.x, -maxX, maxX);
                _targetPos.y = Mathf.Clamp(_targetPos.y, -maxY, maxY);
            }

            // 5. Smooth lerp
            worldContainer.anchoredPosition = Vector2.Lerp(
                worldContainer.anchoredPosition,
                _targetPos,
                Time.deltaTime * smoothSpeed
            );
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the anchoredPosition of a RectTransform relative to worldContainer.
        /// Falls back to (0,0) if null.
        /// </summary>
        private Vector2 GetLocalPos(RectTransform rt)
        {
            if (rt == null) return Vector2.zero;

            // Convert the target's world position to the WorldContainer's local space
            Vector3 worldPos = rt.position;
            Vector3 localPos = worldContainer.InverseTransformPoint(worldPos);
            return new Vector2(localPos.x, localPos.y);
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Temporarily disables camera panning (e.g. during a dialogue cutscene).
        /// </summary>
        public void DisableFollow() => _followEnabled = false;

        /// <summary>
        /// Re-enables camera panning.
        /// </summary>
        public void EnableFollow() => _followEnabled = true;

        /// <summary>
        /// Pushes a camera override that smoothly zooms and shifts the view.
        /// The camera lerps toward these values over <see cref="overrideTransitionSpeed"/> seconds.
        /// Call <see cref="ClearCameraOverride"/> to smoothly restore inspector defaults.
        /// </summary>
        /// <param name="zoom">Target zoom level (e.g. 0.9 to zoom out slightly).</param>
        /// <param name="yOffset">
        ///   Extra canvas-space Y added to the pan target.
        ///   Positive = world shifts up = camera looks more toward the south of the scene.
        /// </param>
        /// <param name="npcPull">NPC pull weight for this override (0–1). Default 0.35.</param>
        public void PushCameraOverride(float zoom, float yOffset, float npcPull = 0.35f)
        {
            _overrideZoom    = zoom;
            _overrideYOffset = yOffset;
            _overrideNpcPull = npcPull;
            _hasOverride     = true;
        }

        /// <summary>
        /// Smoothly restores the camera to its inspector-defined zoom and zero offset.
        /// </summary>
        public void ClearCameraOverride()
        {
            _hasOverride = false;
        }

        /// <summary>
        /// Instantly snaps the camera to the target without lerping.
        /// Call this on scene init to avoid a slide-in from (0,0).
        /// </summary>
        public void SnapToTarget()
        {
            if (worldContainer == null) return;

            Vector2 playerLocal  = GetLocalPos(playerTransform);
            Vector2 rodrickLocal = GetLocalPos(rodrickTransform);

            Vector2 midpoint;
            if (playerTransform != null && rodrickTransform != null)
                midpoint = Vector2.Lerp(rodrickLocal, playerLocal, playerBias);
            else if (playerTransform != null)
                midpoint = playerLocal;
            else
                midpoint = rodrickLocal;

            if (activeNpcTransform != null)
            {
                Vector2 npcLocal = GetLocalPos(activeNpcTransform);
                midpoint = Vector2.Lerp(midpoint, npcLocal, _currentNpcPull);
            }

            _currentZoom    = _hasOverride ? _overrideZoom    : zoomLevel;
            _currentYOffset = _hasOverride ? _overrideYOffset : 0f;
            worldContainer.localScale       = new Vector3(_currentZoom, _currentZoom, 1f);
            worldContainer.anchoredPosition = -midpoint * _currentZoom + new Vector2(0f, _currentYOffset);
        }
    }
}
