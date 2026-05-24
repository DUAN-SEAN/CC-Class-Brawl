using System;
using System.Collections.Generic;

namespace ClassBrawl.Foundation
{
    public interface IGameState
    {
        GamePhase GetState();
        bool IsBattleActive();

        void SetPlayerCharacter(int playerSlot, string characterId);
        PlayerSlot GetPlayerSlot(int playerSlot);
        IReadOnlyList<PlayerSlot> GetAllPlayerSlots();

        void SignalRoundEnd(int winnerIndex, bool matchOver);

        event Action<GamePhase> OnStateChanged;
        event Action<PlayerSlot> OnPlayerJoined;
        event Action<int> OnPlayerLeft;
        event Action OnAllPlayersReady;
    }
}
