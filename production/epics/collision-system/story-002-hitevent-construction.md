# Story 002: HitEvent Construction — OnTriggerEnter2D Callback Routing, HitEvent Fields, Identity Resolution, Event Dispatch

> **Epic**: collision-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/collision-system.md`
**Requirement**: TR-COL-009 ~ TR-COL-016
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: CollisionDetector MonoBehaviour 注册在角色 GameObject 上, 接收所有 OnTriggerEnter2D 回调。命中检测管线: 身份识别 → 自伤排除 → 多次命中检查 → 命中点计算 → HitEvent 创建 → OnHitDetected 事件分发。HitEvent struct 包含 AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 单个 CollisionDetector MonoBehaviour per character 接收所有 OnTriggerEnter2D
- Required: HitEvent struct: AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter
- Required: 命中检测管线顺序: identity → self-hit → multi-hit → hitpoint → event
- Guardrail: OnTriggerEnter2D callback < 0.1ms

---

## Acceptance Criteria

- [ ] CollisionDetector MonoBehaviour 注册在角色 GameObject 上, 接收 OnTriggerEnter2D 回调
- [ ] OnTriggerEnter2D 正确识别 hitbox vs hurtbox (通过 layer 判断)
- [ ] 身份识别: 从 hitbox Collider 读取 HitboxData (AttackerId, AttackId), 从 hurtbox Collider 读取 HurtboxData (TargetId)
- [ ] 命中管线执行: 身份识别 → 自伤排除 → 多次命中检查 → 命中点计算 → HitEvent 创建
- [ ] HitEvent struct 包含: AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter
- [ ] OnHitDetected(HitEvent) 事件在命中确认后触发, 通知攻击系统和格斗状态机
- [ ] hitbox 的 AttackerId 与任何存活角色 ID 不匹配时忽略并记录警告
- [ ] hurtbox 的 TargetId 与任何存活角色 ID 不匹配时忽略并记录警告

---

## Implementation Notes

**来自 ADR-0003 的具体指导**:

1. CollisionDetector 回调架构:
```
OnTriggerEnter2D(Collider2D other):
  1. 识别自身是 hitbox 还是 hurtbox
  2. 识别对方是 hitbox 还是 hurtbox
  3. 如果是 hitbox 碰到 hurtbox → 执行命中管线
  4. 如果是 hitbox 碰到 SolidPlatform → 仅投射物: 通知攻击系统销毁
```

2. 命中检测管线:
```
OnTriggerEnter2D(Hitbox ↔ Hurtbox):
  1. 身份识别: hitbox 读 AttackerId/AttackId, hurtbox 读 TargetId
  2. 自伤排除: AttackerId == TargetId → skip
  3. 多次命中检查: 查询攻击系统 HitTargets 集合
  4. 命中点计算: 重叠区域 AABB 中心
  5. HitEvent {AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter}
  6. OnHitDetected(HitEvent) → 攻击系统 + Combat FSM
```

3. OnTriggerEnter2D 仅在重叠开始时触发一次, 天然防重复

4. 近战 hitbox 碰到 SolidPlatform 的 OnTriggerEnter2D 被忽略

5. HitEvent struct (~80 bytes, 栈分配, 零 GC):
```csharp
public struct HitEvent
{
    public int AttackerId;
    public int TargetId;
    public string AttackId;
    public Vector2 HitPoint;
    public Vector2 HitboxCenter;
    public Vector2 HurtboxCenter;
}
```

6. 事件签名 (ADR-0008): OnHitDetected(HitEvent)

---

## Out of Scope

- Layer Matrix 配置 (Story 001)
- 自伤排除和多次命中的具体逻辑 (Story 003)
- 投射物碰撞路由 (Story 004)
- Hurtbox 大小管理 (Story 005)
- 命中点计算公式验证 (Story 006)

---

## QA Test Cases

- **AC-1 (CollisionDetector 回调)**:
  - Given: CollisionDetector 已注册, hitbox 与 hurtbox 重叠
  - When: OnTriggerEnter2D 触发
  - Then: 回调正确接收并处理

- **AC-2 (Layer 判断)**:
  - Given: OnTriggerEnter2D(Collider2D other)
  - When: 检查 other.gameObject.layer
  - Then: 正确区分 Hitbox(8), Hurtbox(9), SolidPlatform(11)

- **AC-3 (身份识别)**:
  - Given: hitbox 携带 HitboxData(AttackerId=1, AttackId="warrior_ground"), hurtbox 携带 HurtboxData(TargetId=2)
  - When: 碰撞回调处理
  - Then: 正确读取所有身份信息

- **AC-5 (HitEvent 构建)**:
  - Given: 命中确认 (AttackerId=1, TargetId=2, AttackId="warrior_ground")
  - When: HitEvent 创建
  - Then: 所有 6 个字段正确填充

- **AC-6 (OnHitDetected 事件)**:
  - Given: 监听者已订阅 OnHitDetected
  - When: HitEvent 创建完成
  - Then: 事件触发, 监听者收到完整 HitEvent

- **AC-7 (无效 AttackerId)**:
  - Given: hitbox 的 AttackerId=99, 无此角色
  - When: 碰撞回调处理
  - Then: 忽略事件, 记录警告

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/collision/hitevent-construction_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Layer Collision Matrix — 层和标记组件必须已配置)
- Unlocks: Story 003 (Self-hit/Multi-hit — 命中管线中的验证步骤), Story 004 (Projectile Collision), Story 006 (Hitpoint Calculation)
