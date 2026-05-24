using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using ClassBrawl.Foundation;

namespace ClassBrawl.Presentation
{
    /// <summary>
    /// Main menu controller. Manages all button interactions, dialog state,
    /// entrance/exit animations, and event dispatching for the main menu screen.
    /// <para>
    /// Architecture: MonoBehaviour on MenuScene, references a UIDocument with
    /// MainMenu.uxml. Follows the same passive-renderer pattern as HUDController
    /// (ADR-0014). Does not directly modify game state -- fires events through
    /// const string event names with Debug.Log placeholders.
    /// </para>
    /// <para>
    /// Source: design/ux/main-menu.md (UX spec)
    ///        design/ux/main-menu-visual-design.md (visual design spec)
    /// </para>
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // ================================================================
        // Serialized References
        // ================================================================

        [Header("UI Document")]
        [Tooltip("The UIDocument rendering MainMenu.uxml")]
        [SerializeField] private UIDocument _menuDocument;

        [Header("Tuning")]
        [Tooltip("Tuning parameters for animation and timing")]
        [SerializeField] private MainMenuTuningData _tuning;

        // ================================================================
        // Private State
        // ================================================================

        // Root
        private VisualElement _menuRoot;

        // Top bar buttons
        private Button _settingsButton;
        private Button _howToPlayButton;
        private Button _quitButton;

        // CTA
        private Button _startBattleButton;
        private Label _ctaMainText;
        private Label _ctaSubText;

        // Footer
        private Label _versionText;
        private Label _copyrightText;

        // Dialog
        private VisualElement _quitOverlay;
        private VisualElement _quitDialog;
        private Label _quitDialogTitle;
        private Button _dialogCancelButton;
        private Button _dialogConfirmButton;
        private IVisualElementScheduledItem _dialogNavHandle;

        // Background
        private VisualElement _backgroundLayer;

        // State tracking
        private bool _initialized;
        private bool _isQuitDialogOpen;
        private bool _isTransitioning;
        private bool _isReducedMotion;
        private bool _isHighContrast;

        // CTA pulse state
        private IVisualElementScheduledItem _pulseHandle;
        private bool _isPulsing;
        private float _pulsePhase;

        // Background breathing state
        private IVisualElementScheduledItem _breathingHandle;
        private bool _isBreathing;

        // Session start time for telemetry
        private float _sessionStartTime;

        // ================================================================
        // Unity Lifecycle
        // ================================================================

        private void Start()
        {
            _sessionStartTime = Time.time;
            Initialize();
        }

        private void Update()
        {
            if (!_initialized) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isQuitDialogOpen)
                    HideQuitDialog();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // ================================================================
        // Initialization
        // ================================================================

        /// <summary>
        /// Initializes the controller: caches UI references, binds callbacks,
        /// and plays the entrance animation sequence.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            CacheUIReferences();
            BindCallbacks();
            PlayEntranceAnimation();

