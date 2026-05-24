using System;

namespace ClassBrawl.Foundation
{
    /// <summary>
    /// A single entry in the input ring buffer, recording which action was
    /// pressed and on which physics frame it was recorded.
    /// </summary>
    public struct InputEntry
    {
        /// <summary>The type of button action that was pressed.</summary>
        public InputActionType Type;

        /// <summary>The physics frame number when this input was recorded.</summary>
        public int RecordedFrame;

        /// <summary>Whether this entry has already been consumed by a gameplay system.</summary>
        public bool Consumed;
    }

    /// <summary>
    /// Ring buffer for button inputs with frame-based expiration.
    /// Capacity is defined by <see cref="GameConstants.InputBufferFrames"/> (8 entries).
    /// Inputs are written during Update callbacks and consumed during FixedUpdate.
    /// BufferAge validation ensures inputs older than the allowed window are rejected.
    /// </summary>
    public class InputBuffer
    {
        private readonly InputEntry[] _entries;
        private int _head;
        private int _count;

        /// <summary>
        /// The maximum number of entries the ring buffer can hold.
        /// Distinct from the buffer window (how many frames an entry stays valid).
        /// </summary>
        public int BufferCapacity => _entries.Length;

        /// <summary>
        /// The number of entries currently stored in the buffer (including consumed ones
        /// that have not yet been overwritten).
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Creates a new InputBuffer with capacity from GameConstants.InputBufferFrames.
        /// </summary>
        public InputBuffer()
        {
            _entries = new InputEntry[GameConstants.InputBufferFrames];
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Writes a button input action into the ring buffer at the current frame.
        /// Overwrites the oldest entry when the buffer is full (ring behavior).
        /// </summary>
        /// <param name="type">The button action type that was pressed.</param>
        /// <param name="currentFrame">The current physics frame number from FrameCounter.</param>
        public void WriteAction(InputActionType type, int currentFrame)
        {
            _entries[_head] = new InputEntry
            {
                Type = type,
                RecordedFrame = currentFrame,
                Consumed = false
            };

            _head = (_head + 1) % _entries.Length;

            if (_count < _entries.Length)
            {
                _count++;
            }
        }

        /// <summary>
        /// Attempts to find and consume an unprocessed input of the specified type
        /// that is still within the valid buffer window.
        /// <para>
        /// BufferAge = currentFrame - entry.RecordedFrame.
        /// An entry is valid when: not yet consumed, age >= 0, and age <= bufferFrames.
        /// </para>
        /// </summary>
        /// <param name="type">The action type to search for.</param>
        /// <param name="currentFrame">The current physics frame number.</param>
        /// <param name="bufferFrames">
        /// The maximum number of frames an input remains valid after being recorded.
        /// Typically GameConstants.InputBufferFrames (8).
        /// </param>
        /// <returns>True if a valid, unconsumed entry was found and marked as consumed.</returns>
        public bool TryConsumeAction(InputActionType type, int currentFrame, int bufferFrames)
        {
            int startIndex = (_head - _count + _entries.Length) % _entries.Length;

            for (int i = 0; i < _count; i++)
            {
                int index = (startIndex + i) % _entries.Length;
                ref InputEntry entry = ref _entries[index];

                if (entry.Consumed)
                {
                    continue;
                }

                if (entry.Type != type)
                {
                    continue;
                }

                int bufferAge = currentFrame - entry.RecordedFrame;

                if (bufferAge < 0 || bufferAge > bufferFrames)
                {
                    continue;
                }

                entry.Consumed = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all entries from the buffer, resetting head and count to zero.
        /// </summary>
        public void Clear()
        {
            _head = 0;
            _count = 0;

            // Clear the array to release references and reset structs
            Array.Clear(_entries, 0, _entries.Length);
        }
    }
}
