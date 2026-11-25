# 技术与模式分层

同一个名词放到错误层级，会让设计讨论失真。本仓库使用以下四层：

| 层级 | 回答的问题 | 例子 |
| --- | --- | --- |
| 架构风格/架构模式 | 模块怎样组织、依赖和数据流怎样走 | 分层架构、事件驱动、MVC、ECS |
| 设计原则 | 为什么这样切分变化 | OCP、SRP、LSP、ISP、DIP、LoD、CRP |
| GoF 设计模式 | 一组对象如何协作 | State、Observer、Strategy、Adapter |
| 工程惯用法/算法 | 游戏工程怎样实现具体机制 | Composition Root、对象池、BFS、时间轮、双缓冲 |

推荐表达：

> 项目采用分层架构；移动状态组内部使用 State；敌人决策通过 Strategy 可替换；寻路使用共享流场算法。

不推荐表达：

> 项目用了分层模式、状态模式、流场模式和事件模式。

## 本仓库的边界

- `State Machine` 是 GoF State 的可运行参考。
- `Event Bus` 是 Observer，不是 Mediator。
- `Reaction Rules` 是数据驱动查表，不强行归为 Strategy。
- `Grid Propagation` 的核心是 BFS/Flood Fill 和双缓冲惯用法。
- `Flow Field` 混用 BFS、加权路径和缓存；不能仅因使用数组就宣称 ECS。
- `Composition Root` 是架构惯用法，手动 DI 不是容器框架。
- `Snapshot Save` 借鉴 Memento 思想，不是严格三角色模式。
