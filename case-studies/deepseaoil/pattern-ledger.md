# deepseaoil 模式证据账本

本表是模式声明的唯一权威。模式名称按课程口径分层：原则、GoF 模式、架构风格、工程惯用法、算法/数据结构。

| 声明 | 层级 | 证据等级 | 源码证据 | 必须保留的限定 |
| --- | --- | --- | --- | --- |
| 四层单向依赖 | 分层架构 | verified | 四个核心 asmdef、`Docs/工程/架构约束.md` | 不写成“分层模式”；asmdef 是边界，不自动完成 DIP |
| GameRoot 是组合根 | 架构惯用法 | verified | `Presentation/Root/GameRoot.cs` | 同时承担 Unity 常驻入口，不等于通用 DI 容器 |
| GameRoot 是唯一真单例 | GoF Singleton | verified | `Foundation/Singleton.cs`、`GameRoot.cs` | 静态模块不自动都是 Singleton |
| GameRoot 手动装配服务 | 手动 DI | adapted | `GameRoot.Assemble()` | 不宣称使用 DI 框架 |
| EventBus<T> 广播事实 | GoF Observer | verified | `Logic/Event/EventBus.cs` | 不称为 Mediator |
| 状态组委托当前状态 | GoF State | adapted | `Foundation/StateMachine.cs`、`StateGroup.cs` | 不宣称所有业务状态共享完整 HSM |
| IEnemyBrain 可替换决策 | GoF Strategy | verified | `Logic/Actor/EnemyBrain.cs` | 工厂创建实现，但工厂本身不是 Strategy |
| IEffectDriver 可替换表现 | GoF Strategy | verified | `Presentation/Effects/IEffectDriver.cs` | 是否执行由 Effect 数据驱动 |
| EffectDriverFactory 按种类创建 | 简单工厂惯用法 | verified | `Drivers/EffectDriverFactory.cs` | 不称为 Factory Method |
| Config/Asset/Effect 收敛子系统 | GoF Facade | verified | 对应 `*Module` | Facade 不是访问控制层 |
| 逻辑移动口适配刚体 | GoF Adapter | verified | `Presentation/Adapters/*Motor.cs` | 只有在接口转换语境下使用 Adapter |
| Pool 管理借还与重置 | 对象池惯用法 | verified | `Foundation/Pool.cs` | 不称为 Flyweight |
| TileTickQueue 本帧写、下帧读 | 双缓冲惯用法 | verified | `Grid/TileTickQueue.cs` | 不是 GoF 23 模式 |
| TileChainReactor 使用 BFS/泛洪 | 算法/数据结构 | verified | `Grid/TileChainReactor.cs` | 有访问预算，避免环无限扩散 |
| NavField 从目标反推方向 | 算法/数据结构 | verified | `Grid/NavField.cs` | 共享目标与受控重建是收益前提 |
| NavField 混用 BFS 与加权树 | BFS/Dijkstra 思想 | verified | `NavField.Build()` | 不是为每只敌人单独跑 A* |
| 流场使用平行数组与复用缓冲 | 局部 DOD | adapted | `NavField`、只读 Tick 上下文 | 项目整体不是 ECS/DOD 架构 |
| DTO 快照保存世界 | Memento 思想 | concept-only | `GameSaveDTO.cs`、`SaveService.cs` | 没有严格 Originator/Memento/Caretaker 封装 |
| 临时文件后 Replace/Move | 原子发布惯用法 | verified | `SaveService.TryReplaceFile()` | 不能替代跨进程事务 |
| UI 吸收数据/视图分离 | MVC/MVVM 思想 | concept-only | Panels、EventBus 订阅 | 项目不是严格 MVC 框架 |
| 核心热路径降低分配 | 性能目标 | verified | EventBus 测试、容器复用、NonAlloc 物理查询 | 不是全工程、所有时段绝对 0 GC |

## 误用红线

- 有 `switch` 不等于 State。
- 有事件请求不等于 Command。
- 有静态类不等于 Singleton。
- 有数组和结构体不等于 ECS。
- 有快照对象不等于严格 Memento。
- 分层是架构风格，State/Observer 才是 GoF 设计模式。
