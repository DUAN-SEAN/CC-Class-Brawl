using System.Collections.Generic;

namespace ClassBrawl.Core
{
    public interface ISkillDatabase
    {
        IReadOnlyList<SkillData> GetAllSkills();
        SkillData GetSkillById(string skillId);
        IReadOnlyList<SkillData> GetSkillsByRarity(Rarity rarity);
        IReadOnlyList<SkillData> GetSkillsByTag(string tag);
    }
}
