using UnityEngine;
using ClassBrawl.Foundation;

namespace ClassBrawl.Core
{
    public static class KnockbackFormulas
    {
        public static Vector2 CalculateKnockbackVector(
            Vector2 attackerPos, Vector2 targetPos,
            FacingDirection attackerFacing,
            float knockbackMagnitude,
            float knockbackSpeedMultiplier,
            float knockbackLaunchRatio)
        {
            float horizontalDir = Mathf.Sign(targetPos.x - attackerPos.x);
            if (horizontalDir == 0f)
                horizontalDir = (int)attackerFacing;

            var dir = new Vector2(horizontalDir, knockbackLaunchRatio).normalized;
            return dir * knockbackMagnitude * knockbackSpeedMultiplier;
        }
    }
}
