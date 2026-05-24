# Story: Attack Lifecycle

> **Epic**: attack-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/attack-system.md`
- **TR Range**: TR-ATK-001, TR-ATK-002, TR-ATK-005, TR-ATK-035
- **Governing ADR**: ADR-0002 (Dual FSM), ADR-0004 (Skill System — unified AttackData)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现攻击系统的核心生命周期管理：从 CombatFSM 接收阶段变化通知（OnPhaseChanged），管理 AttackInstance 的创建/更新/销毁。统一处理基础招式和技能招式（不区分来源），驱动 hitbox 的启用/禁用时机。这是攻击系统的骨架，后续故事在此基础上添加定位、命中防护、hitstop 和投射物。

## Acceptance Criteria (from GDD)

- **GIVEN** 角色进入 Attacking.Startup, **WHEN** 帧数未达到 StartupFrames, **THEN** 无 hitbox 存在
- **GIVEN** 攻击进入 Active 阶段（近战攻击）, **WHEN** Active 阶段开始, **THEN** hitbox 创建在 CharacterPosition + HitboxOffset x FacingDirection 位置
- **GIVEN** 攻击进入 Recovery 阶段, **WHEN** 近战 hitbox 存在, **THEN** hitbox 被销毁
- **GIVEN** 攻击被 HitStun 强制取消, **WHEN** hitbox 存在, **THEN** hitbox 立即销毁

## Implementation Notes (from ADR-0002, ADR-0004)

- AttackSystem 不维护独立状态机，生命周期由 CombatFSM 驱动
- AttackInstance 结构: AttackerId, Data (AttackData), StartFrame, Phase, PhaseFrame, MeleeHitbox (Collider2D)
- AttackSystem.FixedUpdateSystem() 由 CharacterController 协调器调用（在 CombatFSM 之后）
- 统一 AttackData 格式——职业基础招式和技能招式走完全相同的代码路径 (TR-ATK-001)
- 近战 hitbox 作为角色子物体，通过 SetActive(true/false) 启用/禁用

## Out of Scope

- Hitbox 定位细节（Story 002）
- 多次命中防护（Story 003）
- Hitstop（Story 004）
- 攻击类型解析（Story 005）
- 投射物系统（Story 006-007）

## Dependencies

- Foundation epics must be DONE
- combat-state-machine Story 001 (CombatFSM Core) must be DONE — 提供 OnPhaseChanged 回调
- `AttackData` 已定义（`Assets/Scripts/Core/Data/AttackData.cs`）
- `IAttackSystem` 接口已定义（`Assets/Scripts/Core/Interfaces/IAttackSystem.cs`）

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: Startup 阶段无 hitbox**
- Given: AttackSystem 收到 OnPhaseChanged(Startup)
- When: 检查 hitbox 状态
- Then: 无活跃 hitbox

**Test: Active 阶段创建 hitbox**
- Given: AttackSystem 收到 OnPhaseChanged(Active)，近战攻击
- When: 检查 hitbox 状态
- Then: hitbox GameObject 已激活

**Test: Recovery 阶段销毁 hitbox**
- Given: AttackSystem 收到 OnPhaseChanged(Recovery)
- When: 检查 hitbox 状态
- Then: hitbox GameObject 已禁用

**Test: HitStun 取消销毁 hitbox**
- Given: Active 阶段 hitbox 存在
- When: 收到取消通知（攻击被打断）
- Then: hitbox 立即禁用，AttackInstance 清理

**Test: 统一处理不同来源**
- Given: 基础招式 AttackData 和技能招式 AttackData
- When: 分别执行攻击生命周期
- Then: 行为完全一致（不区分来源）

## Test Evidence

- Automated unit tests: `tests/unit/attack/attack_lifecycle_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/AttackSystem.cs` (new — MonoBehaviour implementing IAttackSystem)
