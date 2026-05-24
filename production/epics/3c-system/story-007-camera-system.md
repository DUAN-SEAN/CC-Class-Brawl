# Story 007: 摄像机系统 — 多人跟踪、动态缩放、边界钳制

> **Epic**: 3C系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/3c-system.md`
**Requirement**: TR-MOV-045 ~ TR-MOV-050 (摄像机相关)
**ADR Governing Implementation**: ADR-0012: Camera Strategy — Custom Orthographic Camera with Bounding Box Tracking
**ADR Decision Summary**: 自定义 CameraController MonoBehaviour, LateUpdate 驱动, 包围盒计算, SmoothDamp 插值, 场地边界 ClampToArenaBounds。不使用 Cinemachine。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: 摄像机在 LateUpdate 中更新 (非 FixedUpdate), 确保读取到最新角色位置。

**Control Manifest Rules (Foundation)**:
- Required: CameraFormulas 为纯静态类, 100% 可单元测试
- Required: CameraTuningData SO 数据驱动
- Required: LateUpdate 标准模式, 不依赖物理同步细节
- Guardrail: CameraController.LateUpdate 帧耗时 < 0.02ms

---

## Acceptance Criteria

- [ ] 摄像机持续跟踪所有在场玩家位置的包围盒中心
- [ ] 动态 Orthographic Size 计算: RequiredHalfWidth + RequiredHalfHeight -> TargetOrthoSize
- [ ] Size 钳制在 [MinCamSize=4.2, MaxCamSize=8.0] 范围内
- [ ] 摄像机平滑插值: SmoothDamp (指数衰减, 帧率无关), CameraSmoothSpeed=5.0
- [ ] 场地边界限制: 摄像机视锥不超出 CameraBounds
- [ ] 所有玩家重合 -> OrthoSize = MinCamSize, 中心 = 该点
- [ ] 单角色存活 -> OrthoSize = MinCamSize, 跟随该角色
- [ ] 玩家间距超出 MaxCamSize -> 停在 MaxCamSize
- [ ] KO 缩放效果接口: ICameraEffectProvider.TriggerKOZoom(intensity, duration)
- [ ] CameraTuningData SO: MinCamSize, MaxCamSize, CamPaddingX, CamPaddingY, CameraSmoothSpeed
- [ ] CameraFormulas 为纯静态类, ComputeTargetOrthoSize 和 ClampToArenaBounds 可单元测试

---

## Implementation Notes

**来自 ADR-0012 的具体指导**:

1. CameraController 在 LateUpdate 中执行 (非 FixedUpdate)
2. 包围盒计算: Bounds.Encapsulate() 遍历所有角色 Transform
3. 正交尺寸公式 (CameraFormulas — 纯静态类):
```
RequiredHalfWidth = (PlayerSpreadX * 0.5 + CamPaddingX) / AspectRatio
RequiredHalfHeight = PlayerSpreadY * 0.5 + CamPaddingY
TargetOrthoSize = Max(RequiredHalfHeight, RequiredHalfWidth)
```

4. SmoothDamp (帧率无关): `Mathf.Lerp(current, target, 1 - Exp(-speed * dt))`

5. 边界钳制 (CameraFormulas.ClampToArenaBounds):
   - 计算摄像机视锥半宽/半高
   - Clamp 摄像机位置使得视锥边缘不超出 arenaBounds
   - 场地过小时居中显示

6. CameraTuningData SO 用于 Inspector 配置, 默认值:
   - MinCamSize = 4.2, MaxCamSize = 8.0
   - CamPaddingX = 3.0, CamPaddingY = 2.0
   - CameraSmoothSpeed = 5.0

7. 接口:
```csharp
public interface ICameraDataProvider
{
    Vector3 GetCameraPosition();
    float GetOrthographicSize();
    float GetHalfWidth();
    float GetHalfHeight();
}

public interface ICameraEffectProvider
{
    void TriggerKOZoom(float intensity, float duration);
}
```

---

## Out of Scope

- 方向指示箭头渲染 (属于 battle-hud epic)
- 屏幕边缘红色渐变警告效果 (属于 battle-hud epic)
- 击飞 KO 摄像机效果的视觉设计 (由 match-management 调用 TriggerKOZoom)

---

## QA Test Cases

- **AC-2 (动态缩放)**:
  - Given: 2 个玩家在场, 距离较近 (spread < 3u)
  - When: 摄像机更新
  - Then: OrthoSize = MinCamSize (4.2u)

- **AC-2 (玩家拉开距离)**:
  - Given: 2 个玩家在场, 距离拉开
  - When: 摄像机更新
  - Then: OrthoSize 平滑增大, 不超过 MaxCamSize (8.0u)

- **AC-5 (边界钳制)**:
  - Given: 摄像机位置计算完成
  - When: 检查视锥边缘
  - Then: 不显示超出场地边界的区域

- **AC-6 (所有玩家重合)**:
  - Given: 2 个玩家在同一位置
  - Then: OrthoSize = MinCamSize, 中心 = 该位置

- **AC-7 (单角色存活)**:
  - Given: 仅 1 个角色在场
  - Then: OrthoSize = MinCamSize, 跟随该角色

- **AC-10 (CameraFormulas 单元测试)**:
  - Given: CameraFormulas.ComputeTargetOrthoSize
  - When: 输入典型值 (spread=6, padding=3, aspect=1.78)
  - Then: 返回正确计算结果 (TargetOrthoSize 约 4.69)

---

## Test Evidence

**Story Type**: Logic (CameraFormulas 可单元测试)
**Required evidence**: `tests/unit/movement/camera-formulas_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: arena-platform epic (IArenaDataProvider.GetCameraBounds() 提供场地边界数据)
- Unlocks: battle-hud epic (ICameraDataProvider 供 HUD 查询)
