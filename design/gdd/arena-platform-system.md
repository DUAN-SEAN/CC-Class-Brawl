# 场地/平台系统 (Arena/Platform System)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 4: 快速战斗

## Overview

场地/平台系统定义了格斗对战的物理空间——平台布局、场地边界和淘汰区（Blast Zone）。作为 Foundation 层基础设施，它管理场地的碰撞体数据（平台位置、大小、类型）和空间边界规则（角色飞出边界即 KO），为 3C 系统提供地面检测依据、为摄像机提供边界约束、为击退与击飞系统提供 KO 判定坐标。每个场地由一个数据驱动配置定义（平台列表、边界坐标、视觉主题），允许设计师在不改代码的情况下创建新场地。虽然玩家不会直接"操作"场地系统，但场地的平台布局直接决定了战斗的空间策略——高低差带来的位置优势、平台间距影响的追逃路线、以及边缘危险区的心理压力，玩家每时每刻都在感受场地设计的影响。

## Player Fantasy

**核心幻想：「这个空间就是我的武器」**

玩家应该将场地本身视为战斗策略的一部分——不只是"背景舞台"，而是一个活的空间要素。平台的高低差决定了进攻路线的选择，边缘的危险区制造持续的紧张感，平台间距影响追与逃的博弈。高手玩家不只是操控角色，还操控空间——他们知道在哪个平台位置最有利，知道如何利用平台布局把对手逼向淘汰区。

**关键情感时刻**：
- **边缘恐惧** — 被击飞向场地边界时，看着自己即将飞出淘汰区的心跳加速
- **平台追逐** — 在多层平台间追击或逃离对手的紧张博弈，"他会上去还是下来？"
- **着陆点控制** — 预判对手的落点，提前占领关键平台的"猎人感"
- **空间掌控** — 将对手逼到不利位置（平台边缘、无平台覆盖的空域）的策略满足感

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 场地布局一眼就能理解，不需要学习"地图攻略"就能有效战斗
- 服务 **Pillar 4: 快速战斗** — 场地大小适中，战斗节奏紧凑，不会出现长时间追不到对手的情况

## Detailed Design

### Core Rules

**1. 场地配置（数据驱动）**

1. 每个场地由一个 `ArenaConfig` ScriptableObject 定义
2. 配置包含：
   - **场地ID** (`arenaId`)：唯一标识符
   - **场地名称** (`arenaName`)：显示名称
   - **平台列表** (`platforms`)：每个平台定义位置、大小、类型
   - **Blast Zone 边界** (`blastZone`)：矩形，定义 KO 判定区域
   - **摄像机边界** (`cameraBounds`)：矩形，约束摄像机显示范围
   - **出生点列表** (`spawnPoints`)：角色初始位置
   - **视觉主题ID** (`themeId`)：关联视觉资源
3. 场地配置可在 Inspector 中编辑，无需改代码即可创建新场地

**2. 平台系统**

1. 两种平台类型：
   - **实心平台（Solid）**：四面碰撞，角色无法穿越。用于场地底部主地面
   - **穿越平台（PassThrough）**：仅顶面碰撞，角色可从下方跳穿、可按下键+跳跃穿越落下。用于空中平台
2. 每个平台定义：
   - `position`: 平台中心坐标 (x, y)
   - `width`: 平台宽度（u）
   - `height`: 平台厚度（u），实心平台高度为碰撞体高度，穿越平台高度通常很小（0.1-0.2u）
   - `type`: Solid 或 PassThrough
3. Unity 实现：
   - 实心平台：`BoxCollider2D`，无特殊组件
   - 穿越平台：`BoxCollider2D` + `PlatformEffector2D`（`usedByEffector = true`，`surfaceArc = 180°`，`useOneWay = true`）
4. 平台运行时为只读——战斗期间平台不移动、不消失、不变形

**3. Blast Zone（淘汰区）**

