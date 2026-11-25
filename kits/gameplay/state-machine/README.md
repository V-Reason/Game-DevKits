# State Machine

> 问题域：玩法状态机 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

把 Idle、Move、Dash、Hurt、WavePhase 等“行为随内部状态变化、且状态会主动迁移”的逻辑拆成可测试的状态对象，避免单个方法不断膨胀为 `switch`。

## 设计模式

- **State（GoF 行为型）**：Context 把 Enter、Tick、Exit、IsComplete 委托给当前状态。
- **两阶段仲裁**：高优先级状态先评估，再提交；只有提交成功才中断当前状态。
- 这不是 Strategy。Strategy 通常由外部选择算法，State 关注内部迁移和生命周期。

## 公开接口

- `IState<TKey,TContext>`：状态标签、进入、退出、Tick、完成判定。
- `StateMachine<TKey,TContext>`：注册状态、显式切换、当前状态和切换通知。
- `StateGroup<TKey,TContext>`：在基本状态机之上提供 Fallback 和抢占评估/提交。

## 最小用法

```csharp
var machine = new StateMachine<Mode, TickContext>();
machine.Add(new IdleState());
machine.Add(new DashState());
machine.TryChange(Mode.Idle, context);
machine.Tick(context);
```

## 必须保持的契约

- 未注册或重复切换不触发 Enter/Exit。
- 切换顺序固定为 `old.Exit -> new.Enter -> StateChanged`。
- 抢占评估不得消费资源；只有提交成功才执行切换。
- 完成态在下一轮仲裁中让位给 Fallback。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Foundation/StateMachine.cs`、`StateGroup.cs` 和 `IState.cs`。本项目重写了类型与过渡契约，没有复制原作代码。

对应案例：[M05 网格与地块](../../../case-studies/deepseaoil/modules/M05-grid.md)、[M06 玩家](../../../case-studies/deepseaoil/modules/M06-player.md)。

## 不适用

- 两个长期稳定分支，直接 `if` 更清楚。
- 只是选择一次算法，应使用 Strategy。
- 需要任意深度层级状态继承时，本 Kit 只提供受控抢占，不宣称完整 HSM。

## 验证

```powershell
dotnet test kits/gameplay/state-machine/tests/GameDevKit.StateMachine.Tests.csproj
```
