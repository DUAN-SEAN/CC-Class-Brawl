using System;

namespace ClassBrawl.Core
{
    public interface IDamageSystem
    {
        float GetDamagePercent(int playerIndex);
        int GetDisplayPercent(int playerIndex);
        void ResetDamage(int playerIndex);
        void ResetAll();

        event Action<int, float> OnDamagePercentChanged;
    }
}
