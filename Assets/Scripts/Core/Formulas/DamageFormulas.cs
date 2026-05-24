using UnityEngine;

namespace ClassBrawl.Core
{
    public static class DamageFormulas
    {
        public static float CalculateDamageGain(float baseDamage)
            => baseDamage;

        public static float CalculateKnockbackMagnitude(
            float baseKnockbackGrowth,
            float targetDamagePercent,
            float baseKnockback)
            => baseKnockbackGrowth * (targetDamagePercent / 100f) * baseKnockback
               + baseKnockback;

        public static int ToDisplayPercent(float damagePercent)
            => (int)Mathf.Floor(damagePercent);
    }
}