1. 矩形区域，由四个边界值定义：`BlastLeft`, `BlastRight`, `BlastTop`, `BlastBottom`
2. 判定规则：角色中心点超出任一边界时，触发 KO 信号
3. KO 判定由击退与击飞系统负责执行（查询场地系统获取边界值），场地系统仅提供数据
4. Blast Zone 边界应大于摄像机边界，确保角色在被 KO 前先离开可视范围

**4. 摄像机边界**

1. 矩形区域，由四个边界值定义：`CamBoundLeft`, `CamBoundRight`, `CamBoundTop`, `CamBoundBottom`
2. 摄像机正交视角不得显示此边界之外的区域（与 3C GDD 的摄像机规则对齐）
3. 摄像机边界默认包含在 Blast Zone 内部，留出一段缓冲区域（`BlastBuffer`）

**5. 出生点**

1. 每个场地至少定义 2 个出生点（MVP 2 人对战）
2. 出生点必须位于实心平台上方或穿越平台上方
3. 出生点之间保持最小距离（`MinSpawnDistance`），防止角色重叠
4. 出生点有面朝方向设置（左/右）

**6. 场地生命周期**

1. **加载**：对局管理系统请求加载场地 → 实例化平台碰撞体 → 建立 Blast Zone → 设置摄像机边界
2. **激活**：战斗进行中，场地数据供其他系统查询
3. **卸载**：对局结束 → 销毁平台实例 → 清理引用

### States and Transitions

场地系统自身的状态较少，主要为场地数据的加载/卸载生命周期：

| 当前状态 | 触发条件 | 目标状态 |
|---------|---------|---------|
| Unloaded | 对局管理系统请求加载场地 | Loading |
| Loading | 平台实例化完成、碰撞体就绪 | Active |
| Loading | 配置验证失败或资源创建失败 | Error |
| Active | 对局管理系统宣布对局结束 | Unloading |
| Error | 对局管理系统调用 UnloadArena() | Unloaded |
| Unloading | 所有平台实例已销毁 | Unloaded |

场地在 Active 状态期间为只读——不响应任何修改请求。Error 状态下的数据查询返回默认值，调用方应检查 `GetState()` 后再使用查询结果。

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 3C系统 | 场地 → 3C | 平台碰撞体提供地面检测和着陆判定。3C 通过 Unity 物理系统（`OnCollisionEnter2D`）自动检测平台 |
| 3C系统（摄像机） | 场地 → 3C | 提供 `cameraBounds`，摄像机正交尺寸计算受此边界约束（3C GDD: "不能显示超出场地边界的区域"） |
| 击退与击飞系统 | 场地 → 击退 | 提供 Blast Zone 边界坐标。击退系统每帧检查角色位置是否超出边界来判定 KO |
| 击退与击飞系统 | 击退 → 场地 | KO 发生时通知场地系统（用于可能的视觉效果触发，如摄像机缩放） |
| 对局管理系统 | 对局 → 场地 | 请求加载/卸载场地，传入 `arenaId` |
| 对局管理系统 | 场地 → 对局 | 提供出生点坐标供角色初始化 |
| AI对手 | 场地 → AI | 提供平台布局数据（位置、类型、大小），供 AI 计算移动路径和位置策略 |

**场地系统向上提供的接口契约**:
- `IArenaDataProvider` 接口是所有系统查询场地数据的唯一入口
- 查询方法：`GetBlastZone()`, `GetCameraBounds()`, `GetPlatforms()`, `GetSpawnPoints()`
- 平台数据结构：`PlatformData { position, width, height, type }`
- 边界数据结构：`BoundsData { left, right, top, bottom }`

## Formulas

**单位系统**: 与 3C 系统一致，1 Unity 单位 = 64 像素 (PPU = 64)。

### 1. Blast Buffer 验证公式

