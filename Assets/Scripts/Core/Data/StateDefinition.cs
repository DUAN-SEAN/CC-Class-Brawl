using System.Collections.Generic;

namespace ClassBrawl.Core
{
    [System.Serializable]
    public struct StateDefinition
    {
        public string StateName;
        public int StartupFrames;
        public int ActiveFrames;
        public int RecoveryFrames;
        public List<CancelEntry> CancelTable;
    }
}
