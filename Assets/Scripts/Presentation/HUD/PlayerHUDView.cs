using System;
using UnityEngine;
using UnityEngine.UIElements;
using ClassBrawl.Core;

namespace ClassBrawl.Presentation
{
    /// <summary>
    /// Per-player HUD view. Caches all <c>Q&lt;T&gt;()</c> references for a single
    /// player's HUD elements and exposes update methods called by
    /// <see cref="HUDController"/>. Tracks pulse state for the focus bar
    /// to avoid redundant schedule allocation.
    /// <para>
    /// Each instance handles one player (P1 or P2). The controller creates
    /// two instances and routes events to the correct one by player index.
    /// </para>
    /// </summary>
    public class PlayerHUDView
    {
        // ---- Damage Percent ----
        private Label _damagePercentLabel;

        // ---- Focus Bar ----
        private VisualElement _focusFill;
        private VisualElement _thresholdMarker;
        private Label _maxLabel;

        // ---- Skill Slots ----
        private VisualElement[] _skillSlots;
        private VisualElement[] _skillIcons;
        private Label[] _keyHints;

        // ---- State Tracking ----
        private bool _isPulsing;
        private IVisualElementScheduledItem _pulseHandle;
        private float _currentPulseFrequency;
        private float _pulsePhase;
        private Color _playerColor;
        private int _maxSkills;
        private int _currentDamageClassIndex = -1;

        // ---- Accessibility State ----
        private bool _highContrast;

        // ---- Stale Data Tracking ----
        private float _lastDamageEventTime;
        private float _lastFocusEventTime;
        private bool _damageStaleMarkShown;
        private bool _focusStaleMarkShown;
        private float _staleTimeout;

        /// <summary>
        /// Player index (0 = P1, 1 = P2). Set during initialization.
        /// </summary>
        public int PlayerIndex { get; private set; }

        /// <summary>
        /// Initializes the view by caching all UI element references
        /// from the parent container. Called once during HUD setup.
        /// </summary>
        /// <param name="playerIndex">0 for P1, 1 for P2.</param>
        /// <param name="container">The player info area VisualElement.</param>
        /// <param name="playerColor">Identity color for this player's class.</param>
        /// <param name="tuning">Tuning data providing max slots and stale timeout.</param>
        public void Initialize(int playerIndex, VisualElement container, Color playerColor, HUDTuningData tuning)
        {
            PlayerIndex = playerIndex;
            _playerColor = playerColor;
            _maxSkills = tuning.MaxSkillsPerMatch;
            _staleTimeout = tuning.DataStaleTimeout;

            string prefix = $"P{playerIndex + 1}";

            // Damage percent label
            _damagePercentLabel = container.Q<Label>($"{prefix}DamagePercent");

            // Focus bar elements
            _focusFill = container.Q($"{prefix}FocusFill");
            _thresholdMarker = container.Q($"{prefix}ThresholdMarker");
            _maxLabel = container.Q<Label>($"{prefix}MaxLabel");

            // Skill slots
            _skillSlots = new VisualElement[_maxSkills];
            _skillIcons = new VisualElement[_maxSkills];
            _keyHints = new Label[_maxSkills];

            VisualElement slotsContainer = container.Q($"{prefix}SkillSlots");
            for (int i = 0; i < _maxSkills; i++)
            {
                _skillSlots[i] = slotsContainer.Q($"{prefix}Slot{i}");
                _skillIcons[i] = slotsContainer.Q($"{prefix}SlotIcon{i}");
                _keyHints[i] = slotsContainer.Q<Label>($"{prefix}SlotKey{i}");
            }

            // Apply player color to focus fill
            ApplyPlayerColor();
        }