`IsValid = (CamBoundLeft - BlastLeft >= MinBlastBufferX) AND (BlastRight - CamBoundRight >= MinBlastBufferX) AND (BlastTop - CamBoundTop >= MinBlastBufferY) AND (CamBoundBottom - BlastBottom >= MinBlastBufferY)`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 摄像机左边界 | CamBoundLeft | float | negative | 摄像机显示区域左 x 坐标 |
| 摄像机右边界 | CamBoundRight | float | positive | 摄像机显示区域右 x 坐标 |
| 摄像机上边界 | CamBoundTop | float | positive | 摄像机显示区域上 y 坐标 |
| 摄像机下边界 | CamBoundBottom | float | negative | 摄像机显示区域下 y 坐标 |
| Blast Zone 左 | BlastLeft | float | negative | KO 判定左边界 x |
| Blast Zone 右 | BlastRight | float | positive | KO 判定右边界 x |
| Blast Zone 上 | BlastTop | float | positive | KO 判定上边界 y |
| Blast Zone 下 | BlastBottom | float | negative | KO 判定下边界 y |
| 最小水平缓冲 | MinBlastBufferX | float | 默认 7.0 | 水平方向 blast buffer 最小值（u） |
| 最小垂直缓冲 | MinBlastBufferY | float | 默认 6.0 | 垂直方向 blast buffer 最小值（u） |

**Output Range:** Boolean — true 表示配置合法
**Example:** CamBoundLeft = -8.0, BlastLeft = -15.0. CamBoundLeft - BlastLeft = -8.0 - (-15.0) = 7.0 >= 7.0. Valid.

### 2. MVP 默认场地布局（战场型）

基于以下推导逻辑：
- `StageWidth = 12.0u`（以 MaxGroundSpeed 5.0 u/s 穿越 = 2.4s）
- `PlatformSpacingY = 2.8u`（从地面单跳可到达，JumpHeight = 3.5u > 2.8u）
- `CenterPlatformY = 5.0u`（从地面单跳不可到达 5.0 > 3.5u，需二段跳或借助侧平台）
- `PlatformWidth = 5.0u`（约 42% 舞台宽度，留出平台间空隙）

**平台布局:**

| 元素 | 类型 | 位置 (x, y) | 宽度 | 高度 |
|------|------|-------------|------|------|
| 主舞台 | Solid | (0, 0) | 12.0 | 0.5 |
| 左平台 | PassThrough | (-3.5, 2.8) | 5.0 | 0.15 |
| 中央平台 | PassThrough | (0, 5.0) | 5.0 | 0.15 |
| 右平台 | PassThrough | (3.5, 2.8) | 5.0 | 0.15 |

**边界定义:**

| 边界 | 值 | 推导依据 |
|------|-----|---------|
| CamBoundLeft | -8.0 | StageWidth/2(6) + CamPaddingX(3.0) - 调整(1) = 8 |
| CamBoundRight | 8.0 | 对称 |
| CamBoundTop | 8.0 | 中央平台顶(5.15) + JumpHeight(3.5) - margin ≈ 8.15 → 8.0 |
| CamBoundBottom | -3.0 | 主舞台底(-0.25) + 下落空间 |
| BlastLeft | -15.0 | CamBoundLeft(-8) - MinBlastBufferX(7) = -15 |
| BlastRight | 15.0 | 对称 |
| BlastTop | 14.0 | CamBoundTop(8) + MinBlastBufferY(6) = 14 |
| BlastBottom | -10.0 | CamBoundBottom(-3) - MinBlastBufferY(7) = -10 |

**出生点:**

| 出生点 | 位置 (x, y) | 面朝方向 |
|--------|------------|---------|
| Spawn 1 | (-3.0, 0.75) | Right |
| Spawn 2 | (3.0, 0.75) | Left |

Y = 0.75 = 主舞台顶面(0.25) + 角色半高(0.5)

### 3. 出生点最小距离

`SpawnDistance = Sqrt((SpawnA_x - SpawnB_x)^2 + (SpawnA_y - SpawnB_y)^2)`

