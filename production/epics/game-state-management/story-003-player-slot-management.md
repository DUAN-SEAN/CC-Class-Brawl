# Story 003: Player Slot Management — PlayerSlot Data, Device Pairing, OnPlayerJoined/Left

> **Epic**: 游戏状态管理
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/game-state-management.md`
**Requirement**: TR-GST-015 ~ TR-GST-018 (玩家数据相关)
**ADR Governing Implementation**: ADR-0007: Scene & Game State Management, ADR-0005: Input System
**ADR Decision Summary**: PlayerSlot 数据跨状态持久化, PlayerInputManager DontDestroyOnLoad 跨场景, JoinPlayersWhenButtonIsPressed 自动加入, device pairing 通过 onPlayerJoined 回调。PlayerSlot 结构: PlayerIndex, CharacterId, IsConnected, IsReady。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: PlayerSlot 数据必须在 DontDestroyOnLoad GameStateManager 中跨状态持久化
- Required: PlayerInputManager 单例, joinBehavior: JoinPlayersWhenButtonIsPressed, maxPlayerCount: 2 (MVP)
- Required: Device pairing flow: EnableJoining → onPlayerJoined → auto-assign device+index → DisableJoining
- Required: Device disconnect: onDeviceLost → hold state → notify UI
- Required: MaxPlayerCount = 2, 架构预留 4 人
- Guardrail: MVP 不允许 1 人开始对战

---

## Acceptance Criteria

- [ ] PlayerSlot 结构体: PlayerIndex (int), CharacterId (string), IsConnected (bool), IsReady (bool)
- [ ] 玩家通过手柄连接自动注册为参战者 (PlayerInputManager.onPlayerJoined)
- [ ] 每个注册玩家持有 PlayerSlot 数据对象, 包含玩家编号, 已选角色, 输入设备引用
- [ ] 最多 2 人注册 (MVP), 架构预留 4 人
- [ ] OnPlayerJoined(PlayerSlot) 事件在玩家加入时触发, 所有监听者收到通知
- [ ] OnPlayerLeft(int playerIndex) 事件在设备断开时触发
- [ ] SetPlayerCharacter(playerSlot, characterId) 注册角色选择, 新选择立即生效
- [ ] 玩家数据在状态间持久化 — 角色选择在局间保持, 直到玩家主动更换
- [ ] 输入设备断开时玩家槽位保留, IsConnected=false, 显示"等待连接"
- [ ] CharacterSelect 中只剩 1 个手柄连接时无法完成选择, 显示"等待第二位玩家"

---

## Implementation Notes

**来自 ADR-0007/ADR-0005 的具体指导**:

1. PlayerSlot 在 GameStateManager 中持有 (DontDestroyOnLoad), 跨场景持久化

2. IGameState 接口 (已定义):
```csharp
void SetPlayerCharacter(int playerSlot, string characterId);
PlayerSlot GetPlayerSlot(int playerSlot);
IReadOnlyList<PlayerSlot> GetAllPlayerSlots();
```

3. Device pairing 流程:
   - CharacterSelect 进入时: PlayerInputManager.EnableJoining()
   - onPlayerJoined 回调: 自动分配 device + playerIndex
   - 所有玩家就绪后: DisableJoining()

4. Device disconnect:
   - onDeviceLost 回调: PlayerSlot.IsConnected = false
   - 槽位保留, 不移除
   - 其他玩家可继续操作

5. 角色选择:
   - 不锁定角色 — 两个玩家可以选择相同角色 (MVP)
   - 切换角色立即生效, 旧选择释放

6. 事件 (ADR-0008):
   - OnPlayerJoined(PlayerSlot) — 生产者: GameStateManager
   - OnPlayerLeft(int) — 生产者: GameStateManager
   - OnAllPlayersReady() — 生产者: GameStateManager

---

## Out of Scope

- GamePhase FSM (Story 001)
- 场景加载/卸载 (Story 002)
- 倒计时与输入冻结 (Story 004)
- BattleEnd 冻结帧 (Story 005)
- 角色选择 UI 的具体实现 (UI epic)

---

## QA Test Cases

- **AC-2 (自动注册)**:
  - Given: CharacterSelect 状态, EnableJoining 已调用
  - When: 手柄按下按钮
  - Then: PlayerSlot 创建, OnPlayerJoined 触发

- **AC-5 (OnPlayerJoined 事件)**:
  - Given: 监听者已注册
  - When: 新玩家加入
  - Then: OnPlayerJoined(slot) 触发, 包含完整 PlayerSlot 数据

- **AC-6 (OnPlayerLeft 事件)**:
  - Given: 玩家1 已注册
  - When: 玩家1 手柄断开
  - Then: OnPlayerLeft(0) 触发, PlayerSlot.IsConnected = false

- **AC-7 (SetPlayerCharacter)**:
  - Given: 玩家1 已注册
  - When: SetPlayerCharacter(0, "Warrior")
  - Then: PlayerSlot.CharacterId = "Warrior"

- **AC-8 (数据持久化)**:
  - Given: 玩家1=Warrior, 玩家2=Mage, 一局结束
  - When: 回到 CharacterSelect
  - Then: 两个 PlayerSlot 保持 Warrior/Mage

- **AC-9 (断开保留槽位)**:
  - Given: 2 个手柄连接
  - When: 玩家1 手柄断开
  - Then: 槽位保留, IsConnected=false, 玩家2 可继续操作

- **AC-10 (1人无法开始)**:
  - Given: 仅 1 个手柄连接
  - When: 检查是否可以开始对战
  - Then: 无法开始, 显示"等待第二位玩家"

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/game-state/player-slot-management_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GamePhase FSM), Story 002 (Scene Management — 场景加载后才有 GameScene)
- Unlocks: Story 004 (Countdown — 需要玩家数据), Story 005 (BattleEnd — 需要玩家数据)