        /// <summary>
        /// Updates the damage percent display text, color tier, and bounce animation.
        /// <para>Source: design/gdd/battle-hud.md Formula 1 (display value) and Formula 2 (color index).</para>
        /// </summary>
        /// <param name="newPercent">Raw damage percent from the damage system.</param>
        /// <param name="tuning">Tuning data for animation parameters.</param>
        public void UpdateDamagePercent(float newPercent, HUDTuningData tuning)
        {
            if (_damagePercentLabel == null) return;

            // Clamp to 0 minimum
            float clampedPercent = Mathf.Max(0f, newPercent);
            int displayValue = Mathf.FloorToInt(clampedPercent);

            // Display text
            _damagePercentLabel.text = displayValue > 999 ? "999+" : $"{displayValue}%";

            // Color tier (Formula 2 from GDD)
            int tierIndex;
            string tierClass;
            if (displayValue >= 150)
            {
                tierIndex = 3;
                tierClass = "damage-critical";
            }
            else if (displayValue >= 100)
            {
                tierIndex = 2;
                tierClass = "damage-high";
            }
            else if (displayValue >= 50)
            {
                tierIndex = 1;
                tierClass = "damage-mid";
            }
            else
            {
                tierIndex = 0;
                tierClass = "damage-low";
            }

            // Update color class
            _damagePercentLabel.RemoveFromClassList("damage-low");
            _damagePercentLabel.RemoveFromClassList("damage-mid");
            _damagePercentLabel.RemoveFromClassList("damage-high");
            _damagePercentLabel.RemoveFromClassList("damage-critical");
            _damagePercentLabel.AddToClassList(tierClass);

            // Bounce animation
            _damagePercentLabel.RemoveFromClassList("bounce");
            _damagePercentLabel.schedule.Execute(() =>
            {
                _damagePercentLabel.AddToClassList("bounce");
                // Remove bounce class after animation completes
                _damagePercentLabel.schedule.Execute(() =>
                {
                    _damagePercentLabel.RemoveFromClassList("bounce");
                }).StartingIn((long)(tuning.DamageNumberBounceDuration * 1000));
            }).StartingIn(16);

            _currentDamageClassIndex = tierIndex;

            // Reset stale tracking
            _lastDamageEventTime = Time.time;
            _damageStaleMarkShown = false;
        }

        /// <summary>
        /// Updates the focus bar fill ratio and threshold marker position.
        /// Manages pulse state transitions based on fill ratio.
        /// <para>Source: design/gdd/battle-hud.md Formula 3 (fill ratio) and Formula 4 (pulse frequency).</para>
        /// </summary>
        /// <param name="focusPoints">Current focus points.</param>
        /// <param name="unlockThreshold">Current unlock threshold.</param>
        /// <param name="unlockedCount">Number of skills already unlocked.</param>
        /// <param name="tuning">Tuning data for pulse parameters.</param>
        public void UpdateFocusBar(float focusPoints, float unlockThreshold, int unlockedCount, HUDTuningData tuning)
        {
            if (_focusFill == null) return;

            // Formula 3: fill ratio
            float fillRatio;
            if (unlockThreshold <= 0f)
            {
                fillRatio = 1.0f;
            }
            else
            {
                fillRatio = Mathf.Clamp01(focusPoints / unlockThreshold);
            }

            _focusFill.style.width = new Length(fillRatio * 100f, LengthUnit.Percent);

            // Threshold marker: always at 100% because fillRatio = FocusPoints / UnlockThreshold.
            // The bar's full width represents 0-to-threshold, so the marker sits at the
            // right edge, indicating "fill to here to unlock." When the threshold increases,
            // the same FocusPoints yields a smaller fillRatio, effectively moving the goal.
            if (_thresholdMarker != null && unlockThreshold > 0f)
            {
                _thresholdMarker.style.left = new Length(100f, LengthUnit.Percent);
            }

            // Formula 4: pulse frequency and maxed state
            bool isMaxed = unlockedCount >= _maxSkills;

            if (isMaxed)
            {
                StopPulse();
                _focusFill.AddToClassList("maxed");
                _focusFill.RemoveFromClassList("pulse");
                _focusFill.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));

