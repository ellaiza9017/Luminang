using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Luminang.UI.Minigames.IsometricGame
{
    /// <summary>
    /// Frame-by-frame sprite animator for UI Image components in the Isometric/Directions minigame.
    ///
    /// HOW TO USE:
    ///   1. Attach to the inner Image child of a character (e.g. Player > Player).
    ///   2. Add entries to the "Animations" list. Each entry needs:
    ///        - A unique name  (e.g. "Idle", "Walk", "Forward", "WrongTurn")
    ///        - Sprite frames in order
    ///        - FPS and loop flag
    ///   3. Set "Default Animation Name" so it auto-plays on Start.
    ///   4. From other scripts call:  myAnimator.Play("Walk");
    ///
    /// MOBILE SAFETY:
    ///   - Zero heap allocations in Update.
    ///   - Uses Time.deltaTime (frame-rate independent).
    ///   - No Animator / AnimationClip assets needed.
    /// </summary>
    public class IsometricSpriteAnimator : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────
        // Data
        // ─────────────────────────────────────────────────────────────────

        [Serializable]
        public class SpriteAnimation
        {
            [Tooltip("Unique name used to trigger this animation via Play(name).")]
            public string name;

            [Tooltip("Sprite frames in playback order.")]
            public Sprite[] frames;

            [Tooltip("Frames per second for this animation.")]
            [Range(1f, 60f)]
            public float fps = 10f;

            [Tooltip("If true the animation loops. If false it freezes on the last frame.")]
            public bool loop = true;
        }

        // ─────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────

        [Header("Animation Library")]
        [Tooltip("All animation states for this character.")]
        public SpriteAnimation[] animations;

        [Header("Startup")]
        [Tooltip("Name of the animation to play on Start.")]
        public string defaultAnimationName = "Idle";

        // ─────────────────────────────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────────────────────────────

        private Image  _image;
        private int    _currentClipIndex  = -1;
        private int    _currentFrameIndex =  0;
        private float  _timer             =  0f;
        private bool   _isPlaying         =  true;

        // Built once in Awake — O(1) lookup with no GC
        private Dictionary<string, int> _nameToIndex;

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
                if (_image == null)
                {
                    Debug.LogWarning($"[IsometricSpriteAnimator] '{gameObject.name}' needs an Image component.", this);
                    return;
                }
            }

            if (_nameToIndex == null)
            {
                int count = animations != null ? animations.Length : 0;
                _nameToIndex = new Dictionary<string, int>(count, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < count; i++)
                {
                    if (animations[i] != null && !string.IsNullOrEmpty(animations[i].name))
                        _nameToIndex[animations[i].name] = i;
                }
            }
        }

        private void Start()
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(defaultAnimationName))
                Play(defaultAnimationName);
        }

        private void Update()
        {
            if (!_isPlaying || _currentClipIndex < 0 || _image == null) return;

            if (animations == null || _currentClipIndex >= animations.Length) return;
            SpriteAnimation clip = animations[_currentClipIndex];
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;

            _timer += Time.deltaTime;
            float timePerFrame = 1f / Mathf.Max(clip.fps, 0.01f);

            if (_timer >= timePerFrame)
            {
                _timer -= timePerFrame;
                _currentFrameIndex++;

                if (_currentFrameIndex >= clip.frames.Length)
                {
                    if (clip.loop)
                    {
                        _currentFrameIndex = 0;
                    }
                    else
                    {
                        _currentFrameIndex = clip.frames.Length - 1;
                        _isPlaying = false;
                    }
                }

                if (_currentFrameIndex >= 0 && _currentFrameIndex < clip.frames.Length)
                {
                    Sprite next = clip.frames[_currentFrameIndex];
                    if (next != null) _image.sprite = next;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Switches to the named animation, restarting from frame 0.
        /// Safe to call every frame — won't restart if already playing the same clip.
        /// </summary>
        public void Play(string animationName)
        {
            EnsureInitialized();

            if (_nameToIndex == null || !_nameToIndex.TryGetValue(animationName, out int index))
            {
                // Soft warning, does not crash
                return;
            }

            // Don't restart if already playing this clip
            if (_currentClipIndex == index && _isPlaying) return;

            _currentClipIndex  = index;
            _currentFrameIndex = 0;
            _timer             = 0f;
            _isPlaying         = true;

            // Show first frame immediately
            if (animations != null && _currentClipIndex < animations.Length)
            {
                SpriteAnimation clip = animations[_currentClipIndex];
                if (clip != null && clip.frames != null && clip.frames.Length > 0 && clip.frames[0] != null && _image != null)
                    _image.sprite = clip.frames[0];
            }
        }

        /// <summary>Pauses the animation on the current frame.</summary>
        public void Pause() => _isPlaying = false;

        /// <summary>Resumes a paused animation.</summary>
        public void Resume() => _isPlaying = true;

        /// <summary>Name of the currently playing animation, or empty if none.</summary>
        public string CurrentAnimation =>
            (_currentClipIndex >= 0 && animations != null && _currentClipIndex < animations.Length)
                ? animations[_currentClipIndex].name
                : string.Empty;

        /// <summary>Returns true if the named animation is currently playing.</summary>
        public bool IsPlaying(string animationName)
        {
            if (_nameToIndex == null || !_nameToIndex.TryGetValue(animationName, out int index)) return false;
            return _currentClipIndex == index && _isPlaying;
        }
    }
}
