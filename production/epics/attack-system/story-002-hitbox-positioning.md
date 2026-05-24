# Story 002: Hitbox Positioning — HitboxCenter Formula, X Mirror, autoSyncTransforms

> **Epic**: attack-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/attack-system.md`
**Requirement**: TR-ATK-003, TR-ATK-006, TR-ATK-007, TR-ATK-008
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: 近战 hitbox 作为角色 Rigidbody2D 子物体, 通过 Transform.localPosition 定位。HitboxCenter = CharacterPosition + Vector2(HitboxOffset.x * FacingDirection, HitboxOffset.y)。X 分量随面朝方向镜像。autoSyncTransforms=true 保证即时同步。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 近战 hitbox 是角色 Rigidbody2D 的子 GameObject, 通过 Transform.localPosition 偏移
- Required: Physics2D.autoSyncTransforms = true
- Required: Hitbox 定位公式: CharacterPosition + HitboxOffset * FacingDirection
- Guardrail: 碰撞系统帧耗时 < 0.5ms

---

## Acceptance Criteria

- [ ] HitboxCenter = CharacterPosition + Vector2(HitboxOffset.x * FacingDirection, HitboxOffset.y)
- [ ] 角色面朝右 (FacingDirection=1): hitbox 偏移 = HitboxOffset (原始值)
- [ ] 角色面朝左 (FacingDirection=-1): hitbox 偏移 X 分量镜像 (偏移 = -HitboxOffset.x)
- [ ] hitbox 位置随角色面朝方向实时更新 — 角色转身时 hitbox 跟随
- [ ] Physics2D.autoSyncTransforms = true, hitbox 位置变更在物理步中立即反映
- [ ] HitboxSize = (0, 0) 时 hitbox 创建但大小为零, 不命中任何目标, 记录警告

---

## Implementation Notes

**来自 ADR-0003 的具体指导**:

1. 近战 hitbox 定位:
   - hitbox GameObject 是角色 Rigidbody2D 的子物体
   - 位置通过 Transform.localPosition 设置偏移量
   - 随角色移动自动同步 (层级关系保证)

2. 面朝方向镜像:
   - Active 阶段开始时: localPosition = new Vector2(HitboxOffset.x * FacingDirection, HitboxOffset.y)
   - 每帧检查 FacingDirection 是否改变, 如果改变更新 localPosition

3. autoSyncTransforms:
   - 在游戏初始化时设置 Physics2D.autoSyncTransforms = true
   - 保证 FixedUpdate 中 hitbox 位置变更在物理步中立即反映

4. 公式示例:
   - 角色在 (2.0, 0.5), 面朝右(1), HitboxOffset = (0.8, 0.3)
   - HitboxCenter = (2.0 + 0.8, 0.5 + 0.3) = (2.8, 0.8)
   - 角色面朝左(-1): HitboxCenter = (2.0 - 0.8, 0.5 + 0.3) = (1.2, 0.8)

5. FacingDirection 来自 3C 系统: IMovementController.GetFacing()

---

## Out of Scope

- 攻击生命周期管理 (Story 001)
- 多次命中防护 (Story 003)
- Hitstop (Story 004)
- 攻击类型解析 (Story 005)
- 投射物系统 (Story 006-007)
- Hurtbox 管理 (collision-system epic)

---

## QA Test Cases

- **AC-1 (HitboxCenter 公式)**:
  - Given: 角色在 (2.0, 0.5), FacingDirection=1, HitboxOffset=(0.8, 0.3)
  - When: hitbox 创建
  - Then: HitboxCenter = (2.8, 0.8)

- **AC-2 (面朝右)**:
  - Given: FacingDirection=1, HitboxOffset=(0.8, 0.3)
  - When: hitbox 定位
  - Then: 偏移 X = +0.8

- **AC-3 (面朝左镜像)**:
  - Given: FacingDirection=-1, HitboxOffset=(0.8, 0.3)
  - When: hitbox 定位
  - Then: 偏移 X = -0.8

- **AC-4 (实时更新)**:
  - Given: hitbox 已创建, FacingDirection=1
  - When: 角色转身 FacingDirection=-1
  - Then: hitbox 位置 X 分量镜像更新

- **AC-6 (HitboxSize 零值警告)**:
  - Given: AttackData.HitboxSize = (0, 0)
  - When: hitbox 创建
  - Then: hitbox 大小为零, 记录警告日志

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/attack/hitbox-positioning_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Attack Lifecycle — hitbox 创建/销毁时机)
- Unlocks: Story 003 (Multi-hit prevention — 需要正确定位的 hitbox)
