# Story 003: Self-Hit Exclusion & Multi-Hit Prevention — AttackerId==TargetId, HitTargets Set, Mutual Hit

> **Epic**: collision-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/collision-system.md`
**Requirement**: TR-COL-017 ~ TR-COL-022
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: 自伤排除: AttackerId == TargetId 时跳过。多次命中检查: 查询攻击系统 HitTargets 集合, 已在集合中则跳过。同帧互命中: 碰撞系统不做优先级裁定, 两个 hitbox 同时命中对方 hurtbox 时发送两个独立 HitEvent, 由格斗状态机决定处理顺序。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 命中管线包含自伤排除步骤 (AttackerId == TargetId → skip)
- Required: 命中管线包含多次命中检查步骤 (HitTargets 集合查询)
- Guardrail: OnTriggerEnter2D callback < 0.1ms

---

## Acceptance Criteria

- [ ] 自伤排除: AttackerId == TargetId → 跳过碰撞事件, 不创建 HitEvent
- [ ] 多次命中检查: TargetId 已在攻击系统的 HitTargets 集合中 → 跳过
- [ ] 同帧互命中: 两个 hitbox 同时命中对方 hurtbox → 发送两个独立 HitEvent, 不裁定优先级
- [ ] 同一攻击 hitbox 多帧持续与同一 hurtbox 重叠: OnTriggerEnter2D 仅触发一次 (Unity Trigger 行为), HitTargets 提供额外防护
- [ ] 同帧 hitbox 同时碰到多个 hurtbox: 为每个 hurtbox 独立触发, 每个执行完整验证管线

---

## Implementation Notes

**来自 ADR-0003 的具体指导**:

1. 自伤排除在命中管线中的位置:
```
命中管线:
  1. 身份识别
  2. 自伤排除: if (AttackerId == TargetId) return;  ← 这里
  3. 多次命中检查
  4. 命中点计算
  5. HitEvent 分发
```

2. 多次命中检查:
   - 碰撞系统调用攻击系统接口查询 HitTargets
   - `if (attackSystem.HasAlreadyHit(attackId, targetId)) return;`
   - 如果攻击系统报告已命中, 跳过

3. 同帧互命中:
   - 碰撞系统不裁定优先级
   - 两个 OnTriggerEnter2D 各自独立处理
   - 各自创建独立的 HitEvent
   - 格斗状态机决定处理顺序

4. OnTriggerEnter2D 天然去重:
   - 仅在重叠**开始**时触发一次
   - 持续重叠不重复触发
   - HitTargets 是额外防护层

---

## Out of Scope

- Layer Matrix 配置 (Story 001)
- HitEvent 构建细节 (Story 002)
- 投射物碰撞路由 (Story 004)
- Hurtbox 大小管理 (Story 005)
- 命中点计算 (Story 006)
- HitTargets 集合的维护 (attack-system Story 003)

---

## QA Test Cases

- **AC-1 (自伤排除)**:
  - Given: Player 1 hitbox (AttackerId=1) 与 Player 1 hurtbox (TargetId=1) 重叠
  - When: OnTriggerEnter2D 触发
  - Then: AttackerId == TargetId → 跳过, 无 HitEvent

- **AC-2 (多次命中检查)**:
  - Given: Warrior GroundAttack 已命中 Rogue (HitTargets={2})
  - When: 同一攻击 hitbox 继续与 Rogue hurtbox 重叠
  - Then: OnTriggerEnter2D 不再次触发 (Unity 天然去重); 即使触发, HitTargets 检查也会过滤

- **AC-3 (同帧互命中)**:
  - Given: Player 1 和 Player 2 同一帧互相命中
  - When: 两个 OnTriggerEnter2D 回调触发
  - Then: 两个独立 HitEvent 分别创建和分发, 不裁定优先级

- **AC-4 (多帧重叠)**:
  - Given: hitbox 在 Active 阶段多帧与同一 hurtbox 重叠
  - When: 持续重叠
  - Then: OnTriggerEnter2D 仅在第一帧触发一次

- **AC-5 (多目标独立)**:
  - Given: hitbox 同时碰到多个 hurtbox
  - When: 多个 OnTriggerEnter2D 回调
  - Then: 每个独立执行完整管线 (自伤排除 + 多次命中检查)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/collision/self-hit-multi-hit-prevention_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Layer Matrix), Story 002 (HitEvent Construction — 命中管线框架)
- Unlocks: Story 004 (Projectile Collision — 需要自伤排除和多次命中检查)
