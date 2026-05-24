# Story 004: Countdown & Input Freeze — 3 秒倒计时, 3C 输入冻结/解冻

> **Epic**: 游戏状态管理
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/game-state-management.md`
**Requirement**: TR-GST-003, TR-GST-016, TR-GST-019 (倒计时相关)
**ADR Governing Implementation**: ADR-0007: Scene & Game State Management
**ADR Decision Summary**: Countdown 状态持续 3 秒 (固定), 期间冻结 3C 输入, 显示 3-2-1 倒计时, 3 秒后自动转换到 Battle。倒计时显示帧映射公式: DisplayNumber = Max(1, Ceil(RemainingCountdownTime / (1.0/60.0) / 60))。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: CountdownDuration = 3.0s (安全范围 1.0-5.0)
- Required: Countdown 状态下冻结 3C 输入
- Required: 3C 系统通过 IMovementController.FreezeMovement(bool) 接口冻结/解冻
- Guardrail: Countdown 显示 3, 2, 1 各持续 1 秒 (±2 帧)

---

## Acceptance Criteria

- [ ] Countdown 状态持续 CountdownDuration (3.0s), 显示 3, 2, 1 各持续 1 秒 (±2 帧)
- [ ] 倒计时显示帧映射: DisplayNumber = Max(1, Ceil(RemainingCountdownTime / (1.0/60.0) / 60))
- [ ] 3 秒后自动转换到 Battle 状态
- [ ] Countdown 期间所有玩家 3C 输入被冻结 — 不响应方向/跳跃/攻击输入
- [ ] Countdown 结束时解冻所有玩家 3C 输入
- [ ] OnStateChanged(Battle) 事件在转换时触发

---

## Implementation Notes

**来自 ADR-0007 的具体指导**:

1. Countdown 状态进入动作: 冻结 3C 输入, 显示 3-2-1 倒计时
2. Countdown 状态退出动作: 解冻输入

3. 倒计时计时使用 Time.fixedDeltaTime (1/60s) 在 FixedUpdate 中递减
4. 倒计时显示数字通过公式计算:
   ```
   DisplayNumber = Max(1, Ceil(RemainingCountdownTime / (1.0 / 60.0) / 60))
   ```
   输出范围: 1 到 CountdownDuration 的整数

5. 3C 冻结接口 (ADR-0002):
   - IMovementController.FreezeMovement(true) — 冻结移动
   - CombatFSM 通知 — 需要通过 ICombatStateProvider 禁止攻击输入

6. 倒计时 UI 由对局 UI 系统实现, 本 story 仅提供倒计时数据和状态转换

---

## Out of Scope

- 倒计时的视觉/音频表现 (UI/VFX epic)
- GamePhase FSM 核心逻辑 (Story 001)
- 场景管理 (Story 002)
- PlayerSlot 管理 (Story 003)
- BattleEnd 冻结帧 (Story 005)

---

## QA Test Cases

- **AC-1 (倒计时 3-2-1)**:
  - Given: 进入 Countdown 状态
  - When: 0-1s: 显示 3; 1-2s: 显示 2; 2-3s: 显示 1
  - Then: 每个数字持续 1 秒 (±2 帧)
  - Edge cases: CountdownDuration 可配置, 默认 3.0s

- **AC-3 (自动转换到 Battle)**:
  - Given: Countdown 计时完成 (3.0s)
  - When: RemainingCountdownTime <= 0
  - Then: 自动 TransitionTo(Battle)

- **AC-4 (3C 输入冻结)**:
  - Given: Countdown 状态
  - When: 玩家按方向/跳跃/攻击
  - Then: 3C 系统不响应任何输入

- **AC-5 (3C 输入解冻)**:
  - Given: Countdown 结束转换到 Battle
  - When: Battle 状态开始
  - Then: 所有玩家 3C 输入正常响应

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/game-state/countdown-input-freeze_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GamePhase FSM), Story 002 (Scene Management), Story 003 (PlayerSlot)
- Unlocks: Story 005 (BattleEnd — 倒计时是多局循环的回归点)
