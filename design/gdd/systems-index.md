# Systems Index: 职业对决 (Class Brawl)

> **Status**: Draft
> **Created**: 2026-05-23
> **Last Updated**: 2026-05-23
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

职业对决是一款 2D 横版格斗游戏，融合肉鸽随机技能成长机制。核心循环是：基础职业对战 → 积攒专注值 → 随机解锁技能 → 每局不同的战斗体验。系统架构分为 5 层：Foundation（3C、场地、状态管理）→ Core（战斗、职业、专注值、技能数据）→ Feature（技能抽取/装备、协同、对局管理）→ Presentation（HUD、视觉、音效）→ Polish（AI）。MVP 需要 15 个系统来验证核心假设："随机技能进化的格斗对战是否好玩"。

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | 3C系统 | Core | MVP | In Review | [3c-system.md](3c-system.md) | — |
| 2 | 场地/平台系统 | Core | MVP | Designed | [arena-platform-system.md](arena-platform-system.md) | — |
| 3 | 游戏状态管理 | Core | MVP | Designed | [game-state-management.md](game-state-management.md) | — |
| 4 | 格斗状态机 | Gameplay | MVP | Designed | [combat-state-machine.md](combat-state-machine.md) | 3C系统 |
| 5 | 职业系统 | Gameplay | MVP | Designed | [class-system.md](class-system.md) | 3C系统 |
| 6 | 攻击系统 | Gameplay | MVP | Designed | [attack-system.md](attack-system.md) | 3C系统, 格斗状态机 |
| 7 | 碰撞判定系统 | Gameplay | MVP | Designed | [collision-system.md](collision-system.md) | 3C系统, 攻击系统 |
| 8 | 伤害计算系统 | Gameplay | MVP | Designed | [damage-calculation-system.md](damage-calculation-system.md) | 碰撞判定系统, 职业系统 |
| 9 | 击退与击飞系统 | Gameplay | MVP | Designed | [knockback-launch-system.md](knockback-launch-system.md) | 伤害计算系统, 场地/平台系统 |
| 10 | 专注值系统 | Gameplay | MVP | Designed | [focus-system.md](focus-system.md) | 碰撞判定系统 |
| 11 | 技能数据库 | Gameplay | MVP | Designed | [skill-database.md](skill-database.md) | 职业系统 |
| 12 | 技能抽取系统 | Gameplay | MVP | Designed | [skill-draw-system.md](skill-draw-system.md) | 技能数据库, 专注值系统 |
| 13 | 技能装备管理 | Gameplay | MVP | Designed | [skill-equipment-management.md](skill-equipment-management.md) | 技能抽取系统, 格斗状态机 |
| 14 | 对局管理系统 | Gameplay | MVP | Designed | [match-management-system.md](match-management-system.md) | 游戏状态管理, 击退与击飞系统 |
| 15 | 战斗HUD | UI | MVP | Designed | [battle-hud.md](battle-hud.md) | 伤害计算系统, 专注值系统, 技能装备管理 |
| 16 | 能量视觉系统 | Presentation | VS | Not Started | — | 专注值系统, 技能附属物系统 |
| 17 | 技能附属物系统 | Presentation | VS | Not Started | — | 技能装备管理, 3C系统 |
| 18 | 对局UI | UI | VS | Not Started | — | 对局管理系统, 游戏状态管理 |
| 19 | 角色选择UI | UI | VS | Not Started | — | 职业系统, 游戏状态管理 |
| 20 | 音效系统 | Audio | VS | Not Started | — | 碰撞判定系统, 技能装备管理 |
| 21 | 技能协同系统 | Gameplay | Alpha | Not Started | — | 技能装备管理, 技能数据库 |
| 22 | AI对手 | Meta | Alpha | Not Started | — | 格斗状态机, 攻击系统, 3C系统, 场地/平台系统 |

---

## Categories

| Category | Description |
|----------|-------------|
| **Core** | 基础系统能力：3C（输入/角色控制/摄像机）、场地、状态管理 |
| **Gameplay** | 让游戏好玩的系统：战斗、职业、专注值、技能、对局 |
| **UI** | 玩家信息显示：HUD、对局UI、角色选择 |
| **Presentation** | 视觉/音频反馈：能量视觉、附属物、音效 |
| **Audio** | 声音系统：打击音效、技能音效、BGM |
| **Meta** | 核心循环之外：AI对手 |

---

## Priority Tiers

| Tier | Definition | Target Milestone |
|------|------------|------------------|
| **MVP** | 核心循环必需。缺少任何一个都无法测试"好玩吗" | First playable |
| **Vertical Slice** | 一个完整区域的打磨体验，展示完整游戏感 | Demo / VS |
| **Alpha** | 所有功能以粗略形式存在 | Alpha milestone |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **3C系统** — 输入处理、角色移动/跳跃、摄像机控制是一切游戏体验的物理基础
2. **场地/平台系统** — 平台布局、碰撞体、blast zone 是战斗空间的定义
3. **游戏状态管理** — 菜单→选人→倒计时→战斗→结果的状态流转

