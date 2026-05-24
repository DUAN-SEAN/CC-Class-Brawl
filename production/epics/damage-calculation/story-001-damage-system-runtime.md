# Story 001: Damage System Runtime

> **Epic**: 伤害计算系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/damage-calculation-system.md`
**Requirement**: `TR-DMG-001~010`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: DamageSystem 是 MonoBehaviour，订阅 CombatFSM 的 OnHitDetected 事件，使用纯静态 DamageFormulas 计算伤害百分比和击退力度，通过事件分发结果。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: DamageSystem subscribes to OnHitDetected from CombatFSM
- Required: DamagePercent update and KnockbackMagnitude calculation synchronous in same frame
- Required: DamageFormulas must be pure static class, 100% unit-testable
- Required: DamagePercent stored as float, only ever increases (MVP)
- Guardrail: Full pipeline (damage + knockback + KO, 2 players) < 0.2ms per frame

---

## Acceptance Criteria

- [ ] DamageSystem 订阅 OnHitDetected 事件，命中时更新目标 DamagePercent
- [ ] DamagePercent += BaseDamage（从 AttackData 查询），DamagePercent 只增不减
- [ ] 调用 DamageFormulas.CalculateKnockbackMagnitude 计算 KnockbackMagnitude
- [ ] 触发 OnDamagePercentChanged(CharacterId, float) 事件通知 HUD
- [ ] 触发 OnHitProcessed(HitEvent, AttackData, KnockbackMagnitude) 事件通知下游系统
- [ ] DisplayPercent = Floor(DamagePercent) 用于 HUD 显示
- [ ] 同帧多次命中：每次独立处理，第二次使用已更新的 DamagePercent
- [ ] 系统在 OnEnable 订阅、OnDisable 取消订阅所有事件（ADR-0008）

---

## Implementation Notes

- DamageSystem 实现 IDamageSystem 接口，提供 GetDamagePercent(CharacterId)、ResetDamage(CharacterId)、ResetAll() 方法
- 使用 Dictionary<CharacterId, float> 存储每个角色的 DamagePercent
- OnHitDetected 回调中：查询 AttackData（通过 IAttackDataProvider），更新 DamagePercent，计算 KnockbackMagnitude，分发事件
- AttackId 无效时忽略命中并记录错误日志
- 所有事件签名遵循 ADR-0008 规范：On + EventName + (sender key info, event data)
- DamageFormulas.CalculateDamagePercent 和 DamageFormulas.CalculateKnockbackMagnitude 已存在并通过测试，本 story 集成它们到 MonoBehaviour 运行时

---

## Out of Scope

- 击退力度 → 击退向量的转化（Story 002）
- 新一局重置逻辑（Story 003）
- 边界情况处理如 BaseKnockback=0、负数 DamagePercent（Story 004）
- HUD 显示实现（battle-hud epic）
- 伤害数字弹出视觉效果（Presentation 层）

---

## QA Test Cases

- **AC-1**: DamagePercent 累积
  - Given: 角色 DamagePercent=30.0, AttackData.BaseDamage=12.0
  - When: OnHitDetected 触发
  - Then: DamagePercent=42.0
  - Edge cases: DamagePercent=0 命中后 = BaseDamage; 多次命中依次累积

- **AC-2**: KnockbackMagnitude 计算
  - Given: BaseKnockback=8.0, BaseKnockbackGrowth=0.15, DamagePercent=100.0
  - When: 计算击退力度
  - Then: KnockbackMagnitude=9.2
  - Edge cases: DamagePercent=0 时 Magnitude=BaseKnockback

- **AC-3**: 事件分发
  - Given: DamageSystem 已初始化
  - When: 命中处理完成
  - Then: OnDamagePercentChanged 和 OnHitProcessed 均被触发
  - Edge cases: 无订阅者时不报错

- **AC-4**: DisplayPercent 取整
  - Given: DamagePercent=42.7
  - When: 查询显示百分比
  - Then: DisplayPercent=42
  - Edge cases: DamagePercent=0.3 → DisplayPercent=0

- **AC-5**: AttackId 无效
  - Given: HitEvent.AttackId 不存在于 AttackDataProvider
  - When: OnHitDetected 触发
  - Then: 命中被忽略，DamagePercent 不变，记录错误
  - Edge cases: 空字符串 AttackId

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/damage-calculation/damage_system_runtime_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: CombatFSM (OnHitDetected 事件), IAttackDataProvider (AttackData 查询), DamageFormulas (已有)
- Unlocks: Story 002 (KnockbackMagnitude → 击退向量), Story 003 (重置), Story 004 (边界情况)
