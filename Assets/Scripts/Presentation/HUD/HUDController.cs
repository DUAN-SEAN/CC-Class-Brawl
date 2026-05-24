using System;
using UnityEngine;
using UnityEngine.UIElements;
using ClassBrawl.Foundation;
using ClassBrawl.Core;
using ClassBrawl.Feature;

namespace ClassBrawl.Presentation
{
    /// <summary>
    /// Central HUD controller. Subscribes to upstream system events and routes
    /// data updates to <see cref="PlayerHUDView"/> instances and overlay elements.
    /// <para>
    /// Architecture: MonoBehaviour on GameScene, references a UIDocument with
    /// BattleHUD.uxml. All system interfaces are injected via <see cref="Initialize"/>.
    /// No singletons, no direct game state mutation. The HUD is a pure passive
    /// renderer (ADR-0014).
    /// </para>
    /// <para>
    /// Event subscriptions are set up in <see cref="Initialize"/> and torn down
    /// in <see cref="Cleanup"/>. Per-frame polling is limited to edge warnings
    /// and direction arrows (Update method).
    /// </para>
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        // ================================================================
        // Serialized References
        // ================================================================

        [Header("UI Document")]
        [Tooltip("The UIDocument rendering BattleHUD.uxml")]
        [SerializeField] private UIDocument _hudDocument;

        [Header("Tuning")]
        [Tooltip("HUD tuning parameters (ScriptableObject)")]
        [SerializeField] private HUDTuningData _tuning;

        // ================================================================
        // Private State
        // ================================================================

        // Root
        private VisualElement _hudRoot;

        // Per-player views
        private PlayerHUDView _p1View;
        private PlayerHUDView _p2View;

        // Score area elements
        private Label _scoreText;
        private Label _roundText;
        private Label _matchPointIndicator;

        // KO notification
        private Label _koNotification;

        // Edge warning overlay
        private VisualElement _edgeWarning;

        // Direction arrows
        private VisualElement _p1Arrow;
        private VisualElement _p2Arrow;

        // System references (injected)
        private IGameState _gameState;
        private IDamageSystem _damageSystem;
        private IFocusSystem _focusSystem;
        private ISkillEquipmentManager _skillEquipment;
        private IMatchManager _matchManager;
        private IKnockbackSystem _knockbackSystem;
        private IArenaDataProvider _arenaDataProvider;
        private IMovementController[] _movementControllers;
        private Color[] _playerColors;

        // State tracking
        private bool _initialized;
        private GamePhase _currentPhase;
        private int[] _scores;
        private int _winsNeeded;

        // Edge warning state
        private float _edgeWarningAlphaP1;
        private float _edgeWarningAlphaP2;

        // ================================================================
        // Unity Lifecycle
        // ================================================================

