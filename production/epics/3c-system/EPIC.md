# Epic: 3C系统

> **Layer**: Foundation
> **GDD**: design/gdd/3c-system.md
> **Architecture Module**: 3C系统 (IMovementController)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories 3c-system`

## Overview

实现角色控制三大支柱：输入处理（Character）、移动/跳跃控制（Control）、摄像机跟随（Camera）。所有移动逻辑在 FixedUpdate 60Hz 中运行，使用手动重力而非 Unity 自动重力。输入使用 New Input System，支持多手柄本地多人。移动参数全部数据驱动，通过 MovementParams 注入。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Physics Timestep | 60Hz FixedTimestep + Manual Gravity + Rigidbody2D.velocity direct assignment | LOW |
| ADR-0002: Dual FSM Architecture | Movement FSM + Combat FSM 并行运行，互不阻塞 | LOW |
| ADR-0005: Input System | New Input System, per-player PlayerInput, input buffer 8 frames | LOW |
| ADR-0012: Camera Strategy | 动态正交尺寸 + 多人跟踪 + 场地边界钳制 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-MOV-001 ~ TR-MOV-052 | 52 | ADR-0001, 0002, 0005, 0012 |

Full requirement list: `docs/architecture/tr-registry.yaml` (MOV section)

## Existing Code

| File | Type |
|------|------|
| Foundation/Enums/MovementState.cs | Enum |
| Foundation/Enums/FacingDirection.cs | Enum |
| Core/Data/MovementParams.cs | Data struct |
| Foundation/Interfaces/IMovementController.cs | Interface |

**Status**: 数据结构和接口已定义，运行时 MonoBehaviour 实现待开发。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/3c-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories 3c-system` to break this epic into implementable stories.
