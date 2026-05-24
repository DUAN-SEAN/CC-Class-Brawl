using UnityEngine;

namespace ClassBrawl.Foundation
{
    public interface IMovementController
    {
        MovementState GetState();
        Vector2 GetPosition();
        FacingDirection GetFacing();
        bool IsGrounded();
        void FreezeMovement(bool frozen);
        void SetVelocity(Vector2 velocity);
        void ModifySpeed(float multiplier);

        event System.Action OnJump;
        event System.Action OnLand;
        event System.Action OnDashStart;
    }
}
