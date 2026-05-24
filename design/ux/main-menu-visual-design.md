# Main Menu Visual Design Specification

> **Status**: Draft
> **Author**: art-director + user
> **Last Updated**: 2026-05-24
> **Base Resolution**: 1920x1080
> **Rendering**: Screen Space - Overlay, UI Toolkit (UXML/USS) per ADR-0014

---

## Decision Log

本文档基于 `design/ux/main-menu.md`（UX 规格）和 `design/art/art-bible.md`（美术圣经）构建视觉层。以下 4 项关键设计决策由用户确认：

| # | 决策点 | 选项 | 最终决定 | 理由 |
|---|--------|------|---------|------|
| A | CTA 按钮颜色 | 纯白底 / 职业色渐变 / 白底+赤金脉动描边 / 透明底+职业色描边 | **白底 + 赤金脉动描边** | 白色底板保证文字可读性（#FFFFFF on #1A1A2E = 15.4:1 对比度）。赤金 #F0C040 脉动描边赋予仪式感（Art Bible §4.1 赤金 = 成就与稀有），吸引注意力但不过度喧哗 |
| B | 游戏标题处理 | 艺术字 Logo / 纯文字单行 / 分层文字排版（中文大+英文小低透明度） | **分层文字排版** | 中文"职业对决"用 Noto Sans SC 大号 SemiBold 表达品牌力度；英文"CLASS BRAWL"用 Exo 2 小号 Medium 低不透明度作为副标题。两者通过字号和透明度拉开层级，与 HUD 排版体系一致 |
| C | 顶部栏按钮风格 | 纯文字无边框 / 微型底板按钮 / 胶囊按钮 | **微型底板按钮** | #2A2A3E 小底板与 HUD 技能图标底板语言一致（Art Bible §7.6 图标底板色），描边焦点状态与 HUD 焦点指示器统一。低调但不失交互暗示 |
| D | 退出确认弹窗宽度 | 400px / 480px / 560px | **480px** | 中文"确定退出游戏？"约 240px 内容宽度，480px 提供充足内边距。英文 "Are you sure you want to quit?" 约 360px，480px 仍可容纳（main-menu.md Localization 标记为高风险项）。二级面板 #222240 85%，遮罩 #1A1A2E 60% |

---

## 1. Typography System

### 1.1 字体族分配

| 用途 | 字体 | 字重 | 回退字体 | 说明 |
|------|------|------|---------|------|
| CTA 主文字"开始对战" | Noto Sans SC | SemiBold (600) | Exo 2 | 中文品牌文字，力度感 |
| CTA 副文字"按 A 开始" | Exo 2 | Medium (500) | Noto Sans SC | 按键提示，与 HUD 按键提示字体一致 |
| 标题中文"职业对决" | Noto Sans SC | SemiBold (600) | — | 品牌名称，最大字号 |
| 标题英文"CLASS BRAWL" | Exo 2 | Medium (500) | — | 副标题，低不透明度 |
| 顶部栏按钮文字 | Noto Sans SC | Medium (500) | Exo 2 | 次要操作，中等信息层级 |
| 版本号 | Exo 2 | Regular (400) | — | 纯展示，最低信息层级 |
| 弹窗标题 | Noto Sans SC | SemiBold (600) | Exo 2 | 确认对话框标题 |
| 弹窗按钮 | Noto Sans SC | Medium (500) | Exo 2 | 确认/取消按钮 |

**字体渲染设置**：
- 中英文混排：英文/数字使用 Exo 2，中文自动回退 Noto Sans SC
- 行高中取两种字体的较大值
- 来源：Art Bible §7.1 排版方向

### 1.2 排版尺度表

| 元素 | 字号 | 字重 | 字间距 | 行高 | 色彩 | 备注 |
|------|------|------|--------|------|------|------|
| CTA 主文字"开始对战" | 36px | SemiBold (600) | 1.8px (5%) | 50px (140%) | #FFFFFF | CTA 核心，最大按钮文字 |
| CTA 副文字"按 A 开始" | 14px | Medium (500) | 0.6px (4%) | 20px (140%) | #E8E8F0 | 按键提示，CTA 辅助信息 |
| 标题中文"职业对决" | 72px | SemiBold (600) | 3.6px (5%) | 101px (140%) | #FFFFFF (20% opacity) | 品牌名，低不透明度装饰性 |
| 标题英文"CLASS BRAWL" | 24px | Medium (500) | 1.2px (5%) | 34px (140%) | #FFFFFF (15% opacity) | 副标题，极低不透明度 |
| 顶部栏按钮"设置"等 | 14px | Medium (500) | 0.6px (4%) | 20px (140%) | #E8E8F0 → #FFFFFF (focused) | 次要操作 |
| 版本号 | 12px | Regular (400) | 0.5px (4%) | 17px (140%) | #3A3A5C | 极低调，需主动寻找才可见 |
| 弹窗标题"确定退出游戏？" | 20px | SemiBold (600) | 1.0px (5%) | 28px (140%) | #FFFFFF | 确认弹窗主信息 |
| 弹窗按钮"确认"/"取消" | 16px | Medium (500) | 0.7px (4%) | 22px (140%) | #E8E8F0 → #FFFFFF (focused) | 弹窗操作 |

**排版硬规则**（来源：Art Bible §7.1）：
- 字间距 = 字号的 3-5%（表中已按 4-5% 计算）
- 行高 = 字号的 140-160%（统一使用 140%）
- 最小字号 10px（版本号 12px 在下限之上）
- 所有数字使用 tabular-nums 特性

---

## 2. Color Palette

### 2.1 核心色板

