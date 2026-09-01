# 角色控制 Character

本文档记录当前已实现的业务侧脚本。项目处于早期阶段，角色控制相关的实验性代码位于两个文件。

## 1. 现状概述

| 文件 | 实际类名 | 状态 |
| --- | --- | --- |
| [Character/CharacterController.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Character/CharacterController.cs) | `CharacterCamera` | 不完整（实验性，`Start`/`Update` 空） |
| [Core/ElasticEffect.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/ElasticEffect.cs) | `qt` | 完整实现 Q 弹形变 + 角色移动 |

> ⚠️ 当前文件名与类名不一致，属实验性命名，后续应按 [开发约定](./Conventions.md) 重命名整理。

## 2. ElasticEffect（Q 弹形变 + 角色移动）

源码：[ElasticEffect.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/ElasticEffect.cs)

该脚本挂载到角色 GameObject 上，整合"果冻 Q 弹物理形变"与"基础角色移动"两套逻辑，是当前场景 `Level_01_CharacterController` 的核心表现脚本。

### 2.1 Inspector 配置项

```
──── Q弹效果 ────
  bounciness      0.1~1   Q弹强度（越大越Q），默认 0.5
  bounceSpeed     3~25    回弹速度，默认 10
  breathAmount    0~0.15  呼吸效果强度（0=关闭），默认 0.04
  breathSpeed     0.5~3   呼吸速度，默认 1.5

──── 角色移动 ────
  moveSpeed       1~20    移动速度，默认 8
  jumpForce       3~20    跳跃力度，默认 10
  moveWobble      0~0.3   移动惯性晃动强度，默认 0.1

──── 触发设置 ────
  landingThreshold 0~10   最小落地速度才触发压扁，默认 2
```

### 2.2 移动逻辑（Update）

- 读取 `Input.GetAxisRaw("Horizontal/Vertical")` 得到移动方向。
- 用 `Vector3.Lerp` 平滑过渡到目标速度，保留 Y 分量（重力/跳跃不受影响）。
- `Quaternion.Slerp` 平滑转向移动方向。
- 按下 `Space` 且 `IsGrounded()` 时 `AddForce(Up * jumpForce, Impulse)` 跳跃。

> 注意：此处直接使用 Unity 原生 `Input`，未走框架 `InputMgr`。后续若要支持按键重绑/手柄，应迁移到 `InputMgr + EventCenter`。

### 2.3 Q 弹物理（弹簧阻尼系统）

核心数据：

```csharp
private Vector3 squashVelocity;   // 形变速度
private Vector3 squashOffset;     // 当前形变偏移
private float stiffness;          // 刚度 = bounceSpeed²
private float damping;            // 阻尼 = 2 * bounceSpeed * 0.7
```

每帧 `UpdateSpringPhysics()`：

```
springForce  = -stiffness * squashOffset
dampingForce = -damping  * squashVelocity
squashVelocity += (springForce + dampingForce) * dt
squashOffset   += squashVelocity * dt
```

达到稳态（位移与速度均接近 0）时归零，避免抖动。

### 2.4 形变触发

- **落地触发**（`FixedUpdate`）：从空中到地面且垂直速度 > `landingThreshold` → `TriggerSquash(Up, intensity)`，强度与落地速度正相关。
- **碰撞触发**（`OnCollisionEnter`）：按碰撞冲量与接触法线方向压扁。
- **移动晃动**（`FixedUpdate`）：地面移动时叠加正弦晃动。

### 2.5 体积守恒形变（ApplyScale）

```csharp
volume = scaleX * scaleY * scaleZ;
correction = 1 / pow(volume, 1/3);   // 三次根号倒数
scaleX/Y/Z *= correction;             // 等比补偿，保持体积近似不变
```

效果：角色"压扁"时同时"变胖"，"拉伸"时"变瘦"，视觉上像果冻。

### 2.6 呼吸效果（ApplyBreathing）

静止时叠加正弦呼吸：

```
breath = sin(time * breathSpeed * 2π) * breathAmount
squashOffset.y += breath   （上下胀缩）
squashOffset.x/z -= breath/2（横向反向）
```

### 2.7 工具方法

```csharp
public void TriggerSquash(Vector3 worldDirection, float intensity);  // 外部触发压扁
public void TriggerStretch(Vector3 worldDirection, float intensity); // 外部触发拉伸（= 反向压扁）
public void ResetJelly();                                              // 重置形变
bool IsGrounded();                                                     // 球形射线地面检测
```

- `IsGrounded()`：用碰撞体 `bounds.extents.y * 0.9` 为半径，从中心向上偏移 0.1 处向下 `SphereCast` 0.3 距离。
- `OnDisable()` 自动 `ResetJelly()`，避免重启用残留形变。
- Editor 中 `OnDrawGizmosSelected()` 绘制地面检测球，便于调参。

### 2.8 物理设置（Awake）

```csharp
rb.interpolation = Interpolate;             // 平滑
rb.collisionDetectionMode = ContinuousDynamic; // 防穿透
rb.drag = 0.5f; rb.angularDrag = 0.5f;
rb.constraints = FreezeRotationX | FreezeRotationZ; // 仅允许绕 Y 转
```

## 3. CharacterCamera（占位）

源码：[CharacterController.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Character/CharacterController.cs)

该文件类名为 `CharacterCamera`，`Start`/`Update` 为空，仅有一行不完整的 `[SerializeField] private`。属未完成代码，建议：

- 若要实现跟随相机，可借助工程已引入的 **Cinemachine**（见 [Packages/manifest.json](file:///e:/Project/FluffyMovers/FluffyMovers/Packages/manifest.json)）配置虚拟相机。
- 完成后建议将文件名改为 `CharacterCamera.cs` 与类名一致，或删除占位。

## 4. 后续建议

1. **重命名整理**：将 `ElasticEffect.cs` → `JellyCharacter.cs`（或 `BouncyMover.cs`）使文件名与类名 `qt` 对齐，或直接重命名类。
2. **接入框架输入**：移动/跳跃改走 `InputMgr`，便于将来支持手柄与按键重绑。
3. **拆分职责**：Q 弹形变是通用表现，可抽成独立组件 `SquashAndStretch`，角色控制单独成类，便于复用到敌人、货物等。
4. **关卡接入**：在 `_Project/Scripts/Level` 与 `Gameplay` 中实现关卡逻辑，配合 `SceneMgr` 切场景。
