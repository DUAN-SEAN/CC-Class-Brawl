using UnityEngine;

namespace ClassBrawl.Presentation
{
    /// <summary>
    /// Tuning parameters for the main menu. All gameplay values are
    /// data-driven via this ScriptableObject.
    /// <para>
    /// Source: design/ux/main-menu-visual-design.md sections 5.2-5.6.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "MainMenuTuning", menuName = "Class Brawl/UI/Main Menu Tuning")]
    public class MainMenuTuningData : ScriptableObject
    {
        [Header("Entrance Animation")]
        [Tooltip("Total duration of the staggered entrance sequence (seconds)")]
        public float IntroDuration = 1.3f;

        [Tooltip("Reduced-motion entrance: single fade duration (seconds)")]
        public float IntroReducedDuration = 0.3f;

        [Header("CTA Pulse")]
        [Tooltip("CTA border pulse period (seconds)")]
        public float CtaPulsePeriod = 2.0f;

        [Tooltip("CTA border pulse minimum opacity")]
        [Range(0f, 1f)]
        public float CtaPulseMinOpacity = 0.4f;

        [Tooltip("CTA border pulse maximum opacity")]
        [Range(0f, 1f)]
        public float CtaPulseMaxOpacity = 1.0f;

        [Tooltip("Fixed border opacity in reduced-motion mode")]
        [Range(0f, 1f)]
        public float CtaPulseReducedOpacity = 0.7f;

        [Header("Background Breathing")]
        [Tooltip("Background breathing light period (seconds)")]
        public float BreathingPeriod = 4.0f;

        [Tooltip("Breathing opacity center value")]
        [Range(0.9f, 1f)]
        public float BreathingCenter = 0.975f;

        [Tooltip("Breathing opacity amplitude")]
        [Range(0f, 0.05f)]
        public float BreathingAmplitude = 0.025f;

        [Header("Scene Transition")]
        [Tooltip("Fade-out duration when starting battle (seconds)")]
        public float ExitFadeDuration = 0.4f;

        [Tooltip("Scene load timeout before showing error (seconds)")]
        public float SceneLoadTimeout = 5.0f;

        [Tooltip("Target scene name for battle")]
        public string GameSceneName = "GameScene";

        [Header("Modal Dialog")]
        [Tooltip("Modal open animation duration (seconds)")]
        public float ModalOpenDuration = 0.2f;

        [Tooltip("Modal close animation duration (seconds)")]
        public float ModalCloseDuration = 0.15f;

        [Tooltip("Reduced-motion modal duration (seconds)")]
        public float ModalReducedDuration = 0.15f;

        [Header("Button Feedback")]
        [Tooltip("Scale factor when button is pressed")]
        [Range(0.9f, 1f)]
        public float ButtonPressScale = 0.97f;

        [Tooltip("Scale factor when CTA has focus")]
        [Range(1f, 1.1f)]
        public float CtaFocusScale = 1.02f;

        [Tooltip("Button press animation duration (seconds)")]
        public float ButtonPressDuration = 0.05f;

        [Tooltip("Button release animation duration (seconds)")]
        public float ButtonReleaseDuration = 0.1f;
    }
}