`IsValid = SpawnDistance >= MinSpawnDistance`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 出生点 A 坐标 | SpawnA_x, SpawnA_y | float | any | 出生点 A 位置 |
| 出生点 B 坐标 | SpawnB_x, SpawnB_y | float | any | 出生点 B 位置 |
| 最小出生距离 | MinSpawnDistance | float | 默认 4.0 | 出生点最小间距（u） |
| 计算结果 | SpawnDistance | float | 0+ | 两出生点欧几里得距离 |

**Output Range:** SpawnDistance 0 到无上限；IsValid 为 boolean
**Example:** Spawn1 = (-3.0, 0.75), Spawn2 = (3.0, 0.75). Distance = Sqrt(36 + 0) = 6.0u >= 4.0. Valid.

### 4. 出生点平台位置验证

`IsValid = (SpawnX >= PlatformX - PlatformWidth/2) AND (SpawnX <= PlatformX + PlatformWidth/2) AND (SpawnY - PlatformTopY > 0) AND (SpawnY - PlatformTopY <= SpawnHeightOffset)`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 出生点 X | SpawnX | float | any | 出生点水平坐标 |
| 出生点 Y | SpawnY | float | any | 出生点垂直坐标 |
| 平台中心 X | PlatformX | float | any | 平台中心水平坐标 |
| 平台宽度 | PlatformWidth | float | 0+ | 平台宽度 |
| 平台顶面 Y | PlatformTopY | float | any | 平台顶面 Y 坐标 = position.y + height/2 |
| 出生高度偏移 | SpawnHeightOffset | float | 默认 0.5 | 出生点与平台顶面的最大允许距离（u） |

**Output Range:** Boolean — true 表示出生点有效
**Example:** Spawn (-3.0, 0.75), Main stage (0, 0, width=12.0, height=0.5, top=0.25). SpawnX=-3.0 在 [-6.0, 6.0] 内, SpawnY-PlatformTopY = 0.75-0.25 = 0.5 <= 0.5. Valid.

## Edge Cases

**平台与碰撞**:
- **如果角色从穿越平台的侧面碰撞**: 穿越平台仅有顶面碰撞体，侧面和底面无碰撞。角色从侧面穿过无阻碍，正常继续移动。
- **如果角色从下方跳穿越平台时恰好停在平台内部**: `PlatformEffector2D` 的 `surfaceArc = 180°` 确保只有从上方接触才有碰撞。从下方进入时无碰撞，角色自然穿过。
- **如果两个角色站在同一穿越平台的相同位置**: 无特殊处理。每个角色独立检测平台碰撞，不互斥。
- **如果角色在穿越平台上方着陆时被击退到平台边缘之外**: 正常处理——角色离开平台后进入 Falling 状态（由 3C 系统处理），平台不"抓住"角色。

**Blast Zone 边界**:
- **如果角色被击退速度极快，一帧之内跨越整个 Blast Zone**: 仍判定 KO。击退系统使用连续碰撞检测或射线检测，即使高速移动也不遗漏。
- **如果角色恰好停在 Blast Zone 边界线上**: 不判定 KO。判定条件是 `角色中心坐标 > 边界值`（严格大于），等于边界不触发。
- **如果角色同时超出两个边界（如右上角）**: 仍然只触发一次 KO 信号。KO 判定不分方向，只要超出任一边界即触发。

**场地配置**:
- **如果场地配置中的平台位置超出摄像机边界**: 配置有效但该平台在战斗中不可见。设计师负责确保平台布局合理。加载时输出警告日志但不阻止加载。
- **如果场地配置中的 Blast Zone 小于摄像机边界（违反 Blast Buffer 公式）**: 加载时验证失败，抛出配置错误异常，拒绝加载该场地。`IsValid = false` 时不允许进入 Active 状态。
- **如果出生点不在任何平台上方**: 加载时验证失败。出生点必须在某个平台（Solid 或 PassThrough）的顶面上方 `SpawnHeightOffset` 距离内。
- **如果只定义了 1 个出生点但需要 2 人对战**: 加载时验证失败。最少需要 2 个出生点。
- **如果两个出生点距离小于 MinSpawnDistance**: 加载时验证失败。

