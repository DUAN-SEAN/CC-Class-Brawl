using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClassBrawl.Presentation
{
    /// <summary>
    /// Manages the main menu entrance sequence animation.
    /// Implements the staggered fade-in described in
    /// design/ux/main-menu-visual-design.md section 5.3.
    /// <para>
    /// Sequence (total ~1.3s):
    /// 1. Background opacity 0% to 100% (0.0s, 0.5s, cubic ease-out)
    /// 2. Title CN/EN fade in (0.4s, 0.3s, cubic ease-out)
    /// 3. Top bar + footer fade in (0.7s, 0.25s, cubic ease-out)
    /// 4. CTA button scale 0.95 to 1.0 + opacity (0.85s, 0.25s, cubic ease-out)
    /// 5. CTA pulse starts (1.1s)
    /// 6. Focus set to CTA button (1.3s)
    /// </para>
    /// <para>
    /// In reduced-motion mode, the stagger is replaced by a single
    /// 0.3s opacity fade for all elements.
    /// </para>
    /// </summary>
    public static class MainMenuEntranceAnimation
    {
        /// <summary>
        /// Plays the full entrance sequence on the main menu root element.
        /// Calls <paramref name="onComplete"/> when the sequence finishes
        /// and focus has been set.
        /// </summary>
        /// <param name="menuRoot">The MainMenu root VisualElement.</param>
        /// <param name="isReducedMotion">True if reduced-motion mode is active.</param>
        /// <param name="onComplete">Callback invoked when the sequence is done.</param>
        public static void Play(VisualElement menuRoot, bool isReducedMotion, Action onComplete = null)
        {
            if (menuRoot == null) return;

            if (isReducedMotion)
            {
                PlayReducedMotion(menuRoot, onComplete);
                return;
            }

            PlayFullSequence(menuRoot, onComplete);
        }

        private static void PlayFullSequence(VisualElement menuRoot, Action onComplete)
        {
            // Cache element references
            VisualElement background = menuRoot.Q("BackgroundLayer");
            VisualElement titleContainer = menuRoot.Q("TitleContainer");
            VisualElement topBar = menuRoot.Q("TopBar");
            VisualElement footer = menuRoot.Q("Footer");
            VisualElement ctaButton = menuRoot.Q("StartBattleButton");

            // Root: fade in
            menuRoot.style.opacity = 0f;
            menuRoot.RemoveFromClassList("fade-out");
            menuRoot.AddToClassList("fade-in");

            // Step 1: Background (0.0s start, 0.5s duration)
            if (background != null)
            {
                background.style.opacity = 0f;
                HUDAnimator.TweenOpacity(background, 0f, 1f, 0.5f, HUDAnimator.CubicEaseOut);
                background.AddToClassList("entrance-visible");
            }

            // Step 2: Title (0.4s start, 0.3s duration)
            if (titleContainer != null)
            {
                titleContainer.style.opacity = 0f;
                titleContainer.schedule.Execute(() =>
                {
                    HUDAnimator.TweenOpacity(titleContainer, 0f, 1f, 0.3f, HUDAnimator.CubicEaseOut);
                    titleContainer.AddToClassList("entrance-visible");
                }).StartingIn(400);
            }

            // Step 3: Top bar + footer (0.7s start, 0.25s duration)
            if (topBar != null)
            {
                topBar.style.opacity = 0f;
                topBar.schedule.Execute(() =>
                {
                    HUDAnimator.TweenOpacity(topBar, 0f, 1f, 0.25f, HUDAnimator.CubicEaseOut);
                    topBar.AddToClassList("entrance-visible");
                }).StartingIn(700);
            }

            if (footer != null)
            {
                footer.style.opacity = 0f;
                footer.schedule.Execute(() =>
                {
                    HUDAnimator.TweenOpacity(footer, 0f, 1f, 0.25f, HUDAnimator.CubicEaseOut);
                    footer.AddToClassList("entrance-visible");
                }).StartingIn(700);
            }

            // Step 4: CTA button (0.85s start, 0.25s duration, scale 0.95 -> 1.0)
            if (ctaButton != null)
            {
                ctaButton.style.opacity = 0f;
                ctaButton.style.scale = new Scale(Vector3.one * 0.95f);
                ctaButton.schedule.Execute(() =>
                {
                    HUDAnimator.TweenOpacity(ctaButton, 0f, 1f, 0.25f, HUDAnimator.CubicEaseOut);
                    HUDAnimator.TweenScale(ctaButton, 0.95f, 1.0f, 0.25f, HUDAnimator.CubicEaseOut);
                    ctaButton.AddToClassList("entrance-visible");
                }).StartingIn(850);
            }

            // Step 5 + 6: Start pulse + set focus (1.3s)
            menuRoot.schedule.Execute(() =>
            {
                onComplete?.Invoke();
            }).StartingIn(1300);
        }

        private static void PlayReducedMotion(VisualElement menuRoot, Action onComplete)
        {
            // Simplified: single 0.3s fade for all elements
            menuRoot.style.opacity = 0f;
            menuRoot.RemoveFromClassList("fade-out");
            menuRoot.AddToClassList("fade-in");

            HUDAnimator.TweenOpacity(menuRoot, 0f, 1f, 0.3f, HUDAnimator.CubicEaseOut, () =>
            {
                // Mark all elements visible immediately
                var elements = new[] { "BackgroundLayer", "TitleContainer", "TopBar", "Footer", "StartBattleButton" };
                foreach (string name in elements)
                {
                    VisualElement el = menuRoot.Q(name);
                    if (el != null)
                    {
                        el.style.opacity = 1f;
                        el.AddToClassList("entrance-visible");
                    }
                }

                onComplete?.Invoke();
            });
        }
    }
}
