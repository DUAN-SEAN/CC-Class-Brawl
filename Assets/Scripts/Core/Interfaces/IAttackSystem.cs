using System;

namespace ClassBrawl.Core
{
    public interface IAttackSystem
    {
        AttackData GetCurrentAttack();

        event Action<int, AttackData, int> OnAttackHit;
        event Action<int> OnHitstopStart;
        event Action OnHitstopEnd;
    }
}
