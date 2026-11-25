# 问题与模式索引

## 按问题找

| 我遇到什么 | 先看 |
| --- | --- |
| 行为随状态变化，分支越来越多 | [State Machine](../kits/gameplay/state-machine/README.md) |
| 按压输入被帧边界吞掉 | [Input Buffer](../kits/gameplay/input-buffer/README.md) |
| 组合规则越加越多 | [Reaction Rules](../kits/gameplay/reaction-rules/README.md) |
| 连锁扩散会重复或无限循环 | [Grid Propagation](../kits/gameplay/grid-propagation/README.md) |
| 大量单位追同一目标 | [Flow Field](../kits/gameplay/flow-field/README.md) |
| 启动、Tick、销毁顺序不可控 | [Composition Root](../kits/engineering/composition-root/README.md) |
| 一个逻辑事实要通知多个表现系统 | [Event Bus](../kits/engineering/event-bus/README.md) |
| 高频创建销毁产生 GC 或引擎开销 | [Object Pool](../kits/engineering/object-pool/README.md) |
| 大量短计时器不能每帧遍历 | [Timer Wheel](../kits/engineering/timer-wheel/README.md) |
| 存档覆盖到一半可能导致旧档也损坏 | [Snapshot Save](../kits/engineering/snapshot-save/README.md) |
| 同一资源被多个系统重复请求 | [Asset Lifecycle](../kits/engineering/asset-lifecycle/README.md) |
| 不同表现需要不同播放策略 | [Effect Orchestrator](../kits/engineering/effect-orchestrator/README.md) |
| 高频音效占满 voice 或互相叠响 | [Audio Arbitration](../kits/engineering/audio-arbitration/README.md) |
| 面板异步加载、重复打开和返回栈混乱 | [UI Stack](../kits/engineering/ui-stack/README.md) |
| 配置缺表、重复主键、引用断裂 | [Config Validation](../kits/engineering/config-validation/README.md) |
| 调试指标分散且耦合正式代码 | [Diagnostics Snapshot](../kits/engineering/diagnostics-snapshot/README.md) |
| 多个系统同帧抢写角色速度 | [Actor Motion](../kits/gameplay/actor-motion/README.md) |
| 敌人沿流场走仍抖动、顶墙或切角 | [Steering & Path Following](../kits/gameplay/steering-path-following/README.md) |
| 波次倒计时、清场和出生节奏不一致 | [Wave Sequencer](../kits/gameplay/wave-sequencer/README.md) |
| 投掷落点与视觉弧线不一致或瞄准翻转 | [Ballistics & Aim](../kits/gameplay/ballistics-aim/README.md) |

## 按模式找

| 模式/思想 | 层级 | Kit |
| --- | --- | --- |
| State | GoF 行为型 | State Machine |
| Observer | GoF 行为型 | Event Bus |
| Strategy | GoF 行为型 | Flow Field 的成本策略 |
| Composition Root | 架构惯用法 | Composition Root |
| Manual DI | 架构惯用法 | Composition Root |
| 对象池 | 游戏工程惯用法 | Object Pool |
| 双缓冲 | 工程惯用法 | Grid Propagation |
| Memento 思想 | GoF 行为型思想 | Snapshot Save |
| BFS / Flood Fill | 算法 | Grid Propagation |
| BFS / Dijkstra | 算法 | Flow Field |
| 时间轮 | 数据结构/算法 | Timer Wheel |
| Strategy | GoF 行为型 | Effect Orchestrator、Audio Arbitration |
| Adapter | GoF 结构型 | Config Validation |
| Facade | GoF 结构型 | Asset Lifecycle、Diagnostics Snapshot |
| State | GoF 行为型 | Wave Sequencer |
| LRU / 引用租约 | 工程惯用法 | Asset Lifecycle |
| 分层面板栈 | 架构惯用法 | UI Stack |
| 前视与转向闸门 | 游戏算法/控制律 | Steering & Path Following |
| 抛物线 / 椭圆轨迹 | 数学算法 | Ballistics & Aim |

## 按 deepseaoil 模块找

| 模块 | 可运行 Kit |
| --- | --- |
| [M01 工程底座](../case-studies/deepseaoil/modules/M01-foundation.md) | Composition Root |
| [M02 数据与配置](../case-studies/deepseaoil/modules/M02-data.md) | Reaction Rules、Snapshot Save |
| [M03 运行时骨架](../case-studies/deepseaoil/modules/M03-runtime.md) | Composition Root、Object Pool |
| [M04 战斗总装](../case-studies/deepseaoil/modules/M04-combat.md) | Event Bus |
| [M05 网格与地块](../case-studies/deepseaoil/modules/M05-grid.md) | State Machine、Grid Propagation |
| [M06 玩家](../case-studies/deepseaoil/modules/M06-player.md) | State Machine、Input Buffer |
| [M07 元素与连锁](../case-studies/deepseaoil/modules/M07-elements.md) | Reaction Rules、Grid Propagation |
| [M08 敌人与寻路](../case-studies/deepseaoil/modules/M08-enemies.md) | Flow Field |
| [M09 关卡与流程](../case-studies/deepseaoil/modules/M09-levels.md) | State Machine、Event Bus |
| [M10 特效与反馈](../case-studies/deepseaoil/modules/M10-effects.md) | Event Bus、Object Pool |
| [M11 光照](../case-studies/deepseaoil/modules/M11-lighting.md) | 案例说明 |
| [M12 UI 与 HUD](../case-studies/deepseaoil/modules/M12-ui.md) | Event Bus |
| [M13 剧情与对话](../case-studies/deepseaoil/modules/M13-story.md) | State Machine、Snapshot Save |
| [M14 音频](../case-studies/deepseaoil/modules/M14-audio.md) | Object Pool、Timer Wheel |
| [M15 存档与诊断](../case-studies/deepseaoil/modules/M15-save.md) | Snapshot Save |

## v0.2 二级深挖

| 父模块 | 深挖页 | 新 Kit |
| --- | --- | --- |
| M02 | [资产生命周期](../case-studies/deepseaoil/submodules/asset-lifecycle.md) | Asset Lifecycle |
| M02 | [配置校验](../case-studies/deepseaoil/submodules/config-validation.md) | Config Validation |
| M06 | [运动账本与门禁](../case-studies/deepseaoil/submodules/actor-motion.md) | Actor Motion |
| M06 | [投掷与瞄准](../case-studies/deepseaoil/submodules/ballistics-aim.md) | Ballistics & Aim |
| M08 | [路径跟随与转向](../case-studies/deepseaoil/submodules/steering-following.md) | Steering & Path Following |
| M09 | [波次调度](../case-studies/deepseaoil/submodules/wave-sequencer.md) | Wave Sequencer |
| M10 | [特效编排](../case-studies/deepseaoil/submodules/effect-orchestration.md) | Effect Orchestrator |
| M12 | [UI 栈](../case-studies/deepseaoil/submodules/ui-stack.md) | UI Stack |
| M14 | [音频仲裁](../case-studies/deepseaoil/submodules/audio-arbitration.md) | Audio Arbitration |
| M15 | [指标与诊断](../case-studies/deepseaoil/submodules/diagnostics-snapshot.md) | Diagnostics Snapshot |
