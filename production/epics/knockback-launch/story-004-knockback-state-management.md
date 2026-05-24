# Story 004: Knockback State Management

> **Epic**: 击退与击飞系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/knockback-launch-system.md`
**Requirement**: `TR-KBL-031~035`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: KnockbackRuntimeState 跟踪击退阶段（不可操作期/恢复期/无），击退系统与格斗状态机双向交互——FSM 判定 Knockback 状态，击退系统提供 KnockbackVector。CombatFSM 在 Knockback 期间调用 FreezeMovement(true) 和 SetVelocity。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: CombatState=Knockback must call SetVelocity(knockbackVector) + FreezeMovement(true) during hitstun
- Required: Knockback system delegates through IMovementController.SetVelocity
- Required: Unfreezing at end of CombatFSM FixedUpdate when transitioning from non-Idle to Idle
- Guardrail: KnockbackSystem per-frame < 0.1ms

---

## Acceptance Criteria

- [ ] KnockbackRuntimeState 生命周期：无击退 → 不可操作期 → 可操作恢复期 → 无击退（或 KO）
- [ ] 进入不可操作期条件：KnockbackMagnitude > KnockbackThreshold（来自格斗状态机）
- [ ] 不可操作期结束条件：HitstunFrames 耗尽
- [ ] 恢复期结束条件：速度回到正常范围（|Vx| <= MaxAirSpeed）或着地
- [ ] 多次击退：恢复期再次被命中时，新击退向量覆盖当前速度，旧衰减状态清除
- [ ] CombatFSM 在 Knockback 状态期间调用 FreezeMovement(true)
- [ ] Knockback 状态结束时（转 Idle）调用 FreezeMovement(false)
- [ ] KO 后 KnockbackRuntimeState 标记为终态，不再更新

---

## Implementation Notes

- KnockbackRuntimeState 是每个角色持有的数据结构，追踪当前阶段、剩余 hitstun 帧、当前速度
- KnockbackSystem 管理 Dictionary<CharacterId, KnockbackRuntimeState>
- 进入 Knockback：CombatFSM 判定 Magnitude > Threshold，将 CombatState 设为 Knockback，初始化 KnockbackRuntimeState
- 不可操作期每帧：递减 hitstun 帧计数器，更新速度（调用 IMovementController.SetVelocity）
- 恢复期：不调用 FreezeMovement，允许玩家输入，但击退残余速度通过 SetVelocity 持续衰减
- 着地事件从 3C 系统获取（IMovementController.IsGrounded() 检测）
- 本 story 侧重 KnockbackSystem 与 CombatFSM 的集成协调

---

## Out of Scope

- 击退向量计算细节（Story 001）
- 物理更新细节（Story 002）
- KO 检测逻辑（Story 003）
- 着地动画和 Landing 状态细节（combat-state-machine epic）
- 击退拖尾视觉（Presentation 层）

---

## QA Test Cases

- **AC-1**: KnockbackRuntimeState 生命周期
  - Given: 角色无击退状态
  - When: 受到 KnockbackMagnitude=10.0 攻击（> Threshold=9.0）
  - Then: 进入不可操作期，HitstunFrames 开始计数
  - Edge cases: Magnitude < Threshold → 不进入 Knockback（保持 HitStun）

- **AC-2**: 恢复期触发
  - Given: 不可操作期 HitstunFrames=9
  - When: 9 帧后
  - Then: 转入恢复期，FreezeMovement(false)
  - Edge cases: HitstunFrames=0 立即转入恢复期

- **AC-3**: 多次击退覆盖
  - Given: 恢复期 Vx=5.0 残余速度
  - When: 再次被命中，新 KnockbackVector=(15, 10)
  - Then: 速度直接设为 (15, 10)，旧残余速度清除
  - Edge cases: 不可操作期再次命中同理

- **AC-4**: 着地结束恢复期
  - Given: 恢复期角色着地
  - When: IsGrounded()=true
  - Then: 恢复期结束，垂直速度归零，3C 接管
  - Edge cases: 不可操作期着地同样结束

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/unit/knockback-launch/knockback_state_management_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story knockback-launch/001 (向量计算), Story knockback-launch/002 (物理), Story knockback-launch/003 (KO)
- Unlocks: combat-state-machine epic (Knockback 状态集成), match-management epic (KO 事件)
