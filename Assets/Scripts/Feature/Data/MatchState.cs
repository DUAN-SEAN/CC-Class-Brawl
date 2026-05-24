namespace ClassBrawl.Feature
{
    public struct MatchState
    {
        public MatchPhase Phase;
        public int[] Scores;
        public int CurrentRound;
        public int WinsNeeded;
        public int MaxRounds;
        public int PlayerCount;
    }
}
