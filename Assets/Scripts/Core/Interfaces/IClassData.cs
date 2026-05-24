using ClassBrawl.Foundation;

namespace ClassBrawl.Core
{
    public interface IClassData
    {
        MovementParams GetMovementParams();
        AttackData GetAttackData(AttackType type);
        VisualData GetVisualData();
        string[] GetSkillPoolTags();
    }
}