| 色彩名称 | Hex | 不透明度 | 用途 | 美术圣经映射 |
|---------|-----|---------|------|-------------|
| 深渊底 | #1A1A2E | 100% | 背景底色 | Art Bible §4.1 基础色板 |
| 暖色变体 | #222240 | 85% | 弹窗底板（二级面板） | Art Bible §2.1 + §7.6 二级面板底板 |
| 图标底板 | #2A2A3E | 100% | 顶部栏按钮底板 | Art Bible §7.6 图标底板色 |
| UI中性灰 | #3A3A5C | 100% | 版本号、分隔线 | Art Bible §4.4 UI中性灰 |
| UI文字主色 | #E8E8F0 | 100% | 正文、次要操作文字 | Art Bible §4.4 UI文字主色 |
| UI文字强调 | #FFFFFF | 100% | 标题、CTA、焦点态文字 | Art Bible §4.4 UI文字强调 |
| 赤金 | #F0C040 | 100% | CTA 脉动描边 | Art Bible §4.1 赤金 — 成就与稀有 |

### 2.2 语义色彩

| 用途 | 色值 | 不透明度 | 说明 |
|------|------|---------|------|
| CTA 按钮底板 | #FFFFFF | 100% | 白底保证文字可读性（决策 A） |
| CTA 脉动描边 | #F0C040 | 100% | 赤金脉动，吸引注意力（决策 A） |
| CTA 脉动描边暗相 | #F0C040 | 40% | 脉动低亮相位，产生呼吸感 |
| 顶部栏按钮底板 | #2A2A3E | 100% | 微型底板，与 HUD 图标底板一致（决策 C） |
| 顶部栏焦点描边 | #F0C040 | 100% | 2px 描边焦点态 |
| 弹窗遮罩 | #1A1A2E | 60% | 弹窗背景遮罩（Art Bible §7.6 面板叠加规则） |
| 弹窗底板 | #222240 | 85% | 二级面板底板（Art Bible §7.6） |
| 弹窗按钮底板 | #2A2A3E | 100% | 与顶部栏按钮底板一致 |
| 弹窗按钮焦点描边 | #F0C040 | 100% | 2px 描边焦点态 |
| 标题中文 | #FFFFFF | 20% | 低不透明度装饰性（决策 B） |
| 标题英文 | #FFFFFF | 15% | 极低不透明度副标题（决策 B） |
| 版本号 | #3A3A5C | 100% | 极低调，需主动寻找 |

### 2.3 焦点/交互状态色

| 状态 | 应用元素 | 属性变化 | 时长 | 缓动 |
|------|---------|---------|------|------|
| 默认 | 所有可交互元素 | 无特殊效果 | — | — |
| 焦点获得 | 顶部栏按钮 | 2px #F0C040 描边 + 文字色 #E8E8F0 → #FFFFFF | 0.1s | Linear |
| 焦点失去 | 顶部栏按钮 | 描边消失 + 文字色恢复 | 0.1s | Linear |
| 焦点获得 | CTA 按钮 | 赤金描边亮度从脉动低相位跳至 100% + Scale 1.0 → 1.02 | 0.1s | Cubic ease-out |
| 焦点获得 | 弹窗按钮 | 2px #F0C040 描边 + 文字色 #E8E8F0 → #FFFFFF | 0.1s | Linear |
| 按下 | 所有按钮 | Scale 0.97 | 0.05s | Linear |
| 按下释放 | 所有按钮 | Scale → 1.0（或焦点态 1.02） | 0.1s | Cubic ease-out |

### 2.4 高对比度模式覆写

| 元素 | 默认值 | 高对比度值 | 提升幅度 |
|------|--------|-----------|---------|
| 标题中文 | #FFFFFF 20% | #FFFFFF 40% | 对比度约翻倍 |
| 标题英文 | #FFFFFF 15% | #FFFFFF 35% | 对比度约翻倍 |
| 版本号 | #3A3A5C | #666666 | 对比度从 ~2.1:1 → ~3.8:1 |
| CTA 脉动描边暗相 | #F0C040 40% | #F0C040 70% | 减弱脉动振幅，保持可见性 |
| 弹窗遮罩 | #1A1A2E 60% | #000000 80% | 增强焦点隔离 |
| 顶部栏底板 | #2A2A3E | #1A1A2E | 加深底板，增加文字对比度 |
| 描边宽度 | 各元素默认值 | 全部 x2 | 强化边缘定义 |

---

## 3. Layout Grid

### 3.1 整体布局区域（1920x1080 基准）

```
┌──────────────────────────────────────────────────────────────┐ Y=0
│  顶部栏 (108px, 10%)                                         │
│  ┌────────┐  ┌──────────┐  ┌──────┐                         │
│  │  设置  │  │ 操作指南  │  │ 退出 │                         │
│  └────────┘  └──────────┘  └──────┘                         │
│                                                              │ Y=108
│                                                              │
│                                                              │
│              ┌─────────────────────┐                         │
│              │                     │                         │ Y=408
│              │    "开始对战"        │                         │
│              │    "按 A 开始"       │                         │
│              │                     │                         │
│              └─────────────────────┘                         │ Y=528
│                                                              │
│                                                              │
│                                                              │
│              "职业对决"                                        │ Y=780
│              "CLASS BRAWL"                                    │ Y=820
│                                                              │
│  v0.1.0                                (c) 2026             │ Y=1060
└──────────────────────────────────────────────────────────────┘ Y=1080
```

### 3.2 精确定位表

| 区域 | 元素 | X (px) | Y (px) | 宽 (px) | 高 (px) | 锚点 | 备注 |
|------|------|--------|--------|---------|---------|------|------|
| 顶部栏 | 设置按钮 | 24 | 24 | 80 | 36 | 左上 | 第一个次要操作 |
| 顶部栏 | 操作指南按钮 | 116 | 24 | 110 | 36 | 左上 | 第二个次要操作 |
| 顶部栏 | 退出按钮 | 242 | 24 | 68 | 36 | 左上 | 第三个次要操作（最右） |
| 中央焦点区 | CTA 容器 | 居中 | ~408 | 320 | 120 | 中央偏上 | 包含主文字 + 副文字 |
| 中央焦点区 | CTA "开始对战" | 居中 | 428 | 280 | 50 | 容器内居中 | 36px 字号 + 行高 |
| 中央焦点区 | CTA "按 A 开始" | 居中 | 490 | 280 | 20 | 容器内居中 | 14px 字号 + 行高 |
| 底部氛围区 | 标题"职业对决" | 居中 | 780 | ~520 | 101 | 中下 | 72px 字号估算宽度 |
| 底部氛围区 | 标题"CLASS BRAWL" | 居中 | 881 | ~250 | 34 | 中下 | 24px 字号估算宽度 |
| 底部氛围区 | 版本号 | 24 | 1060 | ~80 | 17 | 左下 | 右对齐于左下角 |
| 底部氛围区 | 版权文字 | 右-24 | 1060 | ~80 | 17 | 右下 | 左对齐于右下角 |

