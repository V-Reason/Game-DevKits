# Input Buffer

> 问题域：输入与时间窗 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

渲染帧和物理帧不对齐时，玩家在落地前片刻按下的跳跃、冲刺或攻击可能被吞。输入缓冲把“按下沿”保留在一个短时间窗内，由逻辑层在合法时消费。

## 模式与层级

- 属于**游戏循环/输入处理惯用法**，不是 GoF 模式。
- 使用不可变快照、显式时间戳和一次性消费。
- 宿主负责产生按下沿；消费者负责选择合法窗口。

## 公开接口

- `InputSnapshot`：与引擎无关的移动向量快照。
- `InputWindow`：校验有限、非负的闭区间时间窗。
- `InputBuffer<TCommand>`：记录命令时间、查询窗口、消费一次、清空。

## 必须保持的契约

- 时间必须单调不减，倒退立即失败。
- 窗口两端都包含；超出窗口后永不再被消费。
- `TryConsume` 成功后同一按下不能再次触发。
- `Clear` 同时清理命令和最新移动快照。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Logic/Input/InputBuffer.cs` 与 `InputSnapshot.cs`。原作使用环形历史窗口；本 Kit 保留其核心的一击缓冲语义，并简化为可跨项目使用的通用 API。

对应案例：[M06 玩家](../../../case-studies/deepseaoil/modules/M06-player.md)。

## Unity 接法

在 Input System 的按下回调中调用 `Push`，在物理 Tick 前读取输入快照并调用 `TryConsume`。不要把按下沿延迟到渲染帧再求差。

## 验证

```powershell
dotnet test kits/gameplay/input-buffer/tests/GameDevKit.InputBuffer.Tests.csproj
```
