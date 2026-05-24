# Story 006: HitPoint Calculation & Validation — AABB Overlap Center, Data Integrity, Performance

> **Epic**: collision-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/collision-system.md`
**Requirement**: TR-COL-036, TR-COL-037
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: 命中点 = hitbox 和 hurtbox 重叠区域的 AABB 中心。HitPoint = (OverlapMin + OverlapMax) / 2。OverlapMin = Max(HitboxMin, HurtboxMin), OverlapMax = Min(HitboxMax, HurtboxMax)。HitPoint 必须在重叠区域内。数据完整性: 无效 ID 忽略并警告。性能: 碰撞系统帧耗时 < 0.5ms。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 命中点计算为 AABB 重叠区域中心
- Required: HitEvent struct ~80 bytes, 栈分配, 零 GC
- Guardrail: 碰撞系统帧耗时 < 0.5ms (2 角色 + 少量投射物)

---

## Acceptance Criteria

- [ ] 命中点计算: HitPoint = (OverlapMin + OverlapMax) / 2
- [ ] OverlapMin = Max(HitboxMin, HurtboxMin), OverlapMax = Min(HitboxMax, HurtboxMax)
- [ ] HitPoint 在 hitbox 和 hurtbox 重叠区域内
- [ ] AABB 重叠判定: HitboxMin.x < HurtboxMax.x AND HitboxMax.x > HurtboxMin.x AND (Y 同理)
- [ ] 示例验证: Hitbox center (2.8, 0.8) size (0.6, 0.4), Hurtbox center (3.0, 0.9) size (0.72, 1.2) → HitPoint ≈ (2.93, 0.8)
- [ ] 非重叠 hitbox/hurtbox: 无 OnTriggerEnter2D 触发
- [ ] 碰撞系统帧耗时 < 0.5ms (2 角色 + 最多 5 个 hitbox + 2 个 hurtbox)

---

## Implementation Notes

**来自 ADR-0003 的具体指导**:

1. 命中点计算公式:
```
HitboxMin = HitboxCenter - HitboxSize / 2
HitboxMax = HitboxCenter + HitboxSize / 2
HurtboxMin = HurtboxCenter - HurtboxSize / 2
HurtboxMax = HurtboxCenter + HurtboxSize / 2

OverlapMin = Max(HitboxMin, HurtboxMin)
OverlapMax = Min(HitboxMax, HurtboxMax)
HitPoint = (OverlapMin + OverlapMax) / 2
```

2. AABB 重叠判定:
```
IsOverlapping = (HitboxMin.x < HurtboxMax.x) AND (HitboxMax.x > HurtboxMin.x)
           AND (HitboxMin.y < HurtboxMax.y) AND (HitboxMax.y > HurtboxMin.y)
```

3. 注意: Unity OnTriggerEnter2D 仅在重叠开始时触发, 命中点计算在回调中执行, 不需要手动检查 IsOverlapping (Unity 已确认重叠)

4. 性能考虑:
   - HitEvent struct 栈分配 (~80 bytes), 零 GC
   - 碰撞体数量极少 (< 10), Layer Matrix 过滤无效碰撞对
   - OnTriggerEnter2D 回调处理 < 0.1ms per callback

5. 示例计算验证:
   - Hitbox: center (2.8, 0.8), size (0.6, 0.4) → min (2.5, 0.6), max (3.1, 1.0)
   - Hurtbox: center (3.0, 0.9), size (0.72, 1.2) → min (2.64, 0.3), max (3.36, 1.5)
   - OverlapMin = Max((2.5, 0.6), (2.64, 0.3)) = (2.64, 0.6)
   - OverlapMax = Min((3.1, 1.0), (3.36, 1.5)) = (3.1, 1.0)
   - HitPoint = ((2.64+3.1)/2, (0.6+1.0)/2) = (2.87, 0.8)

---

## Out of Scope

- Layer Matrix 配置 (Story 001)
- HitEvent 构建和分发 (Story 002)
- 自伤排除/多次命中 (Story 003)
- 投射物碰撞路由 (Story 004)
- Hurtbox 大小管理 (Story 005)

---

## QA Test Cases

- **AC-1 (命中点公式)**:
  - Given: Hitbox center (2.8, 0.8) size (0.6, 0.4), Hurtbox center (3.0, 0.9) size (0.72, 1.2)
  - When: 命中确认
  - Then: HitPoint = 重叠区域中心 ≈ (2.87, 0.8), 且在重叠区域内

- **AC-4 (AABB 重叠判定)**:
  - Given: Hitbox [2.5, 3.1] x [0.6, 1.0], Hurtbox [2.64, 3.36] x [0.3, 1.5]
  - When: 计算重叠
  - Then: X: 2.5<3.36 AND 3.1>2.64 = true; Y: 0.6<1.5 AND 1.0>0.3 = true → Overlapping

- **AC-5 (示例验证)**:
  - Given: 上述输入
  - When: 计算
  - Then: HitPoint ≈ (2.87, 0.8), 在 [2.64, 3.1] x [0.6, 1.0] 范围内

- **AC-6 (非重叠)**:
  - Given: Hitbox center (5.0, 0.5) size (0.3, 0.3), Hurtbox center (1.0, 0.5) size (0.6, 1.0)
  - When: 物理步执行
  - Then: 无 OnTriggerEnter2D (不重叠)

- **AC-7 (性能)**:
  - Given: 2 角色 + 5 hitbox + 2 hurtbox
  - When: 碰撞系统执行一帧
  - Then: 帧耗时 < 0.5ms

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/collision/hitpoint-calculation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Layer Matrix), Story 002 (HitEvent Construction), Story 005 (Hurtbox Management — hurtbox 大小和位置)
- Unlocks: None (collision-system epic 最终 story)
