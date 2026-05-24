# Story: Input Buffer

> **Epic**: combat-state-machine
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/combat-state-machine.md`
- **TR Range**: TR-CBT-004, TR-CBT-006, TR-CBT-016, TR-CBT-028, TR-CBT-029
- **Governing ADR**: ADR-0002 (Dual FSM Architecture), ADR-0005 (Input System)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现 8 帧输入缓冲区（InputBuffer）：环形缓冲区存储输入条目（类型 + 记录帧号），每帧检查缓冲中未消费的输入是否有效（BufferAge <= InputBufferFrames），按优先级解析（技能 > 攻击 > 闪避/跳跃），将有效输入传递给格斗状态机执行。

## Acceptance Criteria (from GDD)

- **GIVEN** InputBufferFrames = 8, **WHEN** 在可操作帧前 8 帧内按下攻击, **THEN** 输入被接受执行
- **GIVEN** InputBufferFrames = 8, **WHEN** 在可操作帧前 9 帧按下攻击, **THEN** 输入过期被丢弃
- **GIVEN** 缓冲中有攻击输入（BufferAge=3）且当前状态为 Attacking.Startup, **WHEN** 检查缓冲, **THEN** 输入保留在缓冲中，不执行也不丢弃
- **GIVEN** 缓冲中有攻击输入（BufferAge=3）且当前状态为 Attacking.Recovery（取消表允许）, **WHEN** 检查缓冲, **THEN** 输入执行并从缓冲中丢弃
- **GIVEN** BufferAge < 0（帧序号错误）, **WHEN** 检查缓冲, **THEN** 该输入被丢弃
- **GIVEN** 同一帧缓冲中有攻击输入和技能输入, **WHEN** 当前状态可接受输入, **THEN** 按优先级（技能 > 攻击）接受技能输入，攻击输入丢弃
- **GIVEN** InputBufferFrames = 0, **WHEN** 输入写入缓冲, **THEN** 无缓冲效果（立即过期）

## Implementation Notes (from ADR-0002, ADR-0005)

- 使用环形缓冲区（Circular Buffer），固定大小 8 条目
- `InputEntry` 结构: Type (InputType), RecordedFrame (int), Consumed (bool)
- `_head` 写入指针自动覆盖最旧数据
- BufferAge = CurrentFrame - InputRecordedFrame
- 有效条件: `BufferAge >= 0 && BufferAge <= InputBufferFrames && !Consumed`
- 执行条件: 有效 + 当前状态可接受该输入类型
- 丢弃条件: `BufferAge > InputBufferFrames` 或已执行
- InputReader 是唯一输入入口点，CombatFSM 通过 InputBuffer 消费
- BufferCapacity（8 条目）与 BufferWindowFrames（8 帧）是不同概念

## Out of Scope

- InputReader 和 PlayerInput 集成（Foundation 层）
- 取消表逻辑（Story 005）
- 技能输入映射（Feature 层 skill-equipment）

## Dependencies

- Story 001 (CombatFSM Core) must be DONE
- `FrameCounter` 全局帧号组件可用
- `IInputReader` 接口已定义

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: 8 帧内输入有效**
- Given: InputBuffer 容量 8，InputBufferFrames=8，当前帧 = 100
- When: Write(Attack, frame=100)，检查 frame=105
- Then: BufferAge=5, IsBufferValid=true

**Test: 超过 8 帧输入过期**
- Given: 当前帧 = 100，Write(Attack, frame=92)
- When: 检查 frame=100
- Then: BufferAge=8, Valid（边界值，等于窗口）
- When: 检查 frame=101
- Then: BufferAge=9, Invalid（过期）

**Test: BufferAge 为负丢弃**
- Given: 输入 RecordedFrame = 110
- When: 在 CurrentFrame = 100 检查
- Then: BufferAge = -10, 丢弃

**Test: 优先级解析**
- Given: 缓冲中有 Attack（age=3）和 Skill（age=3）
- When: 当前状态可接受输入
- Then: Skill 被接受执行，Attack 被丢弃

**Test: 状态不接受时保留**
- Given: 缓冲中有 Attack（age=2），当前在 Attacking.Startup
- When: 检查缓冲
- Then: 输入保留，不消费不丢弃

**Test: Ring buffer 覆盖**
- Given: 缓冲已满（8 条目）
- When: Write 第 9 个输入
- Then: 最旧条目被覆盖

## Test Evidence

- Automated unit tests: `tests/unit/combat/input_buffer_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/InputBuffer.cs` (new — ring buffer implementation)
