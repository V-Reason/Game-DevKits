# Event Bus

> 问题域：事实广播与表现解耦 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

逻辑层发生“生命值变化”“地块变化”“波次开始”后，UI、音频和特效可能都要响应。发布者不应直接依赖每个消费者。

## 设计模式

- **Observer（GoF 行为型）**：发布者只持有委托订阅者。
- **推模型**：事件结构体携带消费者所需的最小事实。
- 不是 Mediator：事件总线不承担多系统协调规则，也不替参与者决定业务路由。

## 公开接口

- `TypedEventBus<TEvent>`：实例化、按事件类型隔离的发布订阅通道。
- `EventSubscription`：可重复 Dispose 的退订句柄。

## 必须保持的契约

- 发布使用订阅者快照；发布中新增者从下一次事件开始接收。
- 单个订阅者异常不阻断其余订阅者，可注入错误处理回调。
- 预热后热发布路径复用快照缓冲，不逐次分配数组。
- 事件名应表达已经发生的事实，而不是模糊的 `Notify`。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Logic/Event/EventBus.cs` 与 `Events.cs`。原作使用静态泛型通道；本 Kit 改成可实例化的总线，避免测试间全局状态污染。

对应案例：[M04 战斗总装](../../../case-studies/deepseaoil/modules/M04-combat.md)、[M12 UI 与 HUD](../../../case-studies/deepseaoil/modules/M12-ui.md)。

## 验证

```powershell
dotnet test kits/engineering/event-bus/tests/GameDevKit.EventBus.Tests.csproj
```
