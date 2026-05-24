using System;

namespace ClassBrawl.Core
{
    public interface IFocusSystem
    {
        float GetFocusPoints(int playerIndex);
        float GetUnlockThreshold(int playerIndex);
        int GetUnlockedCount(int playerIndex);
        void ResetForNewRound(int playerIndex);
        void ResetForNewMatch(int playerIndex);
        void ResetAllForNewRound();
        void ResetAllForNewMatch();

        event Action<int, int> OnFocusReady;
        event Action<int, float, float> OnFocusChanged;
    }
}
