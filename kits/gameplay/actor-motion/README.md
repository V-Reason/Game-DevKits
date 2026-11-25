# Actor Motion

> 问题域：角色速度账本与移动门禁 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

同帧多个状态、冲量、持续力和受击效果都可能修改速度。若每个系统直接写刚体，最终速度会取决于调用顺序，并可能突破加速度。

## 模式与层级

- **帧速度账本**：帧首读取、帧内累加、帧末一次提交。
- **门禁优先级**：麻痹 > 受击推力 > 减速乘数。
- 是控制律与游戏循环惯用法，不强行贴 GoF 标签。

## 公开接口

- `IMotionMotor`：核心只要求速度读写。
- `MotionParams`：速度、加速度、转向衰减。
- `MotionLedger`：Move、Brake、Impulse、Force、Gates、Commit。
- `MotionStatus`：Slow、Stun、Hurt 及其优先级。
- `MotionGates`：强制速度或速度缩放的只读描述。

## 必须保持的契约

- 斜向输入归一化。
- 冲量不乘 Δt，持续力乘 Δt。
- 反向转向受加速度上限约束。
- 强制速度覆盖本帧状态提交。
- 减速续命只延长窗口，不叠乘；麻痹取较长者。

## deepseaoil 映射

参考 `Logic/Movement/ActorLedger.cs`、`MoveGates.cs` 和 `Logic/Actor/Status/StatusGroup.cs`。本 Kit 修复原作中反向速度分支未限制加速度的问题，并将 `Vector3/Vector2` Unity 类型替换为 `System.Numerics`。

对应深挖：[运动账本与门禁](../../../case-studies/deepseaoil/submodules/actor-motion.md)。

## 验证

```powershell
dotnet test kits/gameplay/actor-motion/tests/GameDevKit.ActorMotion.Tests.csproj
```
