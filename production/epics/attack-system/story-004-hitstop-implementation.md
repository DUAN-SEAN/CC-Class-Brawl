# Story 004: Hitstop Implementation — Freeze Frame Counter, Hitbox Stays Active, Max Extension

> **Epic**: attack-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/attack-system.md`
**Requirement**: TR-ATK-013, TR-ATK-014, TR-ATK-015, TR-ATK-016
**ADR Governing Implementation**: ADR-0002: Dual FSM Architecture, ADR-0013: Projectile System
**ADR Decision Summary**: 命中时触发 hitstop: 通知格斗状态机暂停帧计数 (命中者和被命中者双方), 持续 HitstopFrames 帧。hitstop 期间双方角色动画冻结, hitbox 保持活跃 (Active 阶段不推进)。hitstop 结束后恢复帧计数。hitstop 不提供无敌。HitstopFrames=0 时无 hitstop。叠加规则: 新 hitstop 以更长的为准。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: CombatFSM 通过 ICombatStateProvider 接口控制帧推进
- Required: Hitstop 期间 Active 阶段不推进, hitbox 保持活跃
- Guardrail: DefaultHitstopFrames = 4 (安全范围 0-8)

---

## Acceptance Criteria

- [ ] 命中时触发 hitstop: 命中者和被命中者双方冻结 HitstopFrames 帧
- [ ] hitstop 期间: 双方角色帧计数暂停, hitbox 保持活跃 (Active 阶段不推进)
- [ ] hitstop 结束后: 帧计数恢复, 攻击阶段正常推进
- [ ] HitstopFrames=0: 无 hitstop, 命中瞬间无冻结, 立即继续
- [ ] hitstop 期间被新攻击命中: 新攻击的 hitstop 叠加 (不替换), 以更长的为准
- [ ] hitstop 期间攻击者被击中: 攻击者的 hitstop 被打断, 进入 HitStun/Knockback
- [ ] OnHitstopStart(int frames) 和 OnHitstopEnd() 事件触发 (ADR-0008)
- [ ] hitstop 不提供无敌 — hurtbox 仍然活跃, 可被新攻击命中

---

## Implementation Notes

**来自 ADR-0002/ADR-0013 的具体指导**:

1. Hitstop 实现:
   - 攻击系统通知 CombatFSM: FreezeFrameCounter(frames)
   - CombatFSM 在 FixedUpdate 中检查冻结计数器 > 0 时跳过帧推进
   - hitstop 期间 Active 阶段的 PhaseFrame 不递增

2. hitstop 期间 hitbox 保持活跃:
   - AttackSystem 不推进 AttackInstance.PhaseFrame
   - hitbox 保持当前位置和大小

3. HitstopFrames=0: 跳过整个 hitstop 逻辑, 零开销

4. 叠加规则:
   - 新 hitstop: remaining = max(current_remaining, new_hitstop_frames)
   - 不是累加, 是取最大值

5. 攻击者被打断:
   - 攻击者收到 HitStun → 清除 hitstop 计数器
   - 被命中者的 hitstop 正常结束 (不受影响)

6. 事件签名 (ADR-0008):
   - OnHitstopStart: Action<int> — (frames)
   - OnHitstopEnd: Action — 无参数

---

## Out of Scope

- 攻击生命周期 (Story 001)
- Hitbox 定位 (Story 002)
- 多次命中防护 (Story 003)
- 攻击类型解析 (Story 005)
- 投射物系统 (Story 006-007)
- hitstop 的视觉/动画冻结效果 (动画/VFX epic)

---

## QA Test Cases

- **AC-1 (命中触发 hitstop)**:
  - Given: 攻击命中 (HitstopFrames=4)
  - When: 命中确认
  - Then: 命中者和被命中者双方帧计数暂停 4 帧

- **AC-3 (hitstop 结束恢复)**:
  - Given: hitstop 进行中, 剩余 1 帧
  - When: 最后一帧结束
  - Then: 帧计数恢复, Active 阶段正常推进

- **AC-4 (HitstopFrames=0)**:
  - Given: HitstopFrames=0 的攻击命中
  - When: 命中确认
  - Then: 无冻结, 立即继续

- **AC-5 (hitstop 叠加)**:
  - Given: hitstop 进行中, 剩余 3 帧
  - When: 新攻击命中, HitstopFrames=5
  - Then: hitstop 延长至 5 帧 (取最大值)

- **AC-6 (攻击者被打断)**:
  - Given: 攻击者 hitstop 进行中
  - When: 攻击者被另一个攻击命中 (进入 HitStun)
  - Then: 攻击者 hitstop 被打断, 进入 HitStun

- **AC-7 (事件触发)**:
  - Given: 监听者已订阅
  - When: hitstop 开始/结束
  - Then: OnHitstopStart(4) / OnHitstopEnd() 触发

- **AC-8 (无无敌)**:
  - Given: hitstop 期间
  - When: 另一个 hitbox 与被命中者 hurtbox 重叠
  - Then: OnTriggerEnter2D 正常触发, HitEvent 正常发送

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/attack/hitstop-implementation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Attack Lifecycle), Story 002 (Hitbox Positioning), Story 003 (Multi-hit prevention — 先判定命中再触发 hitstop)
- Unlocks: None (后续 story 不直接依赖 hitstop, 但 combat-state-machine epic 需要)