**运行时**:
- **如果对局进行中尝试修改场地配置**: 忽略。场地在 Active 状态为只读，所有修改请求被拒绝。
- **如果场地加载过程中出现资源错误（如碰撞体创建失败）**: 进入错误状态，通知对局管理系统加载失败，不进入 Active 状态。

## Dependencies

场地/平台系统是 Foundation 层，无上游依赖。以下是所有下游依赖关系：

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 3C系统 | 下游（硬依赖） | 查询 | 平台碰撞体提供地面检测和着陆判定（通过 Unity 物理系统自动交互）；提供 `cameraBounds` 约束摄像机显示范围 | 已设计 |
| 击退与击飞系统 | 下游（硬依赖） | 查询 | `IArenaDataProvider.GetBlastZone()` 返回四个边界坐标，击退系统用于 KO 判定 | 未设计 |
| 对局管理系统 | 下游（硬依赖） | 控制 + 查询 | 对局管理调用加载/卸载接口；场地提供 `GetSpawnPoints()` 供角色初始化 | 未设计 |
| AI对手 | 下游（软依赖） | 查询 | `IArenaDataProvider.GetPlatforms()` 返回平台布局数据，供 AI 路径决策 | 未设计 |
| 战斗HUD | 下游（软依赖） | 查询 | 可查询 Blast Zone 距离比例用于边缘警告视觉（如屏幕边缘变红） | 未设计 |

**场地系统向上提供的接口契约**:
- `IArenaDataProvider` 接口是所有系统查询场地数据的唯一入口
- `GetBlastZone()` → `BoundsData { left, right, top, bottom }`
- `GetCameraBounds()` → `BoundsData { left, right, top, bottom }`
- `GetPlatforms()` → `List<PlatformData>` where `PlatformData { position, width, height, type }`
- `GetSpawnPoints()` → `List<SpawnPointData>` where `SpawnPointData { position, facingDirection }`
- `GetState()` → `ArenaState { Unloaded, Loading, Active, Unloading, Error }`
- `LoadArena(arenaId)` → 请求加载场地（由对局管理系统调用）
- `UnloadArena()` → 请求卸载场地（由对局管理系统调用）

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属公式 |
|--------|--------|---------|---------|---------|---------|
| MinBlastBufferX | 7.0 u | 4.0-12.0 | 角色飞出屏幕后存活时间更长，KO 更慢 | KO 更快，可能看不到飞出动画 | Blast Buffer 验证 |
| MinBlastBufferY | 6.0 u | 3.0-10.0 | 垂直方向存活时间更长 | 垂直 KO 更快 | Blast Buffer 验证 |
| MinSpawnDistance | 4.0 u | 2.0-8.0 | 出生点更分散，开局面距更远 | 出生点更近，开局立即交战 | 出生点距离 |
| SpawnHeightOffset | 0.5 u | 0.2-1.0 | 出生点悬空更高 | 出生点更贴近平台表面 | 出生点验证 |

**场地配置旋钮（每个场地独立）**:

| 旋钮名 | 默认值（战场型） | 说明 |
|--------|----------------|------|
| StageWidth | 12.0 u | 主舞台宽度，影响战斗空间紧凑度 |
| PlatformWidth | 5.0 u | 空中平台宽度，影响着陆面积 |
| PlatformSpacingY | 2.8 u | 侧平台距地面高度，影响跳跃到达性 |
| CenterPlatformY | 5.0 u | 中央平台高度，影响空间层次 |
| CamBoundLeft/Right | ±8.0 u | 摄像机水平边界 |
| CamBoundTop | 8.0 u | 摄像机上边界 |
| CamBoundBottom | -3.0 u | 摄像机下边界 |
| BlastLeft/Right | ±15.0 u | KO 水平边界 |
| BlastTop | 14.0 u | KO 上边界 |
| BlastBottom | -10.0 u | KO 下边界 |

