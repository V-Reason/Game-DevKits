# M03 运行时骨架

## 解决什么问题

场景对象、服务、输入、UI、音频和计时器如果各自 `Awake/Start/Update`，初始化顺序与销毁顺序会不可控。

## 结构

`GameRoot` 是进程级组合根、唯一常驻单例和统一驱动入口；场景通过 `ISceneRoot.Order` 注册；服务按固定顺序初始化、Tick、逆序销毁。

## 模式与思想

- **Composition Root + 手动 DI**：所有装配在一个明确入口。
- **Singleton**：只用于真正进程级唯一入口，不作为全局服务垃圾桶。
- **生命周期编排**：Order、Attach、Tick、Dispose 是显式契约。

## 可复用精华

[Composition Root](../../../kits/engineering/composition-root/README.md)；对象复用参考 [Object Pool](../../../kits/engineering/object-pool/README.md)。

## 不应过度宣称

静态模块不是 Singleton；组合根变长不是架构成就，而是继续拆分的信号。

## 源码

`Presentation/Root/GameRoot.cs`、`Presentation/ISceneRoot.cs`、`Presentation/Adapters/GameTime.cs`。
