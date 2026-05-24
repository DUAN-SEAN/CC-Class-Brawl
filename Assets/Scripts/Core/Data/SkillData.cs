using System.Collections.Generic;
using UnityEngine;

namespace ClassBrawl.Core
{
    [System.Serializable]
    public class SkillData : UnityEngine.ScriptableObject
    {
        public string SkillId;
        public string DisplayName;
        public string Description;
        public Rarity Rarity;
        public float SkillDrawWeight = 1.0f;
        public List<string> Tags;
        public AttackData AttackData;
        public Sprite Icon;
    }
}
