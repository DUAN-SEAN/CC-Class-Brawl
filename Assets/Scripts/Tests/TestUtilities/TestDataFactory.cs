using System.Collections.Generic;
using ClassBrawl.Core;

namespace ClassBrawl.Tests
{
    public static class TestDataFactory
    {
        public static AttackData CreateWarriorGroundAttack()
        {
            return new AttackData
            {
                AttackId = "warrior_ground",
                Type = AttackType.GroundAttack,
                BaseDamage = 12.0f,
                BaseKnockback = 8.0f,
                BaseKnockbackGrowth = 0.05f,
                HitStunFrames = 15,
                KnockbackLaunchRatio = 1.0f,
                KnockbackSpeedMultiplier = 2.0f,
                StartupFrames = 5,
                ActiveFrames = 3,
                RecoveryFrames = 12,
                CancelTable = new List<CancelEntry>(),
                IsProjectile = false
            };
        }

        public static AttackData CreateRogueGroundAttack()
        {
            return new AttackData
            {
                AttackId = "rogue_ground",
                Type = AttackType.GroundAttack,
                BaseDamage = 4.0f,
                BaseKnockback = 5.0f,
                BaseKnockbackGrowth = 0.05f,
                HitStunFrames = 10,
                KnockbackLaunchRatio = 0.8f,
                KnockbackSpeedMultiplier = 2.0f,
                StartupFrames = 3,
                ActiveFrames = 2,
                RecoveryFrames = 8,
                CancelTable = new List<CancelEntry>(),
                IsProjectile = false
            };
        }
    }
}
