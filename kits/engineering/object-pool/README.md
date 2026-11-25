# Object Pool

> 问题域：高频对象生命周期 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

子弹、粒子、飘字、音频播放器和特效实例高频创建/销毁会产生 GC 尖峰和引擎对象开销。对象池复用实例，并把重置与销毁语义显式化。

## 模式与层级

- **对象池（游戏工程惯用法）**：维护空闲与借出对象。
- 不把它称为 Flyweight：享元强调共享不变数据，池强调有状态实例的复用。
- 池空策略是显式配置，不是隐式增长。

## 公开接口

- `PoolOptions<T>`：工厂、Reset、Destroy、保留上限、总量上限。
- `PoolOverflowPolicy`：Grow、GrowUpToLimit、Drop、Throw。
- `ObjectPool<T>`：TryRent、Rent、Return、Prewarm、Clear、Dispose。
- `PoolStats`：Active、Idle、TotalCreated、PeakActive、Dropped。

## 必须保持的契约

- 归还时先执行 Reset，再进入空闲栈。
- 非本池借出的对象或重复归还立即失败。
- 空闲区不超过 MaxRetained；超出的归还对象交给 Destroy。
- Dispose 幂等，之后禁止继续借取。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Foundation/Pool.cs`。本 Kit 保留策略、统计和生命周期回调，去掉 Unity 日志与 GameObject 依赖。

对应案例：[M03 运行时骨架](../../../case-studies/deepseaoil/modules/M03-runtime.md)、[M10 特效反馈](../../../case-studies/deepseaoil/modules/M10-effects.md)。

## 验证

```powershell
dotnet test kits/engineering/object-pool/tests/GameDevKit.ObjectPool.Tests.csproj
```
