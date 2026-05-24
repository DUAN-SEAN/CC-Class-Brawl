using System.Collections.Generic;
using UnityEngine;

namespace ClassBrawl.Core
{
    [System.Serializable]
    public struct AttackData
    {
        public string AttackId;
        public AttackType Type;
        public float BaseDamage;
        public float BaseKnockback;
        public float BaseKnockbackGrowth;
        public int HitStunFrames;
        public float KnockbackLaunchRatio;
        public float KnockbackSpeedMultiplier;
        public int StartupFrames;
        public int ActiveFrames;
        public int RecoveryFrames;
        public List<CancelEntry> CancelTable;
        public bool IsProjectile;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public Vector2 HitboxOffset;
        public Vector2 HitboxSize;
    }
}
