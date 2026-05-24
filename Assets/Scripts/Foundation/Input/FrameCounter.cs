using UnityEngine;

namespace ClassBrawl.Foundation
{
    /// <summary>
    /// Scene-level component that provides a monotonically increasing
    /// frame counter incremented during FixedUpdate. Used by InputBuffer to
    /// compute BufferAge for input validation. Expected to be a single instance
    /// per scene.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class FrameCounter : MonoBehaviour
    {
        /// <summary>
        /// The current physics frame number. Starts at 0 and increments
        /// once per FixedUpdate call.
        /// </summary>
        public int CurrentFrame { get; private set; }

        /// <summary>
        /// Advances the frame counter by one. Called automatically by FixedUpdate.
        /// Exposed publicly for testing purposes.
        /// </summary>
        public void AdvanceFrame()
        {
            CurrentFrame++;
        }

        private void FixedUpdate()
        {
            AdvanceFrame();
        }

        /// <summary>
        /// Resets the frame counter to zero. Call this at the start of a new
        /// round or match to re-synchronize the frame basis.
        /// </summary>
        public void Reset()
        {
            CurrentFrame = 0;
        }
    }
}
