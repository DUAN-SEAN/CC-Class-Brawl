using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClassBrawl.Presentation
{
    /// <summary>
    /// Static utility class providing easing functions and tween helpers
    /// for HUD animations. All animations are driven via UI Toolkit's
    /// <c>VisualElement.schedule</c> to avoid coroutines and keep logic
    /// off the game thread.
    /// <para>
    /// Easing function allocation follows Art Bible section 7.3 and
    /// the visual design spec section 5.1:
    /// - EaseOutBack: damage bounce, KO popup only
    /// - Cubic ease-out: default for all non-elastic entries
    /// - Linear: equip phase 1, flash effects
    /// - Sine: continuous pulse loops
    /// </para>
    /// </summary>
    public static class HUDAnimator
    {
        // ================================================================
        // Easing Functions
        // ================================================================

        /// <summary>
        /// EaseOutBack easing. Produces a slight overshoot then settle.
        /// Use ONLY for damage bounce and KO popup (Art Bible constraint).
        /// </summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;
            return 1.0f + c3 * Mathf.Pow(t - 1.0f, 3f) + c1 * Mathf.Pow(t - 1.0f, 2f);
        }

        /// <summary>
        /// Cubic ease-out. Default easing for most HUD entries.
        /// Fast arrive, slow settle -- matches hard-edge geometric style.
        /// </summary>
        public static float CubicEaseOut(float t)
        {
            return 1.0f - Mathf.Pow(1.0f - t, 3f);
        }

        /// <summary>
        /// Cubic ease-in. Used for HUD fade-out and panel close.
        /// Starts slow, accelerates away.
        /// </summary>
        public static float CubicEaseIn(float t)
        {
            return t * t * t;
        }

        /// <summary>
        /// Linear interpolation. Used for equip phase 1 and flash effects.
        /// </summary>
        public static float Linear(float t)
        {
            return t;
        }

        // ================================================================
        // Accessibility: Reduced Motion Detection
        // ================================================================

        /// <summary>
        /// Checks whether reduced-motion mode is active for the given element
        /// by walking up the visual tree for a parent with the "reduced-motion" class.
        /// When active, scale/bounce animations are replaced with simple opacity fades.
        /// </summary>
        private static bool IsReducedMotion(VisualElement element)
        {
            if (element == null) return false;
            VisualElement current = element;
            while (current != null)
            {
                if (current.ClassListContains("reduced-motion"))
                    return true;
                current = current.parent;
            }
            return false;
        }

        // ================================================================
        // Generic Tween Helpers
        // ================================================================

        /// <summary>
        /// Tweens the scale property of a <see cref="VisualElement"/> over time
        /// using the specified easing function. Calls <paramref name="onComplete"/>
        /// when the animation finishes.
        /// </summary>
        /// <param name="element">Target UI element.</param>
        /// <param name="from">Starting scale value.</param>
        /// <param name="to">Target scale value.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="easing">Easing function mapping [0,1] to [0,1].</param>
        /// <param name="onComplete">Optional callback invoked on completion.</param>
        public static void TweenScale(
            VisualElement element,
            float from,
            float to,
            float duration,
            Func<float, float> easing,
            Action onComplete = null)
        {
            if (element == null) return;

            float elapsed = 0f;
            element.style.scale = new Scale(Vector3.one * from);

            element.schedule.Execute(() =>
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = easing(t);
                float value = Mathf.LerpUnclamped(from, to, easedT);
                element.style.scale = new Scale(Vector3.one * value);

                if (t >= 1.0f)
                {
                    element.style.scale = new Scale(Vector3.one * to);
                    onComplete?.Invoke();
                }
            }).Every(16).ForDuration((long)(duration * 1000));
        }

        /// <summary>
        /// Tweens the opacity of a <see cref="VisualElement"/> over time
        /// using the specified easing function. Calls <paramref name="onComplete"/>
        /// when the animation finishes.
        /// </summary>
        /// <param name="element">Target UI element.</param>
        /// <param name="from">Starting opacity (0-1).</param>
        /// <param name="to">Target opacity (0-1).</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="easing">Easing function mapping [0,1] to [0,1].</param>
        /// <param name="onComplete">Optional callback invoked on completion.</param>
        public static void TweenOpacity(
            VisualElement element,
            float from,
            float to,
            float duration,
            Func<float, float> easing,
            Action onComplete = null)
        {
            if (element == null) return;

            float elapsed = 0f;
            element.style.opacity = from;

            element.schedule.Execute(() =>
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = easing(t);
                float value = Mathf.LerpUnclamped(from, to, easedT);
                element.style.opacity = value;

                if (t >= 1.0f)
                {
                    element.style.opacity = to;
                    onComplete?.Invoke();
                }
            }).Every(16).ForDuration((long)(duration * 1000));
        }

        // ================================================================
        // Composite Animation Sequences
        // ================================================================

        /// <summary>
        /// Plays the damage percent bounce animation: scale from current
        /// to <paramref name="bounceScale"/> and back to 1.0 using EaseOutBack.
        /// Duration is taken from <paramref name="tuning"/>.
        /// <para>Source: design/ux/hud-visual-design.md section 3.1, animation spec.</para>
        /// </summary>
        /// <param name="element">The damage percent label.</param>
        /// <param name="tuning">Tuning data providing bounce scale and duration.</param>
        public static void PlayBounceSequence(VisualElement element, HUDTuningData tuning)
        {
            if (element == null || tuning == null) return;

            // Reduced-motion: skip bounce, use a brief opacity pulse instead
            if (IsReducedMotion(element))
            {
                TweenOpacity(element, 1.0f, 0.6f, tuning.DamageNumberBounceDuration * 0.5f, Linear, () =>
                {
                    TweenOpacity(element, 0.6f, 1.0f, tuning.DamageNumberBounceDuration * 0.5f, CubicEaseOut);
                });
                return;
            }

            float duration = tuning.DamageNumberBounceDuration;
            float peak = tuning.DamageNumberBounceScale;

            // Phase: scale 1.0 -> peak -> 1.0 using EaseOutBack over full duration
            TweenScale(element, 1.0f, peak, duration * 0.5f, EaseOutBack, () =>
            {
                TweenScale(element, peak, 1.0f, duration * 0.5f, CubicEaseOut);
            });
        }

        /// <summary>
        /// Plays the skill equip two-phase animation:
        /// Phase 1: Scale 0 -> 1.2x linear (0.05s or 20% of total)
        /// Phase 2: Scale 1.2x -> 1.0x cubic ease-out (remaining 80%)
        /// <para>Source: design/ux/hud-visual-design.md section 3.3.</para>
        /// </summary>
        /// <param name="element">The skill slot VisualElement.</param>
        /// <param name="duration">Total animation duration in seconds.</param>
        public static void PlayEquipAnimation(VisualElement element, float duration)
        {
            if (element == null) return;

            // Reduced-motion: skip scale animation, use a simple opacity fade
            if (IsReducedMotion(element))
            {
                element.style.scale = new Scale(Vector3.one);
                TweenOpacity(element, 0f, 1.0f, duration, Linear);
                return;
            }

            float phase1Duration = duration * 0.2f;  // 20% for scale up
            float phase2Duration = duration * 0.8f;  // 80% for settle

            // Phase 1: 0 -> 1.2x linear
            TweenScale(element, 0f, 1.2f, phase1Duration, Linear, () =>
            {
                // Phase 2: 1.2x -> 1.0x cubic ease-out
                TweenScale(element, 1.2f, 1.0f, phase2Duration, CubicEaseOut);
            });
        }

        /// <summary>
        /// Plays the KO notification sequence:
        /// Phase 1: Scale 0 -> 1.5x, EaseOutBack, 0.3s
        /// Phase 2: Scale 1.5x -> 1.0x, Cubic ease-out, hold 0.5s
        /// Phase 3: Opacity 1.0 -> 0.0, Cubic ease-in, 0.3s
        /// <para>Source: design/ux/hud-visual-design.md section 3.5.</para>
        /// </summary>
        /// <param name="element">The KO notification label.</param>
        /// <param name="onComplete">Optional callback invoked after full sequence.</param>
        public static void PlayKOSequence(VisualElement element, Action onComplete = null)
        {
            if (element == null) return;

            element.style.display = DisplayStyle.Flex;
            element.style.opacity = 1f;

            // Reduced-motion: skip scale, use a simple fade in -> hold -> fade out
            if (IsReducedMotion(element))
            {
                element.style.scale = new Scale(Vector3.one);
                TweenOpacity(element, 0f, 1.0f, 0.2f, CubicEaseOut, () =>
                {
                    // Hold for 0.5s then fade out
                    element.schedule.Execute(() =>
                    {
                        TweenOpacity(element, 1.0f, 0.0f, 0.3f, CubicEaseIn, () =>
                        {
                            element.style.display = DisplayStyle.None;
                            onComplete?.Invoke();
                        });
                    }).StartingIn(500);
                });
                return;
            }

            // Phase 1: scale 0 -> 1.5, EaseOutBack, 0.3s
            TweenScale(element, 0f, 1.5f, 0.3f, EaseOutBack, () =>
            {
                // Phase 2: scale 1.5 -> 1.0 + hold 0.5s
                TweenScale(element, 1.5f, 1.0f, 0.1f, CubicEaseOut, () =>
                {
                    // Hold for 0.5s then fade out
                    element.schedule.Execute(() =>
                    {
                        // Phase 3: fade out 0.3s
                        TweenOpacity(element, 1.0f, 0.0f, 0.3f, CubicEaseIn, () =>
                        {
                            element.style.display = DisplayStyle.None;
                            onComplete?.Invoke();
                        });
                    }).StartingIn(500);
                });
            });
        }
    }
}
