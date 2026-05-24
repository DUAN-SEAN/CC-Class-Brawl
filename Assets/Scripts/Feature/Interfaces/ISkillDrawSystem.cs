using System;
using System.Collections.Generic;
using ClassBrawl.Core;

namespace ClassBrawl.Feature
{
    public interface ISkillDrawSystem
    {
        DrawPhase GetDrawPhase(int playerIndex);
        IReadOnlyList<SkillData> GetCurrentCandidates(int playerIndex);
        void SelectCandidate(int playerIndex, int candidateIndex);
        void ResetForNewRound(int playerIndex);
        void ResetForNewMatch(int playerIndex);
        void ResetAll();

        event Action<int, IReadOnlyList<SkillData>> OnDrawReady;
        event Action<int, SkillData> OnSkillDrawn;
    }
}
