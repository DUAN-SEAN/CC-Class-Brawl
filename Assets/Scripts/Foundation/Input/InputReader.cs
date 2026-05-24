using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClassBrawl.Foundation
{
    /// <summary>
    /// Per-character input component that wraps PlayerInput and provides
    /// the <see cref="IInputReader"/> interface to downstream systems.
    /// Handles directional input polling, button input buffering, jump
    /// held/released state tracking, pause events, and device disconnects.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class InputReader : MonoBehaviour, IInputReader
    {
        /// <summary>
        /// The dead zone threshold for directional input. Input magnitudes
        /// below this value are treated as zero to filter stick drift.
        /// </summary>
        public const float DeadZone = 0.15f;

        /// <summary>
        /// Fired when the Pause action is triggered. Does NOT write to the
        /// InputBuffer — pause is handled via independent event, not combat buffer.
        /// Parameter is the player index who requested pause.
        /// </summary>
        public event Action<int> OnPauseRequested;

        /// <summary>
        /// Fired when the player's input device is disconnected.
        /// Parameter is the player index who lost their device.
        /// The character should hold its current state until the device reconnects.
        /// </summary>
        public event Action<int> OnDeviceLost;

        /// <inheritdoc/>
        public int PlayerIndex => _playerInput != null ? _playerInput.playerIndex : -1;

        private PlayerInput _playerInput;
        private InputBuffer _inputBuffer;
        private FrameCounter _frameCounter;

        // Cached action references
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;
        private InputAction _dashAction;
        private InputAction _skill1Action;
        private InputAction _skill2Action;
        private InputAction _skill3Action;
        private InputAction _skill4Action;
        private InputAction _pauseAction;

        // Jump held/released state
        private bool _isJumpHeld;
        private bool _shortHopFlag;

        /// <summary>
        /// Initializes the InputReader with external dependencies. Call this
        /// after the component is instantiated or after scene load, once
        /// the FrameCounter reference is available.
        /// </summary>
        /// <param name="frameCounter">The global FrameCounter instance.</param>
        public void Initialize(FrameCounter frameCounter)
        {
            _frameCounter = frameCounter;
            _inputBuffer = new InputBuffer();
        }

        private void OnEnable()
        {
            _playerInput = GetComponent<PlayerInput>();

            if (_playerInput == null)
            {
                Debug.LogError($"[InputReader] No PlayerInput component found on {gameObject.name}.");
                return;
            }

            // Ensure the Gameplay action map is active
            _playerInput.SwitchCurrentActionMap("Gameplay");

            CacheActions();
            SubscribeToActions();
            SubscribeToDeviceEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromActions();
            UnsubscribeFromDeviceEvents();
        }

        /// <inheritdoc/>
        public Vector2 GetMoveInput()
        {
            if (_moveAction == null)
            {
                return Vector2.zero;
            }

            Vector2 input = _moveAction.ReadValue<Vector2>();

            if (input.magnitude < DeadZone)
            {
                return Vector2.zero;
            }

            return input;
        }

        /// <inheritdoc/>
        public bool TryConsumeAction(InputActionType type, int bufferFrames)
        {
            if (_inputBuffer == null || _frameCounter == null)
            {
                return false;
            }

            return _inputBuffer.TryConsumeAction(type, _frameCounter.CurrentFrame, bufferFrames);
        }

        /// <inheritdoc/>
        public bool IsJumpHeld()
        {
            return _isJumpHeld;
        }

        /// <inheritdoc/>
        public bool WasJumpReleasedThisFrame()
        {
            if (_shortHopFlag)
            {
                _shortHopFlag = false;
                return true;
            }

            return false;
        }

        private void CacheActions()
        {
            var actions = _playerInput.actions;
            if (actions == null)
            {
                Debug.LogError($"[InputReader] PlayerInput has no actions asset assigned on {gameObject.name}.");
                return;
            }

            var gameplayMap = actions.FindActionMap("Gameplay");
            var uiMap = actions.FindActionMap("UI");

            if (gameplayMap != null)
            {
                _moveAction = gameplayMap.FindAction("Move");
                _jumpAction = gameplayMap.FindAction("Jump");
                _attackAction = gameplayMap.FindAction("Attack");
                _dashAction = gameplayMap.FindAction("Dash");
                _skill1Action = gameplayMap.FindAction("Skill1");
                _skill2Action = gameplayMap.FindAction("Skill2");
                _skill3Action = gameplayMap.FindAction("Skill3");
                _skill4Action = gameplayMap.FindAction("Skill4");
            }

            if (uiMap != null)
            {
                _pauseAction = uiMap.FindAction("Pause");
            }
        }

        private void SubscribeToActions()
        {
            SubscribePerformed(_jumpAction, OnJumpPerformed);
            SubscribeCanceled(_jumpAction, OnJumpCanceled);
            SubscribePerformed(_attackAction, OnAttackPerformed);
            SubscribePerformed(_dashAction, OnDashPerformed);
            SubscribePerformed(_skill1Action, OnSkill1Performed);
            SubscribePerformed(_skill2Action, OnSkill2Performed);
            SubscribePerformed(_skill3Action, OnSkill3Performed);
            SubscribePerformed(_skill4Action, OnSkill4Performed);
            SubscribePerformed(_pauseAction, OnPausePerformed);
        }

        private void UnsubscribeFromActions()
        {
            UnsubscribePerformed(_jumpAction, OnJumpPerformed);
            UnsubscribeCanceled(_jumpAction, OnJumpCanceled);
            UnsubscribePerformed(_attackAction, OnAttackPerformed);
            UnsubscribePerformed(_dashAction, OnDashPerformed);
            UnsubscribePerformed(_skill1Action, OnSkill1Performed);
            UnsubscribePerformed(_skill2Action, OnSkill2Performed);
            UnsubscribePerformed(_skill3Action, OnSkill3Performed);
            UnsubscribePerformed(_skill4Action, OnSkill4Performed);
            UnsubscribePerformed(_pauseAction, OnPausePerformed);
        }

        private void SubscribeToDeviceEvents()
        {
            if (_playerInput != null)
            {
                _playerInput.onDeviceLost += HandleDeviceLost;
            }
        }

        private void UnsubscribeFromDeviceEvents()
        {
            if (_playerInput != null)
            {
                _playerInput.onDeviceLost -= HandleDeviceLost;
            }
        }

        // --- Button action callbacks ---

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(InputActionType.Jump, frame);
            _isJumpHeld = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            _shortHopFlag = true;
            _isJumpHeld = false;
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(InputActionType.Attack, frame);
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(InputActionType.Dash, frame);
        }

        private void OnSkill1Performed(InputAction.CallbackContext context)
        {
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(InputActionType.Skill1, frame);
        }

        private void OnSkill2Performed(InputAction.CallbackContext context)
        {
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(InputActionType.Skill2, frame);
        }

        private void OnSkill3Performed(InputAction.CallbackContext context)
        {
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(InputActionType.Skill3, frame);
        }

        private void OnSkill4Performed(InputAction.CallbackContext context)
        {
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(InputActionType.Skill4, frame);
        }

        // --- Pause callback (independent, NOT written to buffer) ---

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            OnPauseRequested?.Invoke(PlayerIndex);
        }

        // --- Device disconnect ---

        private void HandleDeviceLost(PlayerInput playerInput)
        {
            OnDeviceLost?.Invoke(PlayerIndex);
        }

        // --- Helper methods for action subscription ---

        private static void SubscribePerformed(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.performed += callback;
            }
        }

        private static void UnsubscribePerformed(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.performed -= callback;
            }
        }

        private static void SubscribeCanceled(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.canceled += callback;
            }
        }

        private static void UnsubscribeCanceled(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.canceled -= callback;
            }
        }

        // --- Internal test helpers ---

        /// <summary>
        /// Simulates a button press for testing. Writes to InputBuffer and
        /// updates internal state as if the InputAction callback fired.
        /// </summary>
        internal void SimulateButtonPress(InputActionType type)
        {
            EnsureInitialized();
            int frame = _frameCounter != null ? _frameCounter.CurrentFrame : 0;
            _inputBuffer.WriteAction(type, frame);

            if (type == InputActionType.Jump)
            {
                _isJumpHeld = true;
            }
        }

        /// <summary>
        /// Simulates a jump button release for testing.
        /// </summary>
        internal void SimulateJumpRelease()
        {
            _shortHopFlag = true;
            _isJumpHeld = false;
        }

        /// <summary>
        /// Simulates a device lost event for testing.
        /// </summary>
        internal void SimulateDeviceLost()
        {
            OnDeviceLost?.Invoke(PlayerIndex);
        }

        private void EnsureInitialized()
        {
            if (_inputBuffer == null)
            {
                _inputBuffer = new InputBuffer();
            }
        }
    }
}
