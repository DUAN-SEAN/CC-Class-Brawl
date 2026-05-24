using System.Collections.Generic;
using ClassBrawl.Foundation;

namespace ClassBrawl.Core
{
    [System.Serializable]
    public class ClassData : UnityEngine.ScriptableObject
    {
        public string ClassId;
        public string DisplayName;
        public MovementParams Movement;
        public List<AttackData> BaseAttacks;
        public VisualData Visual;
        public List<string> SkillPoolTags;
    }
}