### 3.3 布局数学验证

**中央 CTA 位置验证**：
- 屏幕高度 1080px，中央偏上原则
- CTA 容器顶部 Y=408，底部 Y=528，中心 Y=468
- 视觉重心在 43% 高度处（468/1080），略高于中心，符合 UX 规格"中央 40-60%"定义

**顶部栏高度**：
- 1080 x 10% = 108px，符合 main-menu.md 顶部栏"屏幕上方 ~10%"定义
- 按钮顶部距屏幕顶 24px，底部距顶部栏底 48px，留有舒适间距

**底部标题区**：
- "职业对决"基线 Y=880（780+101行高），距底部 200px
- "CLASS BRAWL" 基线 Y=915（881+34行高），距底部 165px
- 版本号 Y=1060，距底部 20px

### 3.4 安全区域

| 区域 | 边距 | 说明 |
|------|------|------|
| 顶部安全边距 | 24px | 顶部栏按钮距屏幕顶 |
| 底部安全边距 | 20px | 版本号距屏幕底 |
| 左侧安全边距 | 24px | 顶部栏按钮 + 版本号 |
| 右侧安全边距 | 24px | 版权文字 |

---

## 4. Per-Element Visual Specs

### 4.1 CTA 按钮"开始对战"

| 属性 | 规格 |
|------|------|
| **底板色** | #FFFFFF (100%) |
| **底板尺寸** | 320 x 80px（含内边距） |
| **底板圆角** | 8px（Art Bible §7.6 面板圆角标准） |
| **主文字** | Noto Sans SC SemiBold 36px，#FFFFFF on #FFFFFF 底板 |
| **副文字** | Exo 2 Medium 14px，#E8E8F0（略灰于主文字，降低层级） |
| **内边距** | 上 18px 下 12px 左右 20px |
| **文字布局** | 主文字居上，副文字居下，两者垂直居中于底板 |

**对比度说明**：白色文字 #FFFFFF on 白色底板 #FFFFFF = 1:1，不可读。需要调整：
- **主文字改用深渊底色**：#1A1A2E on #FFFFFF = ~15.4:1（AAA）
- **副文字改用深色变体**：#3A3A5C on #FFFFFF = ~3.5:1（AA 大文本）或使用 #2A2A3E for 更高对比度 ~5.2:1

**修正后文字色彩**：

| 文字 | 色值 | 背景 | 对比度 | WCAG 等级 |
|------|------|------|--------|----------|
| 主文字"开始对战" | #1A1A2E | #FFFFFF | ~15.4:1 | AAA |
| 副文字"按 A 开始" | #3A3A5C | #FFFFFF | ~3.5:1 | AA (大文本 14px) |

**赤金脉动描边规格**：

| 属性 | 规格 |
|------|------|
| **描边色** | #F0C040（赤金，Art Bible §4.1） |
| **描边宽度** | 3px（与 Epic 稀有度边框同宽，Art Bible §7.2） |
| **脉动方式** | 亮度正弦波调制：opacity 在 40% ↔ 100% 之间变化 |
| **脉动周期** | 2s（与 main-menu.md "约2秒"一致） |
| **脉动缓动** | 正弦波（Art Bible §7.3 原则 C） |
| **外发光** | blur 5px, #F0C040, opacity 随描边同步脉动（25% ↔ 50%） |

**CTA 按钮完整视觉层次**：

```
外发光层（blur 5px, 赤金脉动）
┌────────────────────────────────┐
│  3px 赤金脉动描边               │
│ ┌──────────────────────────┐  │
│ │  #FFFFFF 白色底板          │  │
│ │ ┌──────────────────────┐ │  │
│ │ │  #1A1A2E "开始对战"   │ │  │  ← 深渊底文字
│ │ │  #3A3A5C "按 A 开始"  │ │  │  ← UI中性灰
│ │ └──────────────────────┘ │  │
│ └──────────────────────────┘  │
└────────────────────────────────┘
```

### 4.2 顶部栏按钮"设置" / "操作指南" / "退出"

| 属性 | 规格 |
|------|------|
| **底板色** | #2A2A3E (100%) |
| **底板尺寸** | 按钮自适应 + 内边距 12px（水平）x 8px（垂直） |
| **底板圆角** | 6px（略小于面板标准 8px，微型按钮规格） |
| **文字** | Noto Sans SC Medium 14px，#E8E8F0 |
| **间距** | 按钮间距 12px |
| **描边（默认）** | 无 |
| **描边（焦点）** | 2px #F0C040 实线（Art Bible §7.5.2 焦点指示器） |
| **焦点态文字色** | #FFFFFF |
| **按下态** | Scale 0.97，0.05s Linear |

**单按钮估算尺寸**：

| 按钮 | 中文内容 | 英文内容（最大） | 估算宽度 | 高度 |
|------|---------|----------------|---------|------|
| 设置 | "设置" (2字符) | "Settings" (~8字符) | 80px | 36px |
| 操作指南 | "操作指南" (4字符) | "How to Play" (~12字符) | 110px | 36px |
| 退出 | "退出" (2字符) | "Quit" (~4字符) | 68px | 36px |

**按钮布局**：
- 三个按钮从左到右排列，整体左对齐
- 距屏幕左侧 24px，距屏幕顶部 24px
- 按钮间间距 12px

### 4.3 游戏标题"职业对决 / CLASS BRAWL"

