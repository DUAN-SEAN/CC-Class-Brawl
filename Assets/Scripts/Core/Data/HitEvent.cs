using UnityEngine;

namespace ClassBrawl.Core
{
    public struct HitEvent
    {
        public int AttackerIndex;
        public int TargetIndex;
        public Vector2 HitPoint;
        public string AttackId;
    }
}
