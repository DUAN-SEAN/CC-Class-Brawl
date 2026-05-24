using UnityEngine;

namespace ClassBrawl.Core
{
    public static class FocusFormulas
    {
        public static float CalculateFocusGain(float baseDamage, float gainRate)
            => baseDamage * gainRate;

        public static float CalculateUnlockThreshold(
            int unlockedCount, float baseThreshold, float thresholdGrowth)
            => baseThreshold + unlockedCount * thresholdGrowth;

        public static float ClampFocus(float focusPoints, float focusCap)
            => Mathf.Min(focusPoints, focusCap);
    }
}