            _initialized = true;
        }

        /// <summary>
        /// Unbinds all callbacks and stops running animations.
        /// Call before destroying or when leaving the menu scene.
        /// </summary>
        public void Cleanup()
        {
            if (!_initialized) return;

            UnbindCallbacks();
            StopPulse();
            StopBreathing();

            _initialized = false;
        }

        // ================================================================
        // Public API (Accessibility)
        // ================================================================

        /// <summary>
        /// Enables or disables high-contrast mode on the main menu.
        /// </summary>
        /// <param name="enabled">True to enable high-contrast mode.</param>
        public void SetHighContrast(bool enabled)
        {
            _isHighContrast = enabled;

            if (_menuRoot != null)
            {
                if (enabled)
                    _menuRoot.AddToClassList("high-contrast");
                else
                    _menuRoot.RemoveFromClassList("high-contrast");
            }
        }

        /// <summary>
        /// Enables or disables reduced-motion mode on the main menu.
        /// When active, entrance stagger is replaced by a single fade,
        /// and CTA pulse / background breathing are stopped.
        /// </summary>
        /// <param name="enabled">True to enable reduced-motion mode.</param>
        public void SetReducedMotion(bool enabled)
        {
            _isReducedMotion = enabled;

            if (_menuRoot != null)
            {
                if (enabled)
                    _menuRoot.AddToClassList("reduced-motion");
                else
                    _menuRoot.RemoveFromClassList("reduced-motion");
            }

            if (enabled)
            {
                StopPulse();
                StopBreathing();

                // Set pulse to fixed state
                if (_startBattleButton != null)
                {
                    _startBattleButton.AddToClassList("pulse-stopped");
                    _startBattleButton.RemoveFromClassList("pulsing");
                }
            }
            else if (_initialized)
            {
                // Restart pulse when disabling reduced-motion
                StartPulse();
                StartBreathing();
            }
        }

        // ================================================================
        // UI Reference Caching
        // ================================================================

        private void CacheUIReferences()
        {
            if (_menuDocument == null)
            {
                Debug.LogError("[MainMenuController] UIDocument reference is null.");
                return;
            }

            _menuRoot = _menuDocument.rootVisualElement.Q("MainMenu");

            // Background
            _backgroundLayer = _menuRoot.Q("BackgroundLayer");

            // Top bar
            _settingsButton = _menuRoot.Q<Button>("SettingsButton");
            _howToPlayButton = _menuRoot.Q<Button>("HowToPlayButton");
            _quitButton = _menuRoot.Q<Button>("QuitButton");

            // CTA
            _startBattleButton = _menuRoot.Q<Button>("StartBattleButton");
            _ctaMainText = _menuRoot.Q<Label>("CTAMainText");
            _ctaSubText = _menuRoot.Q<Label>("CTASubText");

            // Footer
            _versionText = _menuRoot.Q<Label>("VersionText");
            _copyrightText = _menuRoot.Q<Label>("CopyrightText");

            // Dialog
            _quitOverlay = _menuRoot.Q("QuitOverlay");
            _quitDialog = _menuRoot.Q("QuitDialog");
            _quitDialogTitle = _menuRoot.Q<Label>("QuitDialogTitle");
            _dialogCancelButton = _menuRoot.Q<Button>("DialogCancelButton");
            _dialogConfirmButton = _menuRoot.Q<Button>("DialogConfirmButton");

            // Set initial text values
            SetLocalizedText();

            // Navigation ring: CTA -> Settings -> HowToPlay -> Quit -> CTA
            _navRing = new Button[] { _startBattleButton, _settingsButton, _howToPlayButton, _quitButton };
        }

        /// <summary>
        /// Sets localized text on all UI elements.
        /// Currently uses hardcoded Chinese text as placeholder.
        /// When the localization system is implemented, this will
        /// read from localization keys instead.
        /// </summary>
        private void SetLocalizedText()
        {
            // UXML already has Chinese text. This method is a hook for
            // the future localization system to override text at runtime.
            // Currently a no-op; all text is set in UXML.
        }

        // Navigation ring
        private Button[] _navRing;

        // ================================================================
        // Callback Binding
        // ================================================================

        private void BindCallbacks()
        {
            // Top bar
            if (_settingsButton != null)
                _settingsButton.clicked += OnSettingsClicked;

            if (_howToPlayButton != null)
                _howToPlayButton.clicked += OnHowToPlayClicked;

            if (_quitButton != null)
                _quitButton.clicked += OnQuitClicked;

            // CTA
            if (_startBattleButton != null)
                _startBattleButton.clicked += OnStartBattleClicked;

            // Dialog
            if (_dialogCancelButton != null)
                _dialogCancelButton.clicked += OnDialogCancelClicked;

            if (_dialogConfirmButton != null)
                _dialogConfirmButton.clicked += OnDialogConfirmClicked;

            // Navigation ring
            RegisterNavigationRing();
        }

        private void UnbindCallbacks()
        {
            if (_settingsButton != null)
                _settingsButton.clicked -= OnSettingsClicked;

            if (_howToPlayButton != null)
                _howToPlayButton.clicked -= OnHowToPlayClicked;

            if (_quitButton != null)
                _quitButton.clicked -= OnQuitClicked;

            if (_startBattleButton != null)
                _startBattleButton.clicked -= OnStartBattleClicked;

            if (_dialogCancelButton != null)
                _dialogCancelButton.clicked -= OnDialogCancelClicked;

            if (_dialogConfirmButton != null)
                _dialogConfirmButton.clicked -= OnDialogConfirmClicked;
        }

        /// <summary>
        /// Registers NavigationMoveEvent on main menu buttons to implement
        /// the circular focus ring: CTA -> Settings -> HowToPlay -> Quit -> CTA.
        /// </summary>
        private void RegisterNavigationRing()
        {
            if (_navRing == null || _navRing.Length == 0) return;

            for (int i = 0; i < _navRing.Length; i++)
            {
                if (_navRing[i] == null) continue;
                int idx = i;
                _navRing[i].RegisterCallback<NavigationMoveEvent>(evt =>
                {
                    int next = -1;
                    if (evt.direction == NavigationMoveEvent.Direction.Right ||
                        evt.direction == NavigationMoveEvent.Direction.Down)
                    {
                        next = (idx + 1) % _navRing.Length;
                    }
                    else if (evt.direction == NavigationMoveEvent.Direction.Left ||
                             evt.direction == NavigationMoveEvent.Direction.Up)
                    {
                        next = (idx - 1 + _navRing.Length) % _navRing.Length;
                    }

                    if (next >= 0 && next < _navRing.Length && _navRing[next] != null)
                    {
                        evt.StopPropagation();
                        _navRing[next].Focus();
                    }
                });
            }
        }

        // ================================================================
        // Button Handlers
        // ================================================================

        /// <summary>
        /// "Start Battle" button handler. Fires the start battle event
        /// and begins the exit transition (fade out, then scene load).
        /// </summary>
        private void OnStartBattleClicked()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            // Stop decorative animations
            StopPulse();
            StopBreathing();

            // Fire event
            Debug.Log($"[MainMenu] Event: {MainMenuEventNames.OnMainMenuStartBattle}");

            // Play exit transition
            PlayExitTransition();
        }

        /// <summary>
        /// "Settings" button handler. Fires telemetry event.
        /// Settings popup content is not yet implemented (requires design/ux/settings.md).
        /// </summary>
        private void OnSettingsClicked()
        {
            Debug.Log($"[MainMenu] Event: {MainMenuEventNames.OnMainMenuSettingsOpened}");
            // TODO: Open settings popup when design/ux/settings.md is complete
        }

        /// <summary>
        /// "How to Play" button handler. Fires telemetry event.
        /// How-to-play popup content is not yet implemented.
        /// </summary>
        private void OnHowToPlayClicked()
        {
            Debug.Log($"[MainMenu] Event: {MainMenuEventNames.OnMainMenuHowToPlayOpened}");
            // TODO: Open how-to-play popup when design/ux/how-to-play.md is complete
        }

        /// <summary>
        /// "Quit" button handler. Opens the quit confirmation dialog.
        /// </summary>
        private void OnQuitClicked()
        {
            Debug.Log($"[MainMenu] Event: {MainMenuEventNames.OnMainMenuQuitRequested}");
            ShowQuitDialog();
        }

        /// <summary>
        /// Dialog "Cancel" button handler. Closes the quit dialog
        /// and returns focus to the quit button.
        /// </summary>
        private void OnDialogCancelClicked()
        {
            HideQuitDialog();
        }

        /// <summary>
        /// Dialog "Confirm" button handler. Fires the quit confirmed event
        /// and quits the application.
        /// </summary>
        private void OnDialogConfirmClicked()
        {
            float sessionDuration = Time.time - _sessionStartTime;
            Debug.Log($"[MainMenu] Event: {MainMenuEventNames.OnMainMenuQuitConfirmed} (session: {sessionDuration:F1}s)");

            HideQuitDialog();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ================================================================
        // Quit Dialog Management
        // ================================================================

        /// <summary>
        /// Shows the quit confirmation dialog with entrance animation.
        /// Default focus is on the "Cancel" button (prevent accidental quit).
        /// </summary>
        private void ShowQuitDialog()
        {
            if (_quitOverlay == null || _isQuitDialogOpen) return;

            _isQuitDialogOpen = true;

            // Show overlay
            _quitOverlay.style.display = DisplayStyle.Flex;

            // Animate dialog panel
            if (_quitDialog != null)
            {
                _quitDialog.RemoveFromClassList("dialog-exit");
                _quitDialog.AddToClassList("dialog-enter");

                if (_isReducedMotion)
                {
                    // Simplified: opacity only
                    _quitDialog.style.opacity = 0f;
                    HUDAnimator.TweenOpacity(_quitDialog, 0f, 1f, 0.15f, HUDAnimator.CubicEaseOut);
                    _quitDialog.style.scale = ScaleIdentity;
                }
                else
                {
                    // Full: scale + opacity
                    _quitDialog.style.opacity = 0f;
                    _quitDialog.style.scale = new Scale(Vector3.one * 0.95f);
                    HUDAnimator.TweenOpacity(_quitDialog, 0f, 1f, 0.2f, HUDAnimator.CubicEaseOut);
                    HUDAnimator.TweenScale(_quitDialog, 0.95f, 1.0f, 0.2f, HUDAnimator.CubicEaseOut);
                }
            }

            // Set default focus to Cancel button
            _dialogCancelButton?.Focus();

            // Focus trap: Cancel <-> Confirm navigation ring
            RegisterDialogFocusTrap();
        }

        /// <summary>
        /// Registers a focus trap on the quit dialog so that navigation
        /// stays within Cancel <-> Confirm buttons.
        /// </summary>
        private void RegisterDialogFocusTrap()
        {
            if (_dialogCancelButton == null || _dialogConfirmButton == null) return;

            _dialogCancelButton.RegisterCallback<NavigationMoveEvent>(OnDialogCancelNavMove);
            _dialogConfirmButton.RegisterCallback<NavigationMoveEvent>(OnDialogConfirmNavMove);
        }

        /// <summary>
        /// Unregisters the dialog focus trap callbacks.
        /// </summary>
        private void UnregisterDialogFocusTrap()
        {
            if (_dialogCancelButton != null)
                _dialogCancelButton.UnregisterCallback<NavigationMoveEvent>(OnDialogCancelNavMove);
            if (_dialogConfirmButton != null)
                _dialogConfirmButton.UnregisterCallback<NavigationMoveEvent>(OnDialogConfirmNavMove);
        }

        private void OnDialogCancelNavMove(NavigationMoveEvent evt)
        {
            if (!_isQuitDialogOpen) return;
            if (evt.direction == NavigationMoveEvent.Direction.Right ||
                evt.direction == NavigationMoveEvent.Direction.Down)
            {
                evt.StopPropagation();
                _dialogConfirmButton?.Focus();
            }
        }

        private void OnDialogConfirmNavMove(NavigationMoveEvent evt)
        {
            if (!_isQuitDialogOpen) return;
            if (evt.direction == NavigationMoveEvent.Direction.Left ||
                evt.direction == NavigationMoveEvent.Direction.Up)
            {
                evt.StopPropagation();
                _dialogCancelButton?.Focus();
            }
        }

        /// <summary>
        /// Hides the quit confirmation dialog with exit animation.
        /// Returns focus to the quit button.
        /// </summary>
        private void HideQuitDialog()
        {
            if (_quitOverlay == null || !_isQuitDialogOpen) return;

            _isQuitDialogOpen = false;

            UnregisterDialogFocusTrap();

            if (_quitDialog != null)
            {
                _quitDialog.RemoveFromClassList("dialog-enter");
                _quitDialog.AddToClassList("dialog-exit");

                float closeDuration = _isReducedMotion ? 0.1f : 0.15f;

                if (_isReducedMotion)
                {
                    HUDAnimator.TweenOpacity(_quitDialog, 1f, 0f, closeDuration, HUDAnimator.CubicEaseIn, () =>
                    {
                        _quitOverlay.style.display = DisplayStyle.None;
                    });
                }
                else
                {
                    HUDAnimator.TweenOpacity(_quitDialog, 1f, 0f, closeDuration, HUDAnimator.CubicEaseIn);
                    HUDAnimator.TweenScale(_quitDialog, 1.0f, 0.95f, closeDuration, HUDAnimator.CubicEaseIn, () =>
                    {
                        _quitOverlay.style.display = DisplayStyle.None;
                    });
                }
            }
            else
            {
                _quitOverlay.style.display = DisplayStyle.None;
            }

            // Return focus to quit button
            _quitButton?.Focus();
        }

        // ================================================================
        // Animations
        // ================================================================

        /// <summary>
        /// Plays the entrance animation sequence.
        /// After completion, starts CTA pulse and background breathing.
        /// </summary>
        private void PlayEntranceAnimation()
        {
            MainMenuEntranceAnimation.Play(_menuRoot, _isReducedMotion, () =>
            {
                // Entrance complete: start decorative animations
                StartPulse();
                StartBreathing();

                // Set default focus to CTA button
                _startBattleButton?.Focus();
            });
        }

        /// <summary>
        /// Plays the exit transition (fade out to black).
        /// Target: 0.4s (cubic ease-in). After completion, the scene
        /// load is triggered by the GameState system.
        /// </summary>
        private void PlayExitTransition()
        {
            if (_menuRoot == null) return;

            // Set CTA to loading state
            if (_ctaMainText != null)
            {
                _ctaMainText.text = "加载中...";
            }

            if (_ctaSubText != null)
            {
                _ctaSubText.style.display = DisplayStyle.None;
            }

            if (_startBattleButton != null)
            {
                _startBattleButton.AddToClassList("loading");
            }

            float fadeDuration = _tuning != null ? _tuning.ExitFadeDuration : 0.4f;
            string sceneName = _tuning != null ? _tuning.GameSceneName : "GameScene";
            float timeout = _tuning != null ? _tuning.SceneLoadTimeout : 5.0f;

            HUDAnimator.TweenOpacity(_menuRoot, 1f, 0f, fadeDuration, HUDAnimator.CubicEaseIn, () =>
            {
                StartCoroutine(LoadGameSceneAsync(sceneName, timeout));
            });
        }

        private IEnumerator LoadGameSceneAsync(string sceneName, float timeout)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            if (asyncLoad == null)
            {
                Debug.LogError($"[MainMenu] Failed to start loading scene: {sceneName}");
                _isTransitioning = false;
                yield break;
            }

            asyncLoad.allowSceneActivation = false;

            float startTime = Time.time;
            while (asyncLoad.progress < 0.9f)
            {
                if (Time.time - startTime > timeout)
                {
                    Debug.LogError("[MainMenu] Scene load timed out.");
                    // TODO: Show error UI per main-menu.md States section
                    _isTransitioning = false;
                    yield break;
                }
                yield return null;
            }

            asyncLoad.allowSceneActivation = true;
        }

        // ================================================================
        // CTA Pulse Animation
        // ================================================================

        /// <summary>
        /// Starts the CTA border opacity pulse animation.
        /// Sine wave modulation: border-opacity oscillates between 0.4 and 1.0
        /// with a 2-second period.
        /// Source: design/ux/main-menu-visual-design.md section 5.6.
        /// </summary>
        private void StartPulse()
        {
            if (_isPulsing || _startBattleButton == null || _isReducedMotion) return;

            _isPulsing = true;
            _pulsePhase = 0f;
            _startBattleButton.AddToClassList("pulsing");

            _pulseHandle = _startBattleButton.schedule.Execute(() =>
            {
                // Sine wave: opacity oscillates between 0.4 and 1.0
                // borderOpacity = 0.7 + 0.3 * sin(2*PI*t/2.0)
                _pulsePhase += Time.deltaTime * Mathf.PI; // 2*PI / 2.0 = PI per second
                float opacity = 0.7f + 0.3f * Mathf.Sin(_pulsePhase);
                _startBattleButton.style.borderBottomColor = new Color(0.94f, 0.75f, 0.25f, opacity);
                _startBattleButton.style.borderTopColor = new Color(0.94f, 0.75f, 0.25f, opacity);
                _startBattleButton.style.borderLeftColor = new Color(0.94f, 0.75f, 0.25f, opacity);
                _startBattleButton.style.borderRightColor = new Color(0.94f, 0.75f, 0.25f, opacity);
            }).Every(16);
        }

        /// <summary>
        /// Stops the CTA pulse animation.
        /// </summary>
        private void StopPulse()
        {
            if (_pulseHandle != null)
            {
                _pulseHandle.Pause();
                _pulseHandle = null;
            }

            _isPulsing = false;

            if (_startBattleButton != null)
            {
                _startBattleButton.RemoveFromClassList("pulsing");
            }
        }

        // ================================================================
        // Background Breathing Animation
        // ================================================================

        /// <summary>
        /// Starts the background breathing light animation.
        /// Sine wave opacity modulation with a 4-second period.
        /// Source: design/ux/main-menu-visual-design.md section 5.6.
        /// </summary>
        private void StartBreathing()
        {
            if (_isBreathing || _backgroundLayer == null || _isReducedMotion) return;

            _isBreathing = true;
            float phase = 0f;
            _backgroundLayer.AddToClassList("breathing");

            _breathingHandle = _backgroundLayer.schedule.Execute(() =>
            {
                // Sine wave: very subtle opacity modulation (0.95 to 1.0)
                phase += Time.deltaTime * Mathf.PI * 0.5f; // 2*PI / 4.0 = PI/2 per second
                float modulation = 0.975f + 0.025f * Mathf.Sin(phase);
                _backgroundLayer.style.opacity = modulation;
            }).Every(16);
        }

        /// <summary>
        /// Stops the background breathing animation.
        /// </summary>
        private void StopBreathing()
        {
            if (_breathingHandle != null)
            {
                _breathingHandle.Pause();
                _breathingHandle = null;
            }

            _isBreathing = false;

            if (_backgroundLayer != null)
            {
                _backgroundLayer.RemoveFromClassList("breathing");
                _backgroundLayer.style.opacity = 1f;
            }
        }

        // ================================================================
        // Helpers
        // ================================================================

        private static Scale ScaleIdentity => new Scale(Vector3.one);
    }
}