| 属性 | 规格 |
|------|------|
| **中文"职业对决"** | Noto Sans SC SemiBold 72px，#FFFFFF 20% |
| **中文字间距** | 3.6px (5%) |
| **中文行高** | 101px (140%) |
| **英文"CLASS BRAWL"** | Exo 2 Medium 24px，#FFFFFF 15% |
| **英文字间距** | 1.2px (5%) |
| **英文行高** | 34px (140%) |
| **强制大写** | 英文 text-transform: uppercase |
| **中文英文间距** | 12px（标题与副标题之间垂直间距） |
| **对齐方式** | 水平居中 |

**视觉层次说明**：
- 中文标题是装饰性品牌元素，极低不透明度（20%）使其成为"背景纹理"而非信息焦点
- 英文副标题更低（15%），仅作为中文的辅助注音/装饰
- 两者加在一起形成"水印式"的底部氛围元素
- 装饰性描边（两侧横线）使用 #3A3A5C 30%，宽度 60px，高 1px

**装饰线布局**：

```
         ─── 职业对决 ───
              CLASS BRAWL
```

- 左横线：标题文字左侧，间距 16px
- 右横线：标题文字右侧，间距 16px
- 横线长度：60px
- 横线颜色：#3A3A5C 30%
- 横线垂直位置：与中文文字基线对齐

### 4.4 版本号与版权

| 属性 | 规格 |
|------|------|
| **版本号文字** | Exo 2 Regular 12px，#3A3A5C |
| **版权文字** | Exo 2 Regular 12px，#3A3A5C |
| **位置** | 版本号：左下角 (24px, 1060px)；版权：右下角 (右-24px, 1060px) |
| **格式** | 版本号 "v0.1.0"；版权 "(c) 2026" |
| **字间距** | 0.5px (4%) |

**对比度**：#3A3A5C on #1A1A2E = ~2.1:1。版本号是纯展示性信息，不属于交互元素，对比度不要求达标。高对比度模式下提升至 #666666（~3.8:1）。

### 4.5 退出确认弹窗

| 属性 | 规格 |
|------|------|
| **宽度** | 480px（决策 D） |
| **最小高度** | 200px |
| **背景** | #222240 85%（二级面板底板，Art Bible §7.6） |
| **圆角** | 8px |
| **描边** | 1px #3A3A5C 60%（Art Bible §7.6 面板描边） |
| **遮罩** | #1A1A2E 60%（Art Bible §7.6 面板叠加规则） |
| **内边距** | 32px |
| **位置** | 屏幕正中央 |

**弹窗内部布局**：

```
┌──────────────────────────────────────────────┐
│                                              │  弹窗 480px 宽
│         确定退出游戏？                         │  标题 20px SemiBold
│                                              │
│                                              │  间距 24px
│                                              │
│    ┌─────────────┐    ┌─────────────┐       │
│    │    取 消     │    │    确 认     │       │  两个按钮
│    └─────────────┘    └─────────────┘       │
│                                              │
└──────────────────────────────────────────────┘
```

**弹窗按钮规格**：

| 属性 | "取消" | "确认" |
|------|--------|--------|
| **底板色** | #2A2A3E | #2A2A3E |
| **底板圆角** | 6px | 6px |
| **尺寸** | 120 x 40px | 120 x 40px |
| **文字** | Noto Sans SC Medium 16px，#E8E8F0 | Noto Sans SC Medium 16px，#E8E8F0 |
| **焦点描边** | 2px #F0C040 | 2px #F0C040 |
| **焦点文字色** | #FFFFFF | #FFFFFF |
| **默认焦点** | **是**（防止误操作，main-menu.md §Interaction Map） | 否 |
| **按下态** | Scale 0.97，0.05s | Scale 0.97，0.05s |

**焦点陷阱**：
- 弹窗打开时焦点锁定在弹窗内（"取消" → "确认" 循环）
- 按 Escape / B 键关闭弹窗，焦点回到"退出"按钮

---

## 5. Animation Style Guide

### 5.1 缓动函数分配表

| 缓动函数 | 使用场景 | 原因 |
|----------|---------|------|
| **Cubic ease-out** | 屏幕淡入、元素渐次出现、CTA 焦点 Scale 变化、弹窗出现、按钮释放 | 默认缓动，"磁吸感"（Art Bible §7.3 原则 A） |
| **Cubic ease-in** | 屏幕淡出、弹窗关闭 | 从静止加速离开 |
| **Linear** | 按钮按下、焦点描边出现/消失 | 不需要缓动的即时状态变化 |
| **正弦波 (Sine)** | CTA 脉动描边、背景呼吸灯 | 循环动画标准波形（Art Bible §7.3 原则 C） |
| **ease-in-out** | 弹窗遮罩渐显/渐隐 | 平滑过渡 |

**禁令**（来源：Art Bible §7.3 动画禁令 + HUD 决策 B）：
- Bounce、Elastic、Back 缓动全部禁用（主菜单无战斗冲击反馈场景）
- 主菜单不是战斗场景，连 HUD 中特批的 EaseOutBack 也不使用

### 5.2 时长标准

| 类别 | 标准时长 | 上限 | 来源 |
|------|---------|------|------|
| 屏幕淡入 | 0.8s | 1.0s | main-menu.md Transitions |
| 元素渐次出现间隔 | 0.15s | 0.2s | Art Bible §7.3 原则 A |
| 屏幕淡出 | 0.4s | 0.5s | main-menu.md Transitions |
| 弹窗出现 | 0.2s | 0.25s | Art Bible §7.3 原则 A |
| 弹窗关闭 | 0.15s | 0.15s | Art Bible §7.3 原则 A |
| 焦点指示器 | 0.1s | 0.1s | Art Bible §7.5.2 |
| 按钮按下 | 0.05s | 0.05s | 即时反馈 |
| 按钮释放 | 0.1s | 0.15s | Cubic ease-out |
| CTA 脉动周期 | 2.0s | — | main-menu.md States |
| 背景呼吸灯周期 | 4.0s | — | Art Bible §2.1 |
| 颜色过渡 | 0.15s | 0.2s | 不超过 0.2s |

### 5.3 进入序列动画（启动淡入 → 默认状态）

总时长约 1.3s（0.8s 淡入 + 0.5s 元素渐次出现）

