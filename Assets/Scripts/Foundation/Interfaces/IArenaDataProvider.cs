using System.Collections.Generic;
using UnityEngine;

namespace ClassBrawl.Foundation
{
    public interface IArenaDataProvider
    {
        BoundsData GetBlastZone();
        BoundsData GetCameraBounds();
        IReadOnlyList<PlatformData> GetPlatforms();
        IReadOnlyList<SpawnPointData> GetSpawnPoints();
        ArenaState GetState();
    }
}