                if (_maxLabel != null)
                    _maxLabel.style.display = DisplayStyle.Flex;
            }
            else if (fillRatio > tuning.FocusPulseThreshold)
            {
                float t = (fillRatio - tuning.FocusPulseThreshold) / (1.0f - tuning.FocusPulseThreshold);
                float frequency = Mathf.Lerp(tuning.FocusPulseMinFrequency, tuning.FocusPulseMaxFrequency, t);
                StartPulse(frequency);
                _focusFill.RemoveFromClassList("maxed");

                if (_maxLabel != null)
                    _maxLabel.style.display = DisplayStyle.None;
            }
            else
            {
                StopPulse();
                _focusFill.RemoveFromClassList("maxed");
                _focusFill.RemoveFromClassList("pulse");
                ApplyPlayerColor();

                if (_maxLabel != null)
                    _maxLabel.style.display = DisplayStyle.None;
            }

            // Reset stale tracking
            _lastFocusEventTime = Time.time;
            _focusStaleMarkShown = false;
        }

        /// <summary>
        /// Plays the focus unlock flash animation: brief white flash then clear.
        /// <para>Source: design/ux/hud.md section on focus bar unlock animation.</para>
        /// </summary>
        /// <param name="tuning">Tuning data (unused but kept for API consistency).</param>
        public void PlayFocusUnlockFlash(HUDTuningData tuning)
        {
            if (_focusFill == null) return;

            // Flash white for 0.1s
            _focusFill.style.backgroundColor = Color.white;
            _focusFill.schedule.Execute(() =>
            {
                ApplyPlayerColor();
            }).StartingIn(100);
        }

        /// <summary>
        /// Updates a skill slot to show the equipped skill icon and rarity border.
        /// Plays the two-phase equip animation.
        /// <para>Source: design/ux/hud-visual-design.md section 3.3.</para>
        /// </summary>
        /// <param name="slotIndex">Slot index (0-based).</param>
        /// <param name="skillData">The equipped skill data (null to clear).</param>
        /// <param name="tuning">Tuning data for animation duration.</param>
        public void UpdateSkillSlot(int slotIndex, SkillData skillData, HUDTuningData tuning)
        {
            if (slotIndex < 0 || slotIndex >= _maxSkills) return;

            VisualElement slot = _skillSlots[slotIndex];
            VisualElement icon = _skillIcons[slotIndex];
            if (slot == null || icon == null) return;

            if (skillData != null)
            {
                // Set icon
                if (skillData.Icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(skillData.Icon);
                }

                // Show icon, hide key hint
                icon.style.display = DisplayStyle.Flex;
                if (_keyHints[slotIndex] != null)
                    _keyHints[slotIndex].style.display = DisplayStyle.None;

                // Remove empty, add rarity class
                slot.RemoveFromClassList("empty");
                string rarityClass = $"rarity-{skillData.Rarity.ToString().ToLower()}";
                slot.AddToClassList(rarityClass);
                slot.AddToClassList("equipped");

                // Play equip animation
                HUDAnimator.PlayEquipAnimation(slot, tuning.SkillEquipAnimDuration);
            }
            else
            {
                // Clear slot
                icon.style.backgroundImage = StyleKeyword.None;
                icon.style.display = DisplayStyle.None;
                if (_keyHints[slotIndex] != null)
                    _keyHints[slotIndex].style.display = DisplayStyle.Flex;

                slot.RemoveFromClassList("equipped");
                slot.RemoveFromClassList("rarity-common");
                slot.RemoveFromClassList("Rarity-rare");
                slot.RemoveFromClassList("rarity-epic");
                slot.AddToClassList("empty");
            }
        }

        /// <summary>
        /// Plays the interrupt flash on a skill slot (brief red border flash).
        /// </summary>
        /// <param name="slotIndex">Slot index (0-based).</param>
        public void PlaySlotInterrupt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSkills) return;
            VisualElement slot = _skillSlots[slotIndex];
            if (slot == null) return;

            slot.AddToClassList("flash-interrupt");
            slot.schedule.Execute(() =>
            {
                slot.RemoveFromClassList("flash-interrupt");
            }).StartingIn(200);
        }

        /// <summary>
        /// Resets all elements to their initial state (0%, empty slots, no pulse).
        /// Called when entering Countdown phase.
        /// </summary>
        /// <param name="tuning">Tuning data for initial values.</param>
        public void ResetToInitialState(HUDTuningData tuning)
        {
            // Damage
            if (_damagePercentLabel != null)
            {
                _damagePercentLabel.text = "0%";
                _damagePercentLabel.RemoveFromClassList("damage-mid");
                _damagePercentLabel.RemoveFromClassList("damage-high");
                _damagePercentLabel.RemoveFromClassList("damage-critical");
                _damagePercentLabel.AddToClassList("damage-low");
            }

            // Focus bar
            if (_focusFill != null)
            {
                _focusFill.style.width = new Length(0f, LengthUnit.Percent);
                _focusFill.RemoveFromClassList("maxed");
                _focusFill.RemoveFromClassList("pulse");
                ApplyPlayerColor();
            }

            if (_maxLabel != null)
                _maxLabel.style.display = DisplayStyle.None;

            StopPulse();

            // Skill slots
            for (int i = 0; i < _maxSkills; i++)
            {
                UpdateSkillSlot(i, null, tuning);
            }
        }

        // ================================================================
        // Pulse Management
        // ================================================================

        private void StartPulse(float frequency)
        {
            if (_isPulsing && Mathf.Approximately(_currentPulseFrequency, frequency))
                return;

            StopPulse();

            _isPulsing = true;
            _currentPulseFrequency = frequency;
            _pulsePhase = 0f;

            float periodMs = 1000f / frequency;

            _pulseHandle = _focusFill.schedule.Execute(() =>
            {
                _pulsePhase += Time.deltaTime * _currentPulseFrequency * Mathf.PI * 2f;
                // Sine wave: opacity modulation +-15% around 1.0
                float modulation = 0.85f + 0.15f * Mathf.Sin(_pulsePhase);
                _focusFill.style.opacity = modulation;
            }).Every(16);

            _focusFill.AddToClassList("pulse");
        }

        private void StopPulse()
        {
            if (_pulseHandle != null)
            {
                _pulseHandle.Pause();
                _pulseHandle = null;
            }

            _isPulsing = false;

            if (_focusFill != null)
            {
                _focusFill.style.opacity = 1f;
            }
        }

        // ================================================================
        // Helpers
        // ================================================================

        private void ApplyPlayerColor()
        {
            if (_focusFill == null) return;

            Color color = _playerColor;

            // High-contrast boost: if the player color is dark enough to have
            // poor contrast against the focus bar track (#333333), lighten it.
            // Warrior Red (#E84545) specifically maps to #F06060.
            if (_highContrast)
            {
                color = GetHighContrastColor(color);
            }

            _focusFill.style.backgroundColor = new StyleColor(color);
        }

        /// <summary>
        /// Enables or disables high-contrast mode for this player's HUD.
        /// When enabled, focus bar fill colors are boosted for accessibility.
        /// </summary>
        /// <param name="enabled">True to enable high-contrast mode.</param>
        public void SetHighContrast(bool enabled)
        {
            _highContrast = enabled;
            ApplyPlayerColor();
        }

        /// <summary>
        /// Returns a high-contrast version of the given player color.
        /// Warrior Red (#E84545) is boosted to #F06060. Other dark colors
        /// are lightened proportionally if their perceived brightness is below 0.4.
        /// </summary>
        private static Color GetHighContrastColor(Color color)
        {
            // Perceived brightness using ITU-R BT.601 luma coefficients
            float brightness = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;

            if (brightness < 0.4f)
            {
                // Lighten toward white to boost contrast against #333333 track
                return Color.Lerp(color, Color.white, 0.35f);
            }

            // Warrior Red check: #E84545 has brightness ~0.39, borderline.
            // Check for red-dominant colors (hue in red range, high saturation).
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float saturation = max > 0f ? (max - min) / max : 0f;

            if (color.r > 0.85f && saturation > 0.6f && color.g < 0.4f && color.b < 0.4f)
            {
                // Red-dominant class color (Warrior): boost to #F06060
                return new Color(0.941f, 0.376f, 0.376f);
            }

            return color;
        }
    }
}
