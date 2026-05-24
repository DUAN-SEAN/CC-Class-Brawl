using System;

namespace ClassBrawl.Feature
{
    public interface IMatchManager
    {
        void Initialize(MatchConfig config);
        void Reset();
        MatchState GetMatchState();
        int[] GetScores();
        int GetCurrentRound();

        event Action<int, int[]> OnRoundEnd;
        event Action<int?> OnMatchEnd;
    }
}
