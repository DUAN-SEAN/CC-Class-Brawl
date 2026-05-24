using UnityEngine;

namespace ClassBrawl.Foundation
{
    /// <summary>
    /// Interface for reading player input. Implemented by InputReader and consumed
    /// by 3C systems (MovementController) and combat systems (CombatFSM) so they
    /// never directly depend on PlayerInput or InputAction APIs.
    /// </summary>
    public interface IInputReader
    {
        /// <summary>
        /// Returns the current directional input vector after dead zone filtering.
        /// Values are in the range [-1, 1] on each axis, or Vector2.zero when
        /// the input magnitude is below the dead zone threshold.
        /// </summary>
        Vector2 GetMoveInput();

        /// <summary>
        /// Attempts to consume a buffered button action of the specified type.
        /// Returns true if a valid, unconsumed entry was found within the buffer window.
        /// </summary>
        /// <param name="type">The button action type to consume.</param>
        /// <param name="bufferFrames">Maximum frames since the input was recorded.</param>
        /// <returns>True if the action was successfully consumed.</returns>
        bool TryConsumeAction(InputActionType type, int bufferFrames);

        /// <summary>
        /// Returns true while the jump button is physically held down.
        /// Used by jump height systems to determine whether to apply full hop
        /// or short hop gravity.
        /// </summary>
        bool IsJumpHeld();

        /// <summary>
        /// Returns true if the jump button was released since the last check.
        /// This flag resets to false after being read (one-shot).
        /// Used to trigger short-hop behavior when the player releases jump quickly.
        /// </summary>
        bool WasJumpReleasedThisFrame();

        /// <summary>
        /// The player index assigned by PlayerInputManager during device pairing.
        /// Used to identify which player this input reader belongs to.
        /// </summary>
        int PlayerIndex { get; }
    }
}
