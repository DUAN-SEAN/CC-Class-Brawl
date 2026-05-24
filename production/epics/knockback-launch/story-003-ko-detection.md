# Story 003: KO Detection

> **Epic**: 击退与击飞系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/knockback-launch-system.md`
**Requirement**: `TR-KBL-021~030`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: 每帧检查角色中心坐标是否超出 Blast Zone 边界，使用严格不等式（>/ <，不含等于），超出时触发 OnKO 事件，停止该角色物理更新。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: KO detection every frame using strict inequality
- Required: All velocity via Rigidbody2D.velocity direct assignment
- Guardrail: KnockbackSystem per-frame < 0.1ms

---

## Acceptance Criteria

- [ ] 每帧检查所有活跃角色是否超出 Blast Zone 边界
- [ ] KO 条件（任一满足）：position.x < BlastLeft | position.x > BlastRight | position.y < BlastBottom | position.y > BlastTop
- [ ] 严格不等式：恰好在边界上不判定 KO（position.x == BlastRight → IsKO=false）
- [ ] 角色在 (16.5, 3.0)、BlastRight=15.0 → IsKO=true
- [ ] 角色在 (15.0, 3.0)、BlastRight=15.0 → IsKO=false
- [ ] 角色在 (-3.0, -10.5)、BlastBottom=-10.0 → IsKO=true
- [ ] KO 触发后：发射 OnKO(CharacterId, KODirection) 事件，停止该角色物理更新
- [ ] 两个角色同一帧都被 KO：各自独立触发 OnKO 事件
- [ ] Blast Zone 数据从 IArenaDataProvider.GetBlastZone() 查询
- [ ] 场地未加载时 GetBlastZone() 返回默认值 (0,0,0,0)，所有角色立即 KO

---

## Implementation Notes

- KO 检测在 KnockbackSystem.FixedUpdate 中执行，检查所有角色位置
- 角色位置从 IMovementController（3C 系统）获取，或直接从 Transform 读取
- Blast Zone 从 IArenaDataProvider 查询，可缓存（场地不变）
- KO 后将该角色标记为已 KO，停止物理更新和后续 KO 检查
- KODirection 为枚举：Left, Right, Top, Bottom
- OnKO 事件通知对局管理系统和战斗 HUD

---

## Out of Scope

- 击退向量计算（Story 001）
- 击退物理模拟（Story 002）
- KO 视觉效果（屏幕闪光、摄像机缩放）（Presentation 层）
- KO 音效（Presentation 层）
- 对局管理系统对 KO 的处理（match-management epic）

---

## QA Test Cases

- **AC-1**: 右侧越界 KO
  - Given: position=(16.5, 3.0), BlastRight=15.0
  - When: 检查 KO
  - Then: IsKO=true, KODirection=Right
  - Edge cases: position=(15.0, 3.0) → IsKO=false

- **AC-2**: 下方越界 KO
  - Given: position=(-3.0, -10.5), BlastBottom=-10.0
  - When: 检查 KO
  - Then: IsKO=true, KODirection=Bottom
  - Edge cases: position=(0, -10.0) → IsKO=false

- **AC-3**: 安全位置
  - Given: position=(0, 0.75), BlastZone=(-15, 15, 14, -10)
  - When: 检查 KO
  - Then: IsKO=false
  - Edge cases: 所有边界内侧位置

- **AC-4**: 同帧双 KO
  - Given: P1 在 (16.5, 3.0), P2 在 (-16.0, 3.0)
  - When: 检查 KO
  - Then: 两个角色均触发 OnKO
  - Edge cases: 确认两个事件独立触发

- **AC-5**: KO 后停止更新
  - Given: 角色 KO 后
  - When: 后续 FixedUpdate
  - Then: 该角色不再检查 KO、不再更新速度
  - Edge cases: 确认其他活跃角色不受影响

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/knockback-launch/ko_detection_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story knockback-launch/001 (KnockbackVector), Story knockback-launch/002 (物理模拟驱动角色移动)
- Unlocks: Story 004 (状态管理), match-management epic (KO 事件消费)
