using System.Collections.Generic;
using ClassBrawl.Core;

namespace ClassBrawl.Feature
{
    public struct DrawRuntimeState
    {
        public DrawPhase Phase;
        public HashSet<string> AlreadyDrawnSkillIds;
        public List<SkillData> CurrentCandidates;
        public int RemainingTimeoutFrames;
    }
}
