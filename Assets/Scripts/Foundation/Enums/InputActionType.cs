namespace ClassBrawl.Foundation
{
    /// <summary>
    /// Represents the types of button inputs that can be buffered
    /// and consumed by gameplay systems.
    /// </summary>
    public enum InputActionType
    {
        /// <summary>Jump button input.</summary>
        Jump,

        /// <summary>Basic attack button input.</summary>
        Attack,

        /// <summary>Dash button input.</summary>
        Dash,

        /// <summary>Skill slot 1 button input.</summary>
        Skill1,

        /// <summary>Skill slot 2 button input.</summary>
        Skill2,

        /// <summary>Skill slot 3 button input.</summary>
        Skill3,

        /// <summary>Skill slot 4 button input.</summary>
        Skill4,
    }
}
