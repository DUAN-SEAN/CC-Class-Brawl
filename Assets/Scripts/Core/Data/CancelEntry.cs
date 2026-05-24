namespace ClassBrawl.Core
{
    [System.Serializable]
    public struct CancelEntry
    {
        public string TargetState;
        public string InputRequired;
        public AttackPhase RequiredPhase;
    }
}
