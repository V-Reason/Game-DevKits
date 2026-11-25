# Effect Orchestrator

> 问题域：表现效果路由与句柄生命周期 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

一个效果 ID 应交给不同播放驱动；资源未就绪时不能直接丢失播放意图；高频同类效果需要有限等待队列；旧句柄不能误停复用后的新实例。

## 模式与层级

- **Strategy**：`IEffectDriver` 是可替换播放策略。
- **Facade**：`EffectRouter` 隐藏注册、排队、Tick 与统计。
- 驱动由外部简单工厂创建；Router 不接管具体资源与池。

## 公开接口

- `IEffectDriver<TEffectId,TContext>`：就绪、播放、更新、停止、Tick、清场、统计。
- `EffectRouter<TEffectId,TContext>`：注册、路由、待播队列和驱动聚合。
- `EffectHandle`：驱动所有者、实例 ID 与代次。
- `EffectIssue`：未知驱动和队列溢出诊断。

## 必须保持的契约

- 未注册驱动返回 `EffectHandle.None` 并报告问题。
- 未就绪请求按 FIFO 排队；每效果队列有上限。
- 队列溢出丢弃最旧请求。
- 只有驱动判定有效的句柄才能 Update/Stop。
- `ClearAll` 清空待播并让驱动进入新代次。

## deepseaoil 映射

参考 `Presentation/Effects/EffectModule.cs`、`IEffectDriver.cs`、`EffectHandle.cs`、`EffectCatalog.cs` 和 `Drivers/EffectDriverFactory.cs`。Unity 粒子与材质细节不进入核心。

对应深挖：[特效编排](../../../case-studies/deepseaoil/submodules/effect-orchestration.md)。

## 验证

```powershell
dotnet test kits/engineering/effect-orchestrator/tests/GameDevKit.EffectOrchestrator.Tests.csproj
```