**旋钮交互警告**:
- `MinBlastBufferX/Y` 和 `CamBound*/Blast*` 相互约束——缩小摄像机边界时必须确保 Blast Zone 仍满足最小缓冲
- `StageWidth` 和 `PlatformSpacingY` 共同决定场地战斗空间感——改一个必须验证跳跃可达性
- `CenterPlatformY` 必须大于 `JumpHeight`(3.5u, 来自 3C GDD) 才能保持"不可单跳到达"的设计意图

## Visual/Audio Requirements

**场地视觉**:
- 主舞台和空中平台使用游戏艺术风格（像素风/简约几何）的材质渲染
- 平台边缘有微妙的发光/能量描边效果，与游戏"能量爆发"视觉主题对齐
- 穿越平台使用半透明或虚线视觉区分，与实心平台明确区分
- 场地背景使用暗色调（深灰/深蓝），让角色和技能特效成为视觉焦点（与游戏概念 Visual Identity 对齐）

**Blast Zone 视觉反馈**:
- 角色接近 Blast Zone 边界时，屏幕边缘出现红色/橙色渐变警告效果（由战斗HUD实现）
- 角色飞出摄像机边界后，显示方向指示箭头（由战斗HUD实现）
- 上述视觉效果由战斗HUD负责渲染，场地系统仅提供距离数据

**平台着陆效果**:
- 角色着陆时，平台表面产生小型尘土/能量粒子效果（由3C系统触发 OnLand 事件，视觉效果由技能附属物/能量视觉系统处理）
- 场地系统不直接产生粒子效果，仅提供平台表面的碰撞体供粒子定位

**音频反馈**（由音效系统实现，此处仅定义触发事件）:
- 无场地专属音频事件——场地是静态的背景元素
- 平台着陆音效由 3C 系统的 `OnLand` 事件触发，不归场地系统

## UI Requirements

场地系统不直接产生 UI 元素。以下信息由下游系统使用场地数据展示：
- 边缘警告视觉效果 → 战斗HUD 查询 Blast Zone 距离数据
- 方向指示箭头 → 战斗HUD 查询摄像机边界和角色位置
- 场地名称/主题 → 对局UI 在选场/加载界面显示

无独立的 UI 需求。

## Acceptance Criteria

### 平台碰撞

- **GIVEN** 一个实心平台（Solid）位于 (0, 0)，宽 12.0u，高 0.5u，**WHEN** 角色从任意方向（上下左右）接触平台，**THEN** BoxCollider2D 在四面阻挡角色通过
- **GIVEN** 一个穿越平台（PassThrough）位于 (-3.5, 2.8)，配有 PlatformEffector2D（surfaceArc=180°, useOneWay=true），**WHEN** 角色从上方下落到平台，**THEN** 角色正常着陆在平台顶面
- **GIVEN** 一个穿越平台（PassThrough），**WHEN** 角色从下方或侧面穿过平台，**THEN** 无碰撞，角色自由通过
- **GIVEN** 两个角色站在同一穿越平台上，**THEN** 各自独立检测碰撞，互不干扰

### Blast Zone 数据提供

- **GIVEN** 一个 Active 状态的场地，Blast Zone = {-15, 15, 14, -10}，**WHEN** 击退系统调用 `IArenaDataProvider.GetBlastZone()`，**THEN** 返回 `BoundsData { left=-15.0, right=15.0, top=14.0, bottom=-10.0 }`

### 摄像机边界

- **GIVEN** 一个 Active 状态的场地，**WHEN** 3C 摄像机系统调用 `IArenaDataProvider.GetCameraBounds()`，**THEN** 返回 `BoundsData { left=-8.0, right=8.0, top=8.0, bottom=-3.0 }`
- **GIVEN** 摄像机边界和 Blast Zone，**WHEN** 执行 Blast Buffer 验证公式，**THEN** 水平缓冲 = 7.0u >= MinBlastBufferX(7.0)，垂直缓冲 >= MinBlastBufferY(6.0)

