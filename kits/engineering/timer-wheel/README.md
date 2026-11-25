# Timer Wheel

> 问题域：大量短生命周期定时任务 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

冷却、麻痹、倒计时、Buff 和延迟回调数量多、生命周期短。一直遍历全部任务会提高每帧成本；时间轮把任务散到离散槽中，只处理到期的槽。

## 模式与算法

- **时间轮（数据结构/算法）**：槽、轮次、目标时间和句柄。
- **句柄取消**：取消不依赖任务在链表中的位置。
- **双时间轴**：缩放时间用于暂停会停的游戏计时；未缩放时间用于 UI 或真实等待。
- 这不是 GoF 模式。

## 公开接口

- `TimerWheel`：低层时间轮、Schedule、Advance、Cancel、TryGetInfo。
- `TimerService`：缩放/未缩放两条时间轴。
- `TimerHandle`、`TimerOptions`、`TimerInfo`。

## 必须保持的契约

- 单次 Timer 在回调前退出管理表。
- 回调内可安全取消自身。
- 大帧不补跑大量重复周期，避免一次卡顿后爆发回调。
- 负 Delta 视为零；非法配置在调度时失败。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Logic/Services/Timer/TimerWheel.cs`、`TimerManager.cs` 与 `TimerHandle.cs`。本 Kit 去除 Unity 驱动和日志，保留通用时间轮。

对应案例：[M06 玩家](../../../case-studies/deepseaoil/modules/M06-player.md)、[M14 音频](../../../case-studies/deepseaoil/modules/M14-audio.md)。

## 验证

```powershell
dotnet test kits/engineering/timer-wheel/tests/GameDevKit.TimerWheel.Tests.csproj
```
