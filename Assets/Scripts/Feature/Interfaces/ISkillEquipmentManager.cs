using System;
using System.Collections.Generic;
using ClassBrawl.Core;

namespace ClassBrawl.Feature
{
    public interface ISkillEquipmentManager
    {
        SkillData GetSkillInSlot(int playerIndex, int slotIndex);
        IReadOnlyList<SkillData> GetEquippedSkills(int playerIndex);
        int GetEquippedCount(int playerIndex);
        void ResetForNewMatch(int playerIndex);
        void ResetAll();

        event Action<int, int, SkillData> OnSkillEquipped;
        event Action<int, int> OnSkillUnequipped;
    }
}