        private void Update()
        {
            if (!_initialized) return;

            // Only poll during Battle and BattleEnd phases
            if (_currentPhase != GamePhase.Battle && _currentPhase != GamePhase.BattleEnd)
                return;

            UpdateEdgeWarnings();
            UpdateDirectionArrows();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // ================================================================
        // Public API
        // ================================================================

        /// <summary>
        /// Initializes the HUD controller with all required system interfaces.
        /// Caches UI element references, subscribes to events, and sets initial state.
        /// Called by the scene setup code after all systems are ready.
        /// </summary>
        /// <param name="gameState">Game state interface for visibility control.</param>
        /// <param name="damageSystem">Damage system for percent display.</param>
        /// <param name="focusSystem">Focus system for progress bar.</param>
        /// <param name="skillEquipment">Skill equipment manager for slot display.</param>
        /// <param name="matchManager">Match manager for score and round display.</param>
        /// <param name="knockbackSystem">Knockback system for KO notification.</param>
        /// <param name="arenaDataProvider">Arena data for edge warnings.</param>
        /// <param name="movementControllers">Movement controllers per player for position polling.</param>
        /// <param name="playerColors">Per-player identity colors (length must match movementControllers).</param>
        public void Initialize(
            IGameState gameState,
            IDamageSystem damageSystem,
            IFocusSystem focusSystem,
            ISkillEquipmentManager skillEquipment,
            IMatchManager matchManager,
            IKnockbackSystem knockbackSystem,
            IArenaDataProvider arenaDataProvider,
            IMovementController[] movementControllers,
            Color[] playerColors)
        {
            _gameState = gameState;
            _damageSystem = damageSystem;
            _focusSystem = focusSystem;
            _skillEquipment = skillEquipment;
            _matchManager = matchManager;
            _knockbackSystem = knockbackSystem;
            _arenaDataProvider = arenaDataProvider;
            _movementControllers = movementControllers;
            _playerColors = playerColors;

            CacheUIReferences();
            CreatePlayerViews();
            SubscribeEvents();
            ApplyInitialMatchState();
            SetInitialState();

            _initialized = true;
        }

        /// <summary>
        /// Unsubscribes all events and stops running animations.
        /// Call before destroying or when leaving the battle scene.
        /// </summary>
        public void Cleanup()
        {
            if (!_initialized) return;

            UnsubscribeEvents();
            _initialized = false;
        }

        /// <summary>
        /// Enables or disables high-contrast mode across all player HUD views.
        /// Call when the accessibility setting changes.
        /// </summary>
        /// <param name="enabled">True to enable high-contrast mode.</param>
        public void SetHighContrast(bool enabled)
        {
            _p1View?.SetHighContrast(enabled);
            _p2View?.SetHighContrast(enabled);

            if (_hudRoot != null)
            {
                if (enabled)
                    _hudRoot.AddToClassList("high-contrast");
                else
                    _hudRoot.RemoveFromClassList("high-contrast");
            }
        }

        /// <summary>
        /// Enables or disables reduced-motion mode on the HUD root.
        /// When active, HUDAnimator replaces scale/bounce animations with opacity fades.
        /// </summary>
        /// <param name="enabled">True to enable reduced-motion mode.</param>
        public void SetReducedMotion(bool enabled)
        {
            if (_hudRoot != null)
            {
                if (enabled)
                    _hudRoot.AddToClassList("reduced-motion");
                else
                    _hudRoot.RemoveFromClassList("reduced-motion");
            }
        }

        // ================================================================
        // UI Reference Caching
        // ================================================================

        private void CacheUIReferences()
        {
            if (_hudDocument == null)
            {
                Debug.LogError("[HUDController] UIDocument reference is null.");
                return;
            }

            _hudRoot = _hudDocument.rootVisualElement.Q("BattleHUD");

            // Score area
            _scoreText = _hudRoot.Q<Label>("ScoreText");
            _roundText = _hudRoot.Q<Label>("RoundText");
            _matchPointIndicator = _hudRoot.Q<Label>("MatchPointIndicator");

            // KO notification
            _koNotification = _hudRoot.Q<Label>("KONotification");

            // Edge warning
            _edgeWarning = _hudRoot.Q("EdgeWarning");

            // Direction arrows
            _p1Arrow = _hudRoot.Q("P1Arrow");
            _p2Arrow = _hudRoot.Q("P2Arrow");
        }

        private void CreatePlayerViews()
        {
            int playerCount = _movementControllers != null ? _movementControllers.Length : 2;

            _p1View = new PlayerHUDView();
            VisualElement p1Area = _hudRoot.Q("P1InfoArea");
            _p1View.Initialize(0, p1Area, GetPlayerColor(0), _tuning);

            _p2View = new PlayerHUDView();
            VisualElement p2Area = _hudRoot.Q("P2InfoArea");
            _p2View.Initialize(1, p2Area, GetPlayerColor(1), _tuning);
        }

        private Color GetPlayerColor(int playerIndex)
        {
            if (_playerColors != null && playerIndex < _playerColors.Length)
                return _playerColors[playerIndex];
            return Color.white;
        }

        // ================================================================
        // Event Subscription
        // ================================================================

        private void SubscribeEvents()
        {
            // Game state
            if (_gameState != null)
                _gameState.OnStateChanged += OnGameStateChanged;

            // Damage system
            if (_damageSystem != null)
                _damageSystem.OnDamagePercentChanged += OnDamagePercentChanged;

            // Focus system
            if (_focusSystem != null)
            {
                _focusSystem.OnFocusChanged += OnFocusChanged;
                _focusSystem.OnFocusReady += OnFocusReady;
            }

            // Skill equipment
            if (_skillEquipment != null)
            {
                _skillEquipment.OnSkillEquipped += OnSkillEquipped;
                _skillEquipment.OnSkillUnequipped += OnSkillUnequipped;
            }

            // Match manager
            if (_matchManager != null)
            {
                _matchManager.OnRoundEnd += OnRoundEnd;
                _matchManager.OnMatchEnd += OnMatchEnd;
            }

            // Knockback system (KO)
            if (_knockbackSystem != null)
                _knockbackSystem.OnKO += OnKO;
        }

        private void UnsubscribeEvents()
        {
            if (_gameState != null)
                _gameState.OnStateChanged -= OnGameStateChanged;

            if (_damageSystem != null)
                _damageSystem.OnDamagePercentChanged -= OnDamagePercentChanged;

            if (_focusSystem != null)
            {
                _focusSystem.OnFocusChanged -= OnFocusChanged;
                _focusSystem.OnFocusReady -= OnFocusReady;
            }

            if (_skillEquipment != null)
            {
                _skillEquipment.OnSkillEquipped -= OnSkillEquipped;
                _skillEquipment.OnSkillUnequipped -= OnSkillUnequipped;
            }

            if (_matchManager != null)
            {
                _matchManager.OnRoundEnd -= OnRoundEnd;
                _matchManager.OnMatchEnd -= OnMatchEnd;
            }

            if (_knockbackSystem != null)
                _knockbackSystem.OnKO -= OnKO;
        }

        // ================================================================
        // Event Handlers
        // ================================================================

        // ---- Game State (Visibility) ----

        private void OnGameStateChanged(GamePhase newPhase)
        {
            GamePhase previousPhase = _currentPhase;
            _currentPhase = newPhase;

            switch (newPhase)
            {
                case GamePhase.Countdown:
                    if (previousPhase == GamePhase.MatchLoading)
                    {
                        SetInitialState();
                        ShowHUD();
                    }
                    else
                    {
                        // Countdown skipped or re-entered; force show
                        SetInitialState();
                        ShowHUD();
                    }
                    break;

                case GamePhase.Battle:
                    // HUD already visible from Countdown
                    break;

                case GamePhase.BattleEnd:
                    // HUD freezes in current state
                    break;

                case GamePhase.Results:
                    HideHUD();
                    break;

                default:
                    // MainMenu, CharacterSelect, MatchLoading -> hidden
                    HideHUD();
                    break;
            }
        }

        // ---- Damage System ----

        private void OnDamagePercentChanged(int playerIndex, float newPercent)
        {
            if (!_initialized) return;
            GetPlayerView(playerIndex)?.UpdateDamagePercent(newPercent, _tuning);
        }

        // ---- Focus System ----

        private void OnFocusChanged(int playerIndex, float focusPoints, float unlockThreshold)
        {
            if (!_initialized) return;

            int unlockedCount = _focusSystem != null ? _focusSystem.GetUnlockedCount(playerIndex) : 0;
            GetPlayerView(playerIndex)?.UpdateFocusBar(focusPoints, unlockThreshold, unlockedCount, _tuning);
        }

        private void OnFocusReady(int playerIndex, int unlockedCount)
        {
            if (!_initialized) return;
            GetPlayerView(playerIndex)?.PlayFocusUnlockFlash(_tuning);
        }

        // ---- Skill Equipment ----

        private void OnSkillEquipped(int playerIndex, int slotIndex, SkillData skillData)
        {
            if (!_initialized) return;
            GetPlayerView(playerIndex)?.UpdateSkillSlot(slotIndex, skillData, _tuning);
        }

        private void OnSkillUnequipped(int playerIndex, int slotIndex)
        {
            if (!_initialized) return;
            GetPlayerView(playerIndex)?.UpdateSkillSlot(slotIndex, null, _tuning);
        }

        // ---- Match Manager ----

        private void OnRoundEnd(int winnerIndex, int[] scores)
        {
            if (!_initialized) return;
            UpdateScoreDisplay(scores);
        }

        private void OnMatchEnd(int? winnerIndex)
        {
            if (!_initialized) return;
            // Final score update; match is over
            if (_matchManager != null)
            {
                UpdateScoreDisplay(_matchManager.GetScores());
            }
        }

        // ---- Knockback System (KO) ----

        private void OnKO(int winnerIndex, Vector2 knockbackVelocity)
        {
            if (!_initialized) return;

            if (_koNotification != null)
            {
                HUDAnimator.PlayKOSequence(_koNotification);
            }
        }

        // ================================================================
        // Score Display
        // ================================================================

        private void UpdateScoreDisplay(int[] scores)
        {
            if (scores == null || scores.Length < 2) return;
            _scores = scores;

            if (_scoreText != null)
            {
                _scoreText.text = $"{scores[0]} - {scores[1]}";
            }

            if (_roundText != null && _matchManager != null)
            {
                MatchState matchState = _matchManager.GetMatchState();
                int currentRound = matchState.CurrentRound;
                int maxRounds = matchState.MaxRounds;
                _roundText.text = $"R{currentRound}/{maxRounds}";
                _winsNeeded = matchState.WinsNeeded;
            }

            // Match point check (Formula 5)
            UpdateMatchPointIndicator(scores);
        }

        private void UpdateMatchPointIndicator(int[] scores)
        {
            if (_matchPointIndicator == null || scores == null) return;

            bool isMatchPoint = (scores[0] == _winsNeeded - 1) || (scores[1] == _winsNeeded - 1);

            if (isMatchPoint)
            {
                _matchPointIndicator.AddToClassList("visible");
            }
            else
            {
                _matchPointIndicator.RemoveFromClassList("visible");
            }
        }

        // ================================================================
        // HUD Visibility
        // ================================================================

        private void ShowHUD()
        {
            if (_hudRoot == null) return;

            _hudRoot.style.display = DisplayStyle.Flex;
            _hudRoot.RemoveFromClassList("fade-out");
            _hudRoot.AddToClassList("fade-in");

            // Animate opacity from 0 to 1
            HUDAnimator.TweenOpacity(_hudRoot, 0f, 1f, _tuning.HudFadeInDuration, HUDAnimator.CubicEaseOut);
        }

        private void HideHUD()
        {
            if (_hudRoot == null) return;

            _hudRoot.RemoveFromClassList("fade-in");
            _hudRoot.AddToClassList("fade-out");

            HUDAnimator.TweenOpacity(_hudRoot, 1f, 0f, _tuning.HudFadeOutDuration, HUDAnimator.CubicEaseIn,
                () =>
                {
                    _hudRoot.style.display = DisplayStyle.None;
                });
        }

        // ================================================================
        // Per-Frame Polling: Edge Warnings & Direction Arrows
        // ================================================================

        private void UpdateEdgeWarnings()
        {
            if (_arenaDataProvider == null || _movementControllers == null) return;

            BoundsData blastZone = _arenaDataProvider.GetBlastZone();

            for (int i = 0; i < _movementControllers.Length && i < 2; i++)
            {
                Vector2 pos = _movementControllers[i].GetPosition();

                // Calculate minimum distance to any blast zone edge
                float distLeft = Mathf.Abs(pos.x - blastZone.Left);
                float distRight = Mathf.Abs(pos.x - blastZone.Right);
                float distTop = Mathf.Abs(pos.y - blastZone.Top);
                float distBottom = Mathf.Abs(pos.y - blastZone.Bottom);

                float minDist = Mathf.Min(distLeft, distRight, distTop, distBottom);

                // Warning threshold: start showing at ~3 units from blast zone
                const float warningThreshold = 3.0f;
                float alpha = 0f;

                if (minDist < warningThreshold)
                {
                    // Linear interpolation: 0 at threshold, 0.4 at blast zone edge
                    alpha = Mathf.Lerp(0.4f, 0f, minDist / warningThreshold);
                }

                if (i == 0) _edgeWarningAlphaP1 = alpha;
                else _edgeWarningAlphaP2 = alpha;
            }

            // Take maximum alpha from both players
            float maxAlpha = Mathf.Max(_edgeWarningAlphaP1, _edgeWarningAlphaP2);

            if (_edgeWarning != null)
            {
                if (maxAlpha > 0.01f)
                {
                    _edgeWarning.style.display = DisplayStyle.Flex;
                    _edgeWarning.style.opacity = maxAlpha;
                }
                else
                {
                    _edgeWarning.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateDirectionArrows()
        {
            if (_arenaDataProvider == null || _movementControllers == null) return;

            BoundsData cameraBounds = _arenaDataProvider.GetCameraBounds();

            VisualElement[] arrows = { _p1Arrow, _p2Arrow };

            for (int i = 0; i < _movementControllers.Length && i < 2; i++)
            {
                if (arrows[i] == null) continue;

                Vector2 pos = _movementControllers[i].GetPosition();

                bool outOfBounds = pos.x < cameraBounds.Left || pos.x > cameraBounds.Right ||
                                   pos.y < cameraBounds.Bottom || pos.y > cameraBounds.Top;

                if (outOfBounds)
                {
                    arrows[i].style.display = DisplayStyle.Flex;

                    // Position arrow at screen edge pointing toward player
                    float screenLeft = cameraBounds.Left;
                    float screenRight = cameraBounds.Right;
                    float screenTop = cameraBounds.Top;
                    float screenBottom = cameraBounds.Bottom;

                    // Determine which edge to place arrow on
                    if (pos.x < cameraBounds.Left)
                    {
                        arrows[i].style.left = 8f;
                        arrows[i].style.top = Mathf.Clamp(
                            (pos.y - screenBottom) / (screenTop - screenBottom) * Screen.height,
                            0, Screen.height - 24);
                    }
                    else if (pos.x > cameraBounds.Right)
                    {
                        arrows[i].style.left = Screen.width - 32f;
                        arrows[i].style.top = Mathf.Clamp(
                            (pos.y - screenBottom) / (screenTop - screenBottom) * Screen.height,
                            0, Screen.height - 24);
                    }
                    else if (pos.y > cameraBounds.Top)
                    {
                        arrows[i].style.left = Mathf.Clamp(
                            (pos.x - screenLeft) / (screenRight - screenLeft) * Screen.width,
                            0, Screen.width - 24);
                        arrows[i].style.top = 8f;
                    }
                    else if (pos.y < cameraBounds.Bottom)
                    {
                        arrows[i].style.left = Mathf.Clamp(
                            (pos.x - screenLeft) / (screenRight - screenLeft) * Screen.width,
                            0, Screen.width - 24);
                        arrows[i].style.top = Screen.height - 32f;
                    }

                    // Apply player color to arrow
                    if (_playerColors != null && i < _playerColors.Length)
                    {
                        arrows[i].style.backgroundColor = new StyleColor(_playerColors[i]);
                    }

                    arrows[i].AddToClassList("visible");
                }
                else
                {
                    arrows[i].RemoveFromClassList("visible");
                    arrows[i].style.display = DisplayStyle.None;
                }
            }
        }

        // ================================================================
        // Initial State
        // ================================================================

        private void ApplyInitialMatchState()
        {
            if (_matchManager != null)
            {
                MatchState state = _matchManager.GetMatchState();
                _scores = state.Scores;
                _winsNeeded = state.WinsNeeded;
                UpdateScoreDisplay(_scores);
            }
        }

        private void SetInitialState()
        {
            // Reset player views to initial state
            _p1View?.ResetToInitialState(_tuning);
            _p2View?.ResetToInitialState(_tuning);

            // Reset score
            if (_scoreText != null)
                _scoreText.text = "0 - 0";
            if (_roundText != null)
                _roundText.text = "R1/3";
            if (_matchPointIndicator != null)
                _matchPointIndicator.RemoveFromClassList("visible");

            // Hide overlays
            if (_koNotification != null)
                _koNotification.style.display = DisplayStyle.None;
            if (_edgeWarning != null)
                _edgeWarning.style.display = DisplayStyle.None;
            if (_p1Arrow != null)
                _p1Arrow.style.display = DisplayStyle.None;
            if (_p2Arrow != null)
                _p2Arrow.style.display = DisplayStyle.None;

            // Populate from current upstream state if available
            PopulateFromCurrentState();
        }

        /// <summary>
        /// Reads current state from all upstream systems and updates the HUD.
        /// Used when Countdown is skipped or when initializing mid-match.
        /// </summary>
        private void PopulateFromCurrentState()
        {
            // Damage percents
            if (_damageSystem != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    float percent = _damageSystem.GetDamagePercent(i);
                    GetPlayerView(i)?.UpdateDamagePercent(percent, _tuning);
                }
            }

            // Focus bars
            if (_focusSystem != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    float points = _focusSystem.GetFocusPoints(i);
                    float threshold = _focusSystem.GetUnlockThreshold(i);
                    int unlocked = _focusSystem.GetUnlockedCount(i);
                    GetPlayerView(i)?.UpdateFocusBar(points, threshold, unlocked, _tuning);
                }
            }

            // Skill slots
            if (_skillEquipment != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    var equipped = _skillEquipment.GetEquippedSkills(i);
                    if (equipped != null)
                    {
                        for (int s = 0; s < equipped.Count && s < _tuning.MaxSkillsPerMatch; s++)
                        {
                            if (equipped[s] != null)
                            {
                                GetPlayerView(i)?.UpdateSkillSlot(s, equipped[s], _tuning);
                            }
                        }
                    }
                }
            }

            // Scores
            if (_matchManager != null)
            {
                int[] scores = _matchManager.GetScores();
                if (scores != null)
                    UpdateScoreDisplay(scores);
            }
        }

        // ================================================================
        // Helpers
        // ================================================================

        private PlayerHUDView GetPlayerView(int playerIndex)
        {
            return playerIndex switch
            {
                0 => _p1View,
                1 => _p2View,
                _ => null
            };
        }
    }
}
