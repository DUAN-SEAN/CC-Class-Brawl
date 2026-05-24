# Story 001: Layer Collision Matrix — Unity Layers, Layer Collision Matrix, autoSyncTransforms, Marker Components

> **Epic**: collision-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/collision-system.md`
**Requirement**: TR-COL-001 ~ TR-COL-008
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: Unity Physics2D Trigger + Layer Collision Matrix 模式。Layer 配置: Hitbox(8) 仅与 Hurtbox(9)/SolidPlatform(11) 碰撞; Hurtbox(9) 仅与 Hitbox(8) 碰撞; Hitbox 不与 Hitbox 碰撞; autoSyncTransforms=true。HitboxData/HurtboxData 标记组件携带身份信息。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: Layer "Hitbox" (8) collides only with Hurtbox and SolidPlatform
- Required: Layer "Hurtbox" (9) collides only with Hitbox
- Required: Layer "SolidPlatform" (11) collides with Hitbox, Projectile, and characters
- Required: Hitbox/Hurtbox never collide with themselves
- Required: Physics2D.autoSyncTransforms = true
- Guardrail: 碰撞系统帧耗时 < 0.5ms

---

## Acceptance Criteria

- [ ] Unity Layer "Hitbox" (8) 已配置
- [ ] Unity Layer "Hurtbox" (9) 已配置
- [ ] Unity Layer "Projectile" (10) 已配置 (用于未来扩展)
- [ ] Unity Layer "SolidPlatform" (11) 已配置
- [ ] Layer Collision Matrix: Hitbox(8) 与 Hurtbox(9) 碰撞
- [ ] Layer Collision Matrix: Hitbox(8) 与 SolidPlatform(11) 碰撞
- [ ] Layer Collision Matrix: Hitbox(8) 不与 Hitbox(8) 碰撞 (投射物互相穿过)
- [ ] Layer Collision Matrix: Hurtbox(9) 不与 Hurtbox(9) 碰撞
- [ ] Physics2D.autoSyncTransforms = true 已设置
- [ ] HitboxData 标记组件: 携带 AttackerId (int), AttackId (string)
- [ ] HurtboxData 标记组件: 携带 TargetId (int)

---

## Implementation Notes

**来自 ADR-0003 的具体指导**:

1. Layer 配置 (Project Settings → Tags and Layers):
   - Layer 8: "Hitbox"
   - Layer 9: "Hurtbox"
   - Layer 10: "Projectile" (预留)
   - Layer 11: "SolidPlatform"

2. Layer Collision Matrix (Project Settings → Physics 2D):
   ```
   Hitbox ↔ Hurtbox:      ENABLED
   Hitbox ↔ SolidPlatform: ENABLED
   Hitbox ↔ Hitbox:       DISABLED (投射物互相穿过)
   Hurtbox ↔ Hurtbox:     DISABLED
   Hurtbox ↔ SolidPlatform: DISABLED (角色不与平台通过 Trigger 碰撞, 角色平台碰撞由 3C 系统处理)
   ```

3. autoSyncTransforms:
   - 在游戏初始化 (RuntimeInitializeOnLoadMethod 或 GameManager.Awake) 中设置
   - `Physics2D.autoSyncTransforms = true`
   - 性能影响可忽略 (碰撞体 < 10)

4. HitboxData 标记组件:
```csharp
public class HitboxData : MonoBehaviour
{
    public int AttackerId;
    public string AttackId;
}
```

5. HurtboxData 标记组件:
```csharp
public class HurtboxData : MonoBehaviour
{
    public int TargetId;
}
```

6. 这些标记组件附加到对应的 BoxCollider2D (IsTrigger=true) 所在的 GameObject 上, 供碰撞回调中读取身份信息。

---

## Out of Scope

- 碰撞回调处理逻辑 (Story 002)
- 自伤排除和多次命中 (Story 003)
- 投射物碰撞路由 (Story 004)
- Hurtbox 大小缩放 (Story 005)
- 命中点计算 (Story 006)
- PassThrough 平台的 Layer 配置 (arena-platform epic)

---

## QA Test Cases

- **AC-5 (Hitbox↔Hurtbox 碰撞)**:
  - Given: Hitbox layer GameObject 与 Hurtbox layer GameObject 重叠
  - When: 物理步执行
  - Then: OnTriggerEnter2D 触发

- **AC-6 (Hitbox↔SolidPlatform 碰撞)**:
  - Given: Hitbox layer GameObject 与 SolidPlatform layer GameObject 重叠
  - When: 物理步执行
  - Then: OnTriggerEnter2D 触发

- **AC-7 (Hitbox 不自碰)**:
  - Given: 两个 Hitbox layer GameObject 重叠
  - When: 物理步执行
  - Then: 无 OnTriggerEnter2D (投射物互相穿过)

- **AC-8 (Hurtbox 不自碰)**:
  - Given: 两个 Hurtbox layer GameObject 重叠
  - When: 物理步执行
  - Then: 无 OnTriggerEnter2D

- **AC-9 (autoSyncTransforms)**:
  - Given: hitbox position 在 FixedUpdate 中变更
  - When: 物理步执行
  - Then: 变更立即反映在碰撞检测中

- **AC-10/11 (标记组件)**:
  - Given: HitboxData 组件附加到 hitbox GameObject
  - When: 碰撞回调读取
  - Then: AttackerId 和 AttackId 可正确读取

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/collision/layer-collision-matrix_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Foundation epics (3c-system, game-state-management — 需要 Unity 项目基础配置)
- Unlocks: Story 002 (HitEvent Construction — 需要层配置和标记组件), Story 003 (Self-hit/Multi-hit), Story 004 (Projectile Collision), Story 005 (Hurtbox Management), Story 006 (Hitpoint Calculation)