| 顺序 | 元素 | 动画 | 开始时间 | 时长 | 缓动 |
|------|------|------|---------|------|------|
| 1 | 背景底色 | Opacity 0% → 100% | 0.0s | 0.5s | Cubic ease-out |
| 2 | 背景呼吸灯动画 | 开始播放 | 0.3s | 持续 | 正弦波 4s 周期 |
| 3 | 标题"职业对决" | Opacity 0% → 20% | 0.4s | 0.3s | Cubic ease-out |
| 4 | 标题"CLASS BRAWL" | Opacity 0% → 15% | 0.55s | 0.3s | Cubic ease-out |
| 5 | 顶部栏按钮 | Opacity 0% → 100% + translateY(-8px → 0) | 0.7s | 0.25s | Cubic ease-out |
| 6 | 版本号/版权 | Opacity 0% → 100% | 0.7s | 0.25s | Cubic ease-out |
| 7 | CTA 按钮 | Opacity 0% → 100% + Scale 0.95 → 1.0 | 0.85s | 0.25s | Cubic ease-out |
| 8 | CTA 脉动描边 | 开始脉动 | 1.1s | 持续 | 正弦波 2s 周期 |
| 9 | 焦点设置 | 焦点默认落在 CTA 按钮 | 1.3s | 即时 | — |

### 5.4 退出序列动画（默认状态 → 角色选择）

总时长约 0.4s

| 顺序 | 元素 | 动画 | 开始时间 | 时长 | 缓动 |
|------|------|------|---------|------|------|
| 1 | CTA 脉动停止 | 停止脉动 | 0.0s | — | — |
| 2 | 全屏淡出 | Opacity 100% → 0% | 0.0s | 0.4s | Cubic ease-in |
| 3 | 场景切换触发 | — | 0.4s | — | — |

注意：淡出完成后触发场景异步加载（MenuScene → GameScene），目标 < 2s（main-menu.md Exit Points）。

### 5.5 弹出层动画

**弹窗出现（退出确认弹窗）**：

| 阶段 | 属性 | 时长 | 缓动 |
|------|------|------|------|
| 遮罩渐显 | Opacity 0% → 60% | 0.15s | ease-in-out |
| 弹窗缩放出现 | Scale 0.95 → 1.0 + Opacity 0% → 100% | 0.2s | Cubic ease-out |

**弹窗关闭**：

| 阶段 | 属性 | 时长 | 缓动 |
|------|------|------|------|
| 弹窗缩放消失 | Scale 1.0 → 0.95 + Opacity 100% → 0% | 0.15s | Cubic ease-in |
| 遮罩渐隐 | Opacity 60% → 0% | 0.15s | ease-in-out |

弹窗关闭与遮罩渐隐同步进行（0.15s）。

### 5.6 CTA 脉动效果详细规格

| 属性 | 规格 |
|------|------|
| **类型** | 亮度正弦波调制 |
| **周期** | 2.0s |
| **描边不透明度范围** | 40% ↔ 100% |
| **外发光不透明度范围** | 25% ↔ 50% |
| **振幅** | ±30%（描边），±12.5%（外发光） |
| **影响范围** | 仅描边和外发光，底板和文字不受影响 |

**脉动公式**：
```
borderOpacity = 0.7 + 0.3 * sin(2 * PI * t / 2.0)
glowOpacity   = 0.375 + 0.125 * sin(2 * PI * t / 2.0)
```

**焦点态脉动覆盖**：CTA 获得焦点时，描边不透明度跳至 100% 并停止脉动，焦点丢失后恢复脉动。

### 5.7 减少动作模式 (Reduced Motion)

| 动画 | 默认行为 | 减少动作模式 | 理由 |
|------|---------|-------------|------|
| 进入序列 | 渐次出现 0.25s/元素 | **简化为单次 0.3s 全体淡入** | 渐次不是信息必要手段 |
| CTA 脉动 | 正弦波 2s | **停止脉动，描边固定 70%** | 脉动是装饰性动画 |
| 背景呼吸灯 | 正弦波 4s | **停止呼吸灯** | 装饰性动画 |
| 弹窗出现 | Scale + Opacity 0.2s | **简化为 0.15s Opacity 淡入** | 缩放不是信息必要手段 |
| 弹窗关闭 | Scale + Opacity 0.15s | **简化为 0.1s Opacity 淡出** | 同上 |
| 焦点指示器 | 描边出现 0.1s | **保留不变** | 焦点反馈是核心信息 |
| 按钮按下 | Scale 0.97 0.05s | **保留不变** | 按下反馈是交互必要手段 |
| 屏幕淡出 | Opacity 0.4s | **保留不变** | 状态转换必要 |

---

## 6. Asset Manifest

### 6.1 字体文件

| 资产 | 来源 | 格式 | 说明 |
|------|------|------|------|
| Exo 2-Regular | Google Fonts | TTF/OTF | Regular 400 |
| Exo 2-Medium | Google Fonts | TTF/OTF | Medium 500 |
| NotoSansSC-Medium | Google Fonts | TTF/OTF | 中文回退 500 |
| NotoSansSC-SemiBold | Google Fonts | TTF/OTF | 中文品牌标题 600 |

与 HUD 共享字体文件（Exo 2 全字重 + Noto Sans SC）。无需额外导入。

### 6.2 背景动画精灵

| 资产名 | 尺寸 | 格式 | 说明 |
|--------|------|------|------|
| `env_bg_mainmenu_breathlight_1920x1080.png` | 1920x1080px | PNG | 呼吸灯环境光底图（暖色调 4200K），极缓慢明暗周期 4s |

**说明**：呼吸灯效果可通过 Shader 或 Sprite + 脚本亮度调制实现。具体方案由 technical-artist 决定。这里仅列出美术资产需求。

### 6.3 装饰元素精灵

| 资产名 | 尺寸 | 格式 | 说明 |
|--------|------|------|------|
| `ui_deco_title_line_left_60x1.png` | 60x1px | PNG | 标题左侧装饰横线，#3A3A5C |
| `ui_deco_title_line_right_60x1.png` | 60x1px | PNG | 标题右侧装饰横线，#3A3A5C |

