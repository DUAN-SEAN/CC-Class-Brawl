using UnityEngine;

namespace ClassBrawl.Foundation
{
    public static class GameConstants
    {
        public const float Gravity = 32.0f;
        public const float TerminalVelocity = 20.0f;
        public const float FixedTimestep = 1f / 60f;
        public const int InputBufferFrames = 8;
        public const float KnockbackThreshold = 9.0f;
        public const int FixedFramesPerSecond = 60;
    }
}