### 出生点

- **GIVEN** 一个 Active 状态的场地，**WHEN** 对局管理系统调用 `GetSpawnPoints()`，**THEN** 返回至少 2 个 SpawnPointData，包含位置和面朝方向
- **GIVEN** MVP 默认场地，**WHEN** 查询出生点，**THEN** Spawn1 = (-3.0, 0.75) 面朝右，Spawn2 = (3.0, 0.75) 面朝左

### 场地生命周期

- **GIVEN** 一个有效的 ArenaConfig，**WHEN** `LoadArena(arenaId)` 被调用，**THEN** 状态从 Unloaded → Loading → Active；所有平台碰撞体实例化就绪；数据查询可用
- **GIVEN** 一个 Active 状态的场地，**WHEN** `UnloadArena()` 被调用，**THEN** 状态从 Active → Unloading → Unloaded；所有平台 GameObject 已销毁；后续查询返回默认值
- **GIVEN** 一个 Active 状态的场地，**WHEN** 任何系统尝试修改场地配置，**THEN** 修改被忽略，数据不变
- **GIVEN** 一个验证失败的 ArenaConfig（如 Blast Buffer 不足），**WHEN** `LoadArena()` 被调用，**THEN** 状态进入 Error 而非 Active；通知对局管理系统加载失败
- **GIVEN** 一个 Error 状态的场地，**WHEN** `UnloadArena()` 被调用，**THEN** 状态转为 Unloaded

### 配置验证

- **GIVEN** CamBoundLeft=-8.0, BlastLeft=-14.0（缓冲=6.0 < MinBlastBufferX 7.0），**WHEN** 加载场地，**THEN** 验证失败，不进入 Active 状态
- **GIVEN** 仅有 1 个出生点的 ArenaConfig，**WHEN** 加载场地，**THEN** 验证失败，提示至少需要 2 个出生点
- **GIVEN** 两个出生点距离 3.0u < MinSpawnDistance(4.0)，**WHEN** 加载场地，**THEN** 验证失败
- **GIVEN** 出生点 (50.0, 10.0) 无附近平台，**WHEN** 加载场地，**THEN** 验证失败
- **GIVEN** MVP 默认战场型配置（所有值正确），**WHEN** 加载场地，**THEN** 所有验证通过，进入 Active 状态

### 性能

- **GIVEN** 一个 Active 状态的场地（4 个平台），**WHEN** 其他系统查询场地数据，**THEN** 单次查询耗时 < 0.1ms（纯数据查找，无每帧计算）
- **GIVEN** 一个有效的 ArenaConfig，**WHEN** 加载场地（实例化 4 个碰撞体），**THEN** 加载在 1 帧内完成（< 16.6ms）
- **GIVEN** 一个 Active 状态的场地，**WHEN** 卸载场地，**THEN** 卸载在 1 帧内完成

## Open Questions

1. **移动平台是否纳入 MVP？** 当前设计中平台运行时为只读（不移动）。大乱斗部分场地有移动平台，增加动态策略。决定：MVP 不包含移动平台，后续版本考虑。（Owner: 设计师，里程碑: Alpha）
2. **场地变换/阶段切换是否考虑？** 部分格斗游戏有场地形态变化。决定：MVP 不包含，场地保持静态。（Owner: 设计师，里程碑: VS）
3. **摄像机边界钳制由谁负责？** 当前设计中场地系统提供边界数据，3C 摄像机系统负责遵守。是否需要在场地系统中强制钳制摄像机位置？决定：由 3C 摄像机系统负责，场地系统仅提供数据。（Owner: 程序，里程碑: 集成测试时验证）
4. **多次加载场地是否允许？** 当前未定义在 Active 状态下调用 LoadArena() 的行为。建议：拒绝并返回错误。（Owner: 程序，里程碑: 实现）