### Core Layer (depends on foundation)

4. **格斗状态机** — depends on: 3C系统。角色战斗状态流转（idle/攻击/受击/击飞等）
5. **职业系统** — depends on: 3C系统。职业属性、基础招式定义
6. **攻击系统** — depends on: 3C系统, 格斗状态机。攻击发动/活跃/恢复帧、取消规则
7. **碰撞判定系统** — depends on: 3C系统, 攻击系统。hitbox/hurtbox 检测与配对
8. **伤害计算系统** — depends on: 碰撞判定系统, 职业系统。百分比伤害累积、倍率
9. **击退与击飞系统** — depends on: 伤害计算系统, 场地/平台系统。击退力、KO判定
10. **专注值系统** — depends on: 碰撞判定系统。命中获取专注值、解锁阈值
11. **技能数据库** — depends on: 职业系统。技能定义、属性、类型

### Feature Layer (depends on core)

12. **技能抽取系统** — depends on: 技能数据库, 专注值系统。随机抽取技能的 RNG 规则
13. **技能装备管理** — depends on: 技能抽取系统, 格斗状态机。技能激活/切换/上限
14. **对局管理系统** — depends on: 游戏状态管理, 击退与击飞系统。回合追踪、胜利条件

### Presentation Layer (depends on features)

15. **战斗HUD** — depends on: 伤害计算系统, 专注值系统, 技能装备管理。血条/专注值条/技能图标
16. **能量视觉系统** — depends on: 专注值系统, 技能附属物系统。脉动/碎片/震动
17. **技能附属物系统** — depends on: 技能装备管理, 3C系统。角色轮廓上的形状附属物
18. **对局UI** — depends on: 对局管理系统, 游戏状态管理。倒计时/FIGHT!/VICTORY!
19. **角色选择UI** — depends on: 职业系统, 游戏状态管理。职业选择/预览
20. **音效系统** — depends on: 碰撞判定系统, 技能装备管理。打击/技能/BGM

### Polish Layer

21. **技能协同系统** — depends on: 技能装备管理, 技能数据库。技能间协同效果检测
22. **AI对手** — depends on: 格斗状态机, 攻击系统, 3C系统, 场地/平台系统。单人练习

---

## Recommended Design Order

| Order | System | Priority | Layer | Est. Effort |
|-------|--------|----------|-------|-------------|
| 1 | 3C系统 | MVP | Foundation | M |
| 2 | 场地/平台系统 | MVP | Foundation | S |
| 3 | 游戏状态管理 | MVP | Foundation | S |
| 4 | 格斗状态机 | MVP | Core | M |
| 5 | 职业系统 | MVP | Core | S |
| 6 | 攻击系统 | MVP | Core | M |
| 7 | 碰撞判定系统 | MVP | Core | M |
| 8 | 伤害计算系统 | MVP | Core | S |
| 9 | 击退与击飞系统 | MVP | Core | S |
| 10 | 专注值系统 | MVP | Core | S |
| 11 | 技能数据库 | MVP | Core | M |
| 12 | 技能抽取系统 | MVP | Feature | M |
| 13 | 技能装备管理 | MVP | Feature | M |
| 14 | 对局管理系统 | MVP | Feature | S |
| 15 | 战斗HUD | MVP | Presentation | M |
| 16 | 能量视觉系统 | VS | Presentation | M |
| 17 | 技能附属物系统 | VS | Presentation | S |
| 18 | 对局UI | VS | Presentation | S |
| 19 | 角色选择UI | VS | Presentation | S |
| 20 | 音效系统 | VS | Presentation | M |
| 21 | 技能协同系统 | Alpha | Feature | M |
| 22 | AI对手 | Alpha | Polish | L |

---

## Circular Dependencies

- None found

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| 碰撞判定系统 | Technical | Hitbox/hurtbox 在高速格斗中需要帧精确的检测，错误会导致"打不中"或"穿模" | 早期原型验证，参考大乱斗社区已知方案 |
| 技能抽取系统 | Design | 随机抽取可能产生"必胜/必败"组合，破坏对局公平性 | 设计阶段定义技能稀有度权重和互斥规则 |
| 格斗状态机 | Technical | 状态转换的优先级和取消规则直接影响手感，复杂度容易失控 | 从简单状态开始，用状态图可视化 |
| 3C系统 | Technical | 移动/跳跃手感是格斗游戏的基础，调整频繁 | 数据驱动参数，快速迭代 |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 22 |
| Design docs started | 15 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 15/15 |
| VS systems designed | 0/5 |

---

## Next Steps

- [ ] Review and approve this systems enumeration
- [ ] Design MVP-tier systems first (use `/design-system [system-name]`)
- [ ] Run `/design-review` on each completed GDD
- [ ] Run `/gate-check pre-production` when MVP systems are designed
- [ ] Validate the highest-risk systems with `/vertical-slice` before committing to Production
