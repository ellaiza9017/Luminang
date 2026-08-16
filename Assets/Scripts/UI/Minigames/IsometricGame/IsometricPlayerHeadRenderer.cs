using UnityEngine;
using UnityEngine.UI;

namespace Luminang.UI.Minigames.IsometricGame
{
    /// <summary>
    /// Swaps the Player's Head & Hair RawImage texture between Front View (Idle)
    /// and 3/4 View (Walk) using the RenderTextures from CharacterPreviewSetupFront
    /// and CharacterPreviewSetup3/4.
    /// </summary>
    [ExecuteInEditMode]
    public class IsometricPlayerHeadRenderer : MonoBehaviour
    {
        public enum HeadViewState
        {
            Front,      // Idle
            Quarter34   // Walk
        }

        [Header("Render Textures")]
        [Tooltip("The RenderTexture from CharacterPreviewSetupFront's camera.")]
        public RenderTexture frontRenderTexture;

        [Tooltip("The RenderTexture from CharacterPreviewSetup3/4's camera.")]
        public RenderTexture threeQuarterRenderTexture;

        [Header("2D UI Display")]
        [Tooltip("The RawImage on Player > HeadAndHair that displays the head.")]
        public RawImage headDisplayUI;

        [Header("3D Character Outfit Initializers (Optional)")]
        [Tooltip("PlayerOutfitInitializer on CharacterPreviewSetupFront.")]
        public PlayerOutfitInitializer frontOutfitInitializer;

        [Tooltip("PlayerOutfitInitializer on CharacterPreviewSetup3/4.")]
        public PlayerOutfitInitializer threeQuarterOutfitInitializer;

        [Header("Current State")]
        public HeadViewState currentViewState = HeadViewState.Front;

        private void Awake()
        {
            if (headDisplayUI == null)
            {
                headDisplayUI = GetComponent<RawImage>();
            }
        }

        private void Start()
        {
            ApplyViewState(currentViewState);
            RefreshOutfits();
        }

        public void RefreshOutfits()
        {
            if (frontOutfitInitializer != null) frontOutfitInitializer.InitializeOutfit();
            if (threeQuarterOutfitInitializer != null) threeQuarterOutfitInitializer.InitializeOutfit();
        }

        public void SetViewState(HeadViewState state)
        {
            currentViewState = state;
            ApplyViewState(state);
        }

        /// <summary>
        /// Automatically switches between Front (Idle) and 3/4 (Walk) based on animation name.
        /// </summary>
        public void SetStateByAnimationName(string animName)
        {
            if (string.Equals(animName, "Walk", System.StringComparison.OrdinalIgnoreCase))
            {
                SetViewState(HeadViewState.Quarter34);
            }
            else
            {
                SetViewState(HeadViewState.Front);
            }
        }

        private void ApplyViewState(HeadViewState state)
        {
            if (headDisplayUI == null) return;

            if (state == HeadViewState.Front)
            {
                if (frontRenderTexture != null)
                    headDisplayUI.texture = frontRenderTexture;
            }
            else
            {
                if (threeQuarterRenderTexture != null)
                    headDisplayUI.texture = threeQuarterRenderTexture;
                else if (frontRenderTexture != null)
                    headDisplayUI.texture = frontRenderTexture;
            }
        }
    }
}
