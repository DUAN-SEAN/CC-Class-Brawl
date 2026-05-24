using UnityEngine;

namespace ClassBrawl.Core
{
    [System.Serializable]
    public struct MovementParams
    {
        public float MaxGroundSpeed;
        public float GroundAcceleration;
        public float GroundDeceleration;
        public float MaxAirSpeed;
        public float AirControlFactor;
        public float JumpHeight;
        public int MaxAirJumps;
        public float DashSpeed;
        public int DashFrames;
        public float DashCooldown;
        public float CoyoteFrames;
        public float JumpBufferFrames;
    }
}
