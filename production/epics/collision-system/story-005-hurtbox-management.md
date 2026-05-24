# Story 005: Hurtbox Management — Size Scaling (SilhouetteScale), Enable/Disable on KO, Hitstop Handling, IsHurtboxActive

> **Epic**: collision-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/collision-system.md`
**Requirement**: TR-COL-032 ~ TR-COL-035
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: 每个角色拥有永久 hurtbox (BoxCollider2D, IsTrigger=true, Hurtbox layer)。大小: HurtboxBaseSize * SilhouetteScale。KO 后 Collider2D.enabled=false 禁用。新回合 Collider2D.enabled=true 重新启用。hitstop 期间 hurtbox 不禁用 (hitstop 不提供无敌)。IsHurtboxActive(CharacterId) 查询接口。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: KO 角色 hurtbox 通过 Collider2D.enabled = false 禁用
- Required: 新回合 Collider2D.enabled = true 重新启用
- Guardrail: HurtboxBaseSize.x = 0.6u (0.4-0.8), HurtboxBaseSize.y = 1.0u (0.7-1.4)

---

## Acceptance Criteria

- [ ] 每个角色拥有永久 BoxCollider2D (IsTrigger=true), 位于 "Hurtbox" 层 (9)
- [ ] Hurtbox 大小: HurtboxSize = HurtboxBaseSize * SilhouetteScale
- [ ] Warrior (SilhouetteScale=1.2): HurtboxSize = (0.72, 1.2)
- [ ] Rogue (SilhouetteScale=0.85): HurtboxSize = (0.51, 0.85)
- [ ] Mage (SilhouetteScale=1.0): HurtboxSize = (0.6, 1.0)
- [ ] 角色 KO 后: Collider2D.enabled = false, hurtbox 不再参与碰撞检测
- [ ] 新回合开始: Collider2D.enabled = true, hurtbox 重新启用
- [ ] hitstop 期间: hurtbox 保持启用 (hitstop 不提供无敌), 可被新攻击命中
- [ ] IsHurtboxActive(CharacterId) 查询返回 hurtbox 启用状态
- [ ] HurtboxBaseSize 任一分量为 0 或负数: 使用硬编码最小值 (0.3, 0.5), 记录警告

---

## Implementation Notes

**来自 ADR-0003 的具体指导**:

1. Hurtbox 大小公式:
   - `HurtboxSize = HurtboxBaseSize * SilhouetteScale`
   - HurtboxBaseSize 默认 (0.6, 1.0) — 配置在 Tuning Knobs
   - SilhouetteScale 来自职业系统 ClassData

2. Hurtbox GameObject 结构:
   - Hurtbox 是角色 GameObject 的子物体
   - 附加 BoxCollider2D (isTrigger=true) + HurtboxData (TargetId)
   - layer = "Hurtbox" (9)

3. KO 禁用:
   - `hurtboxCollider.enabled = false`
   - Unity 物理引擎立即移除该碰撞体
   - 不再触发任何 OnTriggerEnter2D

4. 新回合启用:
   - `hurtboxCollider.enabled = true`
   - Unity 不会对已存在的重叠触发 OnTriggerEnter2D

5. hitstop 期间:
   - hurtbox 保持 enabled=true
   - 可正常被新攻击命中 (hitstop 不提供无敌)

6. 数据验证:
   - HurtboxBaseSize.x < 0.3 或 y < 0.5 → 强制使用 (0.3, 0.5)

---

## Out of Scope

- Layer Matrix 配置 (Story 001)
- HitEvent 构建 (Story 002)
- 自伤排除/多次命中 (Story 003)
- 投射物碰撞路由 (Story 004)
- 命中点计算 (Story 006)
- SilhouetteScale 的定义 (class-system epic)

---

## QA Test Cases

- **AC-2 (大小缩放 Warrior)**:
  - Given: HurtboxBaseSize = (0.6, 1.0), SilhouetteScale = 1.2
  - When: hurtbox 创建
  - Then: HurtboxSize = (0.72, 1.2)

- **AC-3 (大小缩放 Rogue)**:
  - Given: HurtboxBaseSize = (0.6, 1.0), SilhouetteScale = 0.85
  - When: hurtbox 创建
  - Then: HurtboxSize = (0.51, 0.85)

- **AC-4 (大小缩放 Mage)**:
  - Given: HurtboxBaseSize = (0.6, 1.0), SilhouetteScale = 1.0
  - When: hurtbox 创建
  - Then: HurtboxSize = (0.6, 1.0)

- **AC-5 (KO 禁用)**:
  - Given: 角色 KO 后
  - When: 投射物飞过 KO 角色位置
  - Then: 无 OnTriggerEnter2D, 投射物穿过

- **AC-6 (新回合启用)**:
  - Given: 新回合开始
  - When: Collider2D.enabled = true
  - Then: hurtbox 重新参与碰撞检测, 无残留 hitbox 误判

- **AC-7 (hitstop 不禁用)**:
  - Given: 角色在 hitstop 期间
  - When: 另一个 hitbox 与其 hurtbox 重叠
  - Then: OnTriggerEnter2D 正常触发, HitEvent 正常发送

- **AC-8 (IsHurtboxActive)**:
  - Given: hurtbox 已启用
  - When: IsHurtboxActive(characterId) 查询
  - Then: 返回 true
  - Edge cases: KO 后返回 false

- **AC-9 (数据验证)**:
  - Given: HurtboxBaseSize = (0, -1)
  - When: hurtbox 创建
  - Then: 使用 (0.3, 0.5), 记录警告

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/collision/hurtbox-management_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Layer Matrix — hurtbox layer 和标记组件)
- Unlocks: Story 006 (Hitpoint Calculation — 需要 hurtbox 位置和大小)