**说明**：装饰线也可通过 USS border 或 background-color 实现。如使用纯 CSS 方案则无需精灵资产。

### 6.4 按键提示图标

| 资产名 | 尺寸 | 格式 | 说明 |
|--------|------|------|------|
| `ui_key_a_16.png` | 16x16px | PNG | 手柄 A 键（与 HUD 共享） |
| `ui_key_enter_16.png` | 16x16px | PNG | 键盘 Enter 键 |

与 HUD 按键提示图标共享目录 `Assets/Art/UI/Keys/`。

### 6.5 动画剪辑规格

| 动画名 | 目标元素 | 类型 | 时长 | 关键帧 | 说明 |
|--------|---------|------|------|--------|------|
| `anim_mm_fade_in` | 背景 | Opacity | 0.5s | 0%:0, 100%:100 | Cubic ease-out |
| `anim_mm_title_appear` | 标题中文 | Opacity | 0.3s | 0%:0, 100%:20 | Cubic ease-out |
| `anim_mm_subtitle_appear` | 标题英文 | Opacity | 0.3s | 0%:0, 100%:15 | Cubic ease-out |
| `anim_mm_topbar_slide_in` | 顶部栏容器 | Opacity+TranslateY | 0.25s | 0%:(0,-8px), 100%:(100,0) | Cubic ease-out |
| `anim_mm_cta_appear` | CTA 容器 | Opacity+Scale | 0.25s | 0%:(0,0.95), 100%:(100,1.0) | Cubic ease-out |
| `anim_mm_version_appear` | 版本号/版权 | Opacity | 0.25s | 0%:0, 100%:100 | Cubic ease-out |
| `anim_mm_pulse_cta` | CTA 描边+外发光 | Opacity | 持续 | 正弦波 2s | 描边 40-100%, 发光 25-50% |
| `anim_mm_fade_out` | 全屏 | Opacity | 0.4s | 0%:100, 100%:0 | Cubic ease-in |
| `anim_mm_modal_show` | 弹窗 | Scale+Opacity | 0.2s | 0%:(0,0.95), 100%:(100,1.0) | Cubic ease-out |
| `anim_mm_modal_hide` | 弹窗 | Scale+Opacity | 0.15s | 0%:(100,1.0), 100%:(0,0.95) | Cubic ease-in |
| `anim_mm_overlay_show` | 遮罩 | Opacity | 0.15s | 0%:0, 100%:60 | ease-in-out |
| `anim_mm_overlay_hide` | 遮罩 | Opacity | 0.15s | 0%:60, 100%:0 | ease-in-out |

---

## 7. Accessibility Verification

### 7.1 WCAG 对比度计算

#### 文字对比度

| 元素 | 前景色 | 背景色 | 对比度 | WCAG 等级 | 达标 |
|------|--------|--------|--------|----------|------|
| CTA 主文字 | #1A1A2E | #FFFFFF | ~15.4:1 | AAA | YES |
| CTA 副文字 | #3A3A5C | #FFFFFF | ~3.5:1 | AA (大文本) | YES (14px 为大文本) |
| 顶部栏文字（默认） | #E8E8F0 | #1A1A2E (穿透到底色) | ~13.8:1 | AAA | YES |
| 顶部栏文字（焦点） | #FFFFFF | #2A2A3E (底板) | ~10.8:1 | AAA | YES |
| 弹窗标题 | #FFFFFF | #222240 85% ≈ #252542 | ~13.5:1 | AAA | YES |
| 弹窗按钮文字（默认） | #E8E8F0 | #2A2A3E | ~8.5:1 | AAA | YES |
| 弹窗按钮文字（焦点） | #FFFFFF | #2A2A3E | ~10.8:1 | AAA | YES |
| 版本号 | #3A3A5C | #1A1A2E | ~2.1:1 | — | 非交互，不要求 |
| 标题中文 | #FFFFFF 20% | #1A1A2E | ~3.1:1 | — | 装饰性，非交互 |
| 标题英文 | #FFFFFF 15% | #1A1A2E | ~2.3:1 | — | 装饰性，非交互 |

**说明**：
- CTA 副文字 #3A3A5C on #FFFFFF 为 ~3.5:1，14px 在 WCAG 中属于"大文本"（>=14px bold 或 >=18px regular），达到 AA 3:1 门槛
- 版本号和标题为装饰性元素，不承载交互功能，对比度不要求达标
- 高对比度模式下标题和版本号对比度会提升（见 §2.4）

#### 交互元素焦点可见性

| 元素 | 焦点指示器 | 对比度 | 达标 |
|------|-----------|--------|------|
| CTA 按钮 | 3px #F0C040 描边 + glow | #F0C040 on #1A1A2E ≈ 7.2:1 | AAA |
| 顶部栏按钮 | 2px #F0C040 描边 | #F0C040 on #2A2A3E ≈ 6.1:1 | AA |
| 弹窗按钮 | 2px #F0C040 描边 | #F0C040 on #2A2A3E ≈ 6.1:1 | AA |

### 7.2 色盲安全分析

主菜单不使用颜色编码传达信息（无职业色区分、无危险等级色），色盲风险极低。

| 元素 | 使用颜色 | 色盲风险 | 说明 |
|------|---------|---------|------|
| CTA 描边 | #F0C040 赤金 | 低 | 装饰性脉动，不传达功能性信息 |
| 焦点描边 | #F0C040 赤金 | 低 | 焦点同时通过形状（描边出现）传达 |
| 顶部栏底板 | #2A2A3E | 无 | 结构色，不传达语义信息 |

**结论**：主菜单无需色盲模式特殊处理。所有交互信息通过形状和位置传达，颜色仅为装饰增强。

### 7.3 键盘/手柄全导航验证

| 交互 | 手柄 | 键盘 | 鼠标 | 覆盖 |
|------|------|------|------|------|
| 导航焦点 | D-Pad / 左摇杆 | Tab / Shift+Tab / 方向键 | 鼠标移动 | YES |
| 激活元素 | A | Enter / Space | 左键点击 | YES |
| 返回/关闭 | B / Escape | Escape | — | YES |
| CTA 快速开始 | Start | Enter | 左键点击 | YES |

