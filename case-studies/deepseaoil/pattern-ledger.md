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
| 资源入口统一缓存、加载和引用计数 | Facade + 生命周期惯用法 | verified | `Data/Asset/AssetModule.cs`、`CacheStore.cs`、`RefCounter.cs` | 异步租约不强行称为严格 Proxy |
| 空闲资源冷却后按 LRU 淘汰 | 工程惯用法 | verified | `LifecycleMgr.cs` | Pin 与活跃引用必须排除在淘汰候选外 |
| EffectDriver 可替换播放方式 | GoF Strategy | verified | `Presentation/Effects/IEffectDriver.cs` | 工厂负责创建，不是 Router 的职责 |
| EffectDriverFactory 按种类创建 | Simple Factory | verified | `Drivers/EffectDriverFactory.cs` | 不是 Factory Method |
| EffectHandle 带驱动与代次 | 生命周期惯用法 | verified | `EffectHandle.cs` | 旧句柄不能误停新实例 |
| 音频 voice 上限与同素材去重 | 仲裁/工程惯用法 | verified | `AudioManager.PlaySfxInternal()` | 原项目 voice 满时拒绝，不自动抢占 |
| UI 面板分层栈与关闭最上层 | Facade + 分层栈 | verified | `Presentation/UI/UIMgr.cs` | 不宣称严格 MVC |
| 配置表清单与启动抽样校验 | Facade + 校验管线 | verified | `ConfigModule.cs`、`StartupValidator.cs`、`TablesMeta.cs` | 生成表属性与手写清单必须一致 |
| DataMetrics 使用拉模型 | Snapshot 惯用法 | verified | `Data/Metrics/DataMetrics.cs` | 不走 EventBus，不持资源引用 |
| 角色速度帧末单次提交 | 游戏循环惯用法 | verified | `Logic/Movement/ActorLedger.cs` | 反向分支原实现仍存在加速度未限幅问题 |
| 受击/麻痹/减速形成门禁优先级 | 控制流惯用法 | verified | `Logic/Actor/Status/StatusGroup.cs` | 是优先级裁决，不称为责任链 |
| 流场跟随使用前视、拐点与换步 | 控制算法 | verified | `EnemyPathFollower.cs`、`EnemyNavigator.cs` | 与流场缓存、体半径和可站立探针共同生效 |
| 转向闸门限制单帧翻面 | 控制算法 | verified | `EnemySteeringGate.cs` | 只负责形状，不决定目标方向 |
| 波次 Prep/Battle/Settle | GoF State 思路 | verified | `Logic/Wave/WaveLogic.cs` | 同时使用计时、出生游标与清场判定 |
| 投掷逻辑落点与视觉弧高分离 | 数学算法 | verified | `ProjectileTrajectory.cs`、`Foundation/Ballistics.cs` | 视觉高度不能改变真实落点 |
| 椭圆武器轨道与透视挤压 | 数学算法 | verified | `Presentation/Combat/WeaponAimController.cs` | 层级只看 rawAngle，不看视觉抬升 |

## 误用红线

- 有 `switch` 不等于 State。
- 有事件请求不等于 Command。
- 有静态类不等于 Singleton。
- 有数组和结构体不等于 ECS。
- 有快照对象不等于严格 Memento。
- 分层是架构风格，State/Observer 才是 GoF 设计模式。
