using UnityEngine;

namespace ClassBrawl.Presentation
{
    /// <summary>
    /// HUD tuning parameters. All gameplay-facing visual values are data-driven
    /// through this ScriptableObject. Designers can create instances with
    /// different presets for balancing or accessibility.
    /// <para>
    /// Source: design/gdd/battle-hud.md Tuning Knobs (10 items) + MaxSkillsPerMatch.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "HUDTuningData", menuName = "ClassBrawl/HUDTuningData")]
    public class HUDTuningData : ScriptableObject
    {
        [Header("Damage Percent")]
        [Tooltip("Scale multiplier applied during damage number bounce (1.0 = no bounce)")]
        [Range(1.0f, 1.5f)]
        public float DamageNumberBounceScale = 1.3f;

        [Tooltip("Duration of the damage number bounce animation in seconds")]
        [Range(0.05f, 0.30f)]
        public float DamageNumberBounceDuration = 0.15f;

        [Header("Focus Bar")]
        [Tooltip("Base pulse frequency (Hz) when focus bar is above threshold")]
        [Range(0.5f, 2.0f)]
        public float FocusPulseMinFrequency = 1.0f;

        [Tooltip("Maximum pulse frequency (Hz) when focus bar is near full")]
        [Range(2.0f, 5.0f)]
        public float FocusPulseMaxFrequency = 3.0f;

        [Tooltip("Fill ratio at which pulse acceleration begins")]
        [Range(0.6f, 0.95f)]
        public float FocusPulseThreshold = 0.8f;

        [Header("Skill Slots")]
        [Tooltip("Size of each skill slot in pixels (1920x1080 base)")]
        [Range(32f, 64f)]
        public float SkillSlotSize = 48f;

        [Tooltip("Duration of the skill equip pop-in animation in seconds")]
        [Range(0.1f, 0.5f)]
        public float SkillEquipAnimDuration = 0.25f;

        [Tooltip("Maximum number of skill slots per player per match")]
        public int MaxSkillsPerMatch = 4;

        [Header("HUD Global")]
        [Tooltip("Duration of HUD fade-in when entering Countdown in seconds")]
        [Range(0.1f, 0.5f)]
        public float HudFadeInDuration = 0.3f;

        [Tooltip("Duration of HUD fade-out when entering Results in seconds")]
        [Range(0.1f, 0.5f)]
        public float HudFadeOutDuration = 0.3f;

        [Tooltip("Seconds before displaying a stale-data indicator when events stop")]
        [Range(2.0f, 10.0f)]
        public float DataStaleTimeout = 5.0f;

        [Header("Skill Selection")]
        [Tooltip("Number of candidate skills shown during skill draw")]
        public int CandidateCount = 3;

        [Tooltip("Timeout in seconds before auto-selecting the first skill")]
        public float SelectionTimeout = 5.0f;
    }
}