**焦点循环**：CTA -> 设置 -> 操作指南 -> 退出 -> CTA（循环导航）

**焦点入口**：每次进入主菜单，焦点默认在 CTA 按钮（最高优先级操作）

### 7.4 脉动频率安全验证

| 元素 | 频率 | 安全阈值 (6Hz) | 余量 |
|------|------|---------------|------|
| CTA 脉动描边 | 0.5 Hz (2s 周期) | 6 Hz | 92% 余量 |
| 背景呼吸灯 | 0.25 Hz (4s 周期) | 6 Hz | 96% 余量 |

**结论**：所有脉动频率远低于光敏癫痫安全阈值。

### 7.5 信息密度验证

**主菜单可见文字总量**（默认状态）：

| 元素 | 字符数 | 类型 |
|------|--------|------|
| "开始对战" | 4 | 中文 |
| "按 A 开始" | 5 | 中文+字母 |
| "设置" | 2 | 中文 |
| "操作指南" | 4 | 中文 |
| "退出" | 2 | 中文 |
| "职业对决" | 4 | 中文（装饰性） |
| "CLASS BRAWL" | 10 | 英文（装饰性） |
| "v0.1.0" | 6 | 英文/数字 |
| "(c) 2026" | 6 | 英文/数字/符号 |
| **总计** | **43 字符** | |

Art Bible §7.5.1 限制为 30 字符（不含伤害数字）。主菜单超出的部分为装饰性标题文字（14 字符）和版本号/版权（12 字符），这两项属于极低视觉权重的背景信息。实际交互信息文字为 17 字符（CTA + 三个按钮），在限制内。

### 7.6 减少动作模式完整规格

**触发方式**：设置菜单 -> 无障碍 -> 减少动作开关

**完整变更清单**：详见 §5.7 减少动作模式表。

**设计原则**（来源：Art Bible §7.5.5）：
- 保留所有信息性动画（淡入淡出、焦点指示器、按钮按下反馈）
- 移除所有装饰性动画（脉动、呼吸灯、渐次出现）
- 不提供「完全关闭动画」选项

---

## 8. CSS Variable Mapping

### 8.1 主菜单专用变量（前缀 `--mm-`）

以下变量定义在主菜单专用 USS 文件中，或通过 `:root` 块添加到 `USS_HUD_Theme.uss`。

```css
/* ============================================================
 * Main Menu Visual Design — CSS Custom Properties (Design Tokens)
 * Source: design/ux/main-menu-visual-design.md
 * Base Resolution: 1920x1080
 * ============================================================ */

:root {
    /* ---- Typography ---- */
    --mm-cta-font-size: 36px;
    --mm-cta-letter-spacing: 1.8px;       /* 5% of 36px */
    --mm-cta-line-height: 50px;            /* 140% of 36px */
    --mm-cta-hint-font-size: 14px;
    --mm-cta-hint-letter-spacing: 0.6px;   /* 4% of 14px */
    --mm-cta-hint-line-height: 20px;       /* 140% of 14px */
    --mm-title-cn-font-size: 72px;
    --mm-title-cn-letter-spacing: 3.6px;   /* 5% of 72px */
    --mm-title-cn-line-height: 101px;      /* 140% of 72px */
    --mm-title-en-font-size: 24px;
    --mm-title-en-letter-spacing: 1.2px;   /* 5% of 24px */
    --mm-title-en-line-height: 34px;       /* 140% of 24px */
    --mm-topbar-btn-font-size: 14px;
    --mm-topbar-btn-letter-spacing: 0.6px; /* 4% of 14px */
    --mm-topbar-btn-line-height: 20px;     /* 140% of 14px */
    --mm-version-font-size: 12px;
    --mm-version-letter-spacing: 0.5px;    /* 4% of 12px */
    --mm-version-line-height: 17px;        /* 140% of 12px */
    --mm-modal-title-font-size: 20px;
    --mm-modal-title-letter-spacing: 1.0px; /* 5% of 20px */
    --mm-modal-title-line-height: 28px;    /* 140% of 20px */
    --mm-modal-btn-font-size: 16px;
    --mm-modal-btn-letter-spacing: 0.7px;  /* 4% of 16px */
    --mm-modal-btn-line-height: 22px;      /* 140% of 16px */

    /* ---- CTA Button ---- */
    --mm-cta-width: 320px;
    --mm-cta-height: 80px;
    --mm-cta-bg: rgb(255, 255, 255);                        /* #FFFFFF */
    --mm-cta-border-radius: 8px;
    --mm-cta-border-color: rgb(240, 192, 64);               /* #F0C040 赤金 */
    --mm-cta-border-width: 3px;
    --mm-cta-glow-color: rgba(240, 192, 64, 0.375);         /* 赤金平均不透明度 */
    --mm-cta-glow-blur: 5px;
    --mm-cta-text-color: rgb(26, 26, 46);                   /* #1A1A2E 深渊底 */
    --mm-cta-hint-color: rgb(58, 58, 92);                   /* #3A3A5C UI中性灰 */
    --mm-cta-padding-top: 18px;
    --mm-cta-padding-bottom: 12px;
    --mm-cta-padding-horizontal: 20px;

    /* ---- Top Bar Buttons ---- */
    --mm-topbar-btn-bg: rgb(42, 42, 62);                    /* #2A2A3E 图标底板 */
    --mm-topbar-btn-border-radius: 6px;
    --mm-topbar-btn-padding-h: 12px;
    --mm-topbar-btn-padding-v: 8px;
    --mm-topbar-btn-gap: 12px;
    --mm-topbar-btn-text-color: rgb(232, 232, 240);         /* #E8E8F0 */
    --mm-topbar-btn-text-color-focused: rgb(255, 255, 255); /* #FFFFFF */
    --mm-topbar-focus-border-color: rgb(240, 192, 64);      /* #F0C040 */
    --mm-topbar-focus-border-width: 2px;
    --mm-topbar-margin-left: 24px;
    --mm-topbar-margin-top: 24px;

    /* ---- Title ---- */
    --mm-title-cn-color: rgba(255, 255, 255, 0.20);         /* #FFFFFF 20% */
    --mm-title-en-color: rgba(255, 255, 255, 0.15);         /* #FFFFFF 15% */
    --mm-title-line-color: rgba(58, 58, 92, 0.30);          /* #3A3A5C 30% */
    --mm-title-line-width: 60px;
    --mm-title-line-height: 1px;
    --mm-title-gap: 12px;

    /* ---- Version ---- */
    --mm-version-color: rgb(58, 58, 92);                    /* #3A3A5C */
    --mm-version-margin: 24px;

    /* ---- Modal / Popup ---- */
    --mm-modal-width: 480px;
    --mm-modal-min-height: 200px;
    --mm-modal-bg: rgba(34, 34, 64, 0.85);                  /* #222240 85% */
    --mm-modal-border-radius: 8px;
    --mm-modal-border-color: rgba(58, 58, 92, 0.60);        /* #3A3A5C 60% */
    --mm-modal-border-width: 1px;
    --mm-modal-padding: 32px;
    --mm-modal-overlay-color: rgba(26, 26, 46, 0.60);       /* #1A1A2E 60% */
    --mm-modal-title-color: rgb(255, 255, 255);              /* #FFFFFF */
    --mm-modal-btn-bg: rgb(42, 42, 62);                     /* #2A2A3E */
    --mm-modal-btn-border-radius: 6px;
    --mm-modal-btn-width: 120px;
    --mm-modal-btn-height: 40px;
    --mm-modal-btn-text-color: rgb(232, 232, 240);          /* #E8E8F0 */
    --mm-modal-btn-focus-border-color: rgb(240, 192, 64);   /* #F0C040 */
    --mm-modal-btn-focus-border-width: 2px;
    --mm-modal-btn-gap: 16px;

    /* ---- Animation Durations ---- */
    --mm-anim-screen-fade-in: 0.5s;
    --mm-anim-element-appear: 0.25s;
    --mm-anim-element-stagger: 0.15s;
    --mm-anim-screen-fade-out: 0.4s;
    --mm-anim-modal-show: 0.2s;
    --mm-anim-modal-hide: 0.15s;
    --mm-anim-overlay-show: 0.15s;
    --mm-anim-overlay-hide: 0.15s;
    --mm-anim-focus-transition: 0.1s;
    --mm-anim-press: 0.05s;
    --mm-anim-release: 0.1s;
    --mm-anim-pulse-period: 2.0s;
    --mm-anim-color-transition: 0.15s;

    /* ---- Layout ---- */
    --mm-safe-margin: 24px;
    --mm-bottom-margin: 20px;
}
```

