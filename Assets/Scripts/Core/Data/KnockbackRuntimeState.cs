using UnityEngine;

namespace ClassBrawl.Core
{
    public struct KnockbackRuntimeState
    {
        public bool IsActive;
        public bool IsKO;
        public bool IsInHitstun;
        public Vector2 CurrentVelocity;
        public float RemainingHitstunFrames;
    }
}
