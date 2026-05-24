using System;
using UnityEngine;

namespace ClassBrawl.Core
{
    public interface IKnockbackSystem
    {
        KnockbackState GetKnockbackState(int playerIndex);
        Vector2 GetKnockbackVelocity(int playerIndex);
        void ResetKnockback(int playerIndex);
        void ResetAll();

        event Action<int, Vector2> OnKO;
    }
}