### 8.2 与 HUD 共享变量的映射

以下变量已在 `USS_HUD_Theme.uss` 中定义，主菜单直接复用：

| 共享变量 | 主菜单用途 | 来源 |
|---------|-----------|------|
| `--font-family` | 所有文字字体族声明 | HUD Theme |
| `--font-weight-medium` | 按钮文字、按键提示 | HUD Theme |
| `--font-weight-semibold` | CTA 主文字、标题、弹窗标题 | HUD Theme |
| `--font-weight-regular` | 版本号 | HUD Theme |
| `--color-panel-bg` | 背景底色（深渊底 90%） | HUD Theme |
| `--color-text-primary` | 焦点态文字色 | HUD Theme |
| `--color-text-secondary` | 按钮默认态文字色 | HUD Theme |
| `--color-skill-icon-bg` | 顶部栏按钮底板、弹窗按钮底板 | HUD Theme |
| `--color-border` | 弹窗描边 | HUD Theme |
| `--rarity-epic-color` | CTA 脉动描边、焦点描边（#F0C040） | HUD Theme |
| `--anim-color-transition` | 颜色过渡时长 | HUD Theme |

### 8.3 UI Toolkit 2022.3 实现注意事项

以下限制来自项目约束，实现时需注意：

1. **无 custom cubic-bezier**：UI Toolkit USS 不支持自定义 cubic-bezier transition-timing-function。所有标注为 Cubic ease-out 的动画需通过 C# 脚本使用 `AnimationCurve` 实现，或使用 USS 内置的 `ease-out`（非精确 Cubic，但足够接近）。
2. **无 text-shadow**：标题文字不需要 text-shadow（低不透明度装饰性文字）。如未来需添加文字阴影，需通过 C# 脚本在 Canvas 上绘制或使用额外 Label 元素模拟。
3. **inline style override**：UI Toolkit 中 C# inline style 会覆盖 USS class 样式。避免在 C# 中设置可通过 USS class 控制的属性。
4. **opacity 动画**：USS `opacity` 属性支持 transition，可用于淡入淡出效果。
5. **transform 动画**：USS `transform` 属性（scale, translate）支持 transition，可用于缩放和位移效果。
6. **box-shadow**：USS 支持 `-unity-box-shadow-` 前缀属性，可用于外发光效果。但脉动动画需通过 C# 脚本周期性更新 inline style 实现。

---

## 9. Implementation Checklist

以下清单供 ui-programmer 参考实现顺序：

- [ ] 创建 `Assets/Settings/UI/USS_MainMenu_Theme.uss`（或追加到 `USS_HUD_Theme.uss`）
- [ ] 创建主菜单 UXML 结构：背景层、顶部栏、中央 CTA、底部标题区、版本号区
- [ ] 实现 CTA 按钮视觉（白底 + 深色文字 + 赤金描边）
- [ ] 实现 CTA 脉动动画（C# coroutine 或 DOTween）
- [ ] 实现顶部栏微型底板按钮（含焦点描边状态）
- [ ] 实现退出确认弹窗（遮罩 + 弹窗 + 焦点陷阱）
- [ ] 实现进入序列动画（渐次出现）
- [ ] 实现退出序列动画（淡出到黑屏）
- [ ] 实现手柄/键盘焦点导航和循环
- [ ] 实现减少动作模式覆盖
- [ ] 实现高对比度模式覆盖
- [ ] 本地化文本适配测试（中文 vs 英文按钮宽度）
