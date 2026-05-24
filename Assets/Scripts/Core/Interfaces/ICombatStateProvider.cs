using System;
using System.Collections.Generic;

namespace ClassBrawl.Core
{
    public interface ICombatStateProvider
    {
        CombatState GetCurrentState();
        AttackPhase GetCurrentAttackPhase();
        bool CanAcceptInput();
        void RegisterState(StateDefinition stateDefinition);
        void DeregisterAllSkillStates();

        event Action<int, CombatState, CombatState> OnCombatStateChanged;
        event Action<int, AttackPhase> OnAttackPhaseChanged;
    }
}
