# Game-DevKits

Game-DevKits 是面向游戏开发团队的知识百宝箱：把真实项目里验证过的设计、算法和工程习惯，重写成可独立复制、可运行、可测试的中性范式。

它不是一套要求所有项目依赖的框架，也不是按 GoF 目录堆砌的教程。入口以“遇到什么问题”为主，设计模式作为可检索标签。

## 项目属性

| 项目 | 说明 |
| --- | --- |
| 状态 | v0.2 工程系统与运动战斗扩展 |
| 主要读者 | 未来的项目作者、协作开发者、技术复盘者 |
| 核心语言 | C# 9 / `.NET Standard 2.1` |
| 测试环境 | `.NET 8` + xUnit |
| 游戏引擎 | 核心不依赖引擎；提供 Unity 2022.3 薄适配 |
| 首个案例 | deepseaoil《荒原复苏指南》 |
| 许可证 | MIT |

## 当前内容

v0.1 包含 10 个基础 Kit；v0.2 新增 10 个工程与玩法 Kit，总计 20 个：

| 问题域 | Kit | 核心模式/算法 |
| --- | --- | --- |
| 玩法 | [State Machine](kits/gameplay/state-machine/README.md) | State、两阶段仲裁 |
| 玩法 | [Input Buffer](kits/gameplay/input-buffer/README.md) | 输入缓冲、时间窗 |
| 玩法 | [Reaction Rules](kits/gameplay/reaction-rules/README.md) | 数据驱动规则表、OCP |
| 玩法 | [Grid Propagation](kits/gameplay/grid-propagation/README.md) | BFS、Flood Fill、双缓冲 |
| 玩法 | [Flow Field](kits/gameplay/flow-field/README.md) | BFS/Dijkstra、缓存失效、局部 DOD |
| 工程 | [Composition Root](kits/engineering/composition-root/README.md) | Composition Root、手动 DI、生命周期 |
| 工程 | [Event Bus](kits/engineering/event-bus/README.md) | Observer、快照订阅 |
| 工程 | [Object Pool](kits/engineering/object-pool/README.md) | 对象池、溢出策略 |
| 工程 | [Timer Wheel](kits/engineering/timer-wheel/README.md) | 时间轮、句柄取消、双时间轴 |
| 工程 | [Snapshot Save](kits/engineering/snapshot-save/README.md) | Memento 思想、版本化、原子写入 |
| 工程 | [Asset Lifecycle](kits/engineering/asset-lifecycle/README.md) | Facade、并发加载、引用租约、LRU |
| 工程 | [Effect Orchestrator](kits/engineering/effect-orchestrator/README.md) | Strategy、待播队列、代次句柄 |
| 工程 | [Audio Arbitration](kits/engineering/audio-arbitration/README.md) | 准入策略、voice 上限、淡化与节流 |
| 工程 | [UI Stack](kits/engineering/ui-stack/README.md) | 分层栈、异步生命周期、返回语义 |
| 工程 | [Config Validation](kits/engineering/config-validation/README.md) | 清单、聚合校验、Spec Adapter |
| 工程 | [Diagnostics Snapshot](kits/engineering/diagnostics-snapshot/README.md) | Pull 模型、指标快照、异常隔离 |
| 玩法 | [Actor Motion](kits/gameplay/actor-motion/README.md) | 帧速度账本、移动门禁、状态优先级 |
| 玩法 | [Steering & Path Following](kits/gameplay/steering-path-following/README.md) | 前视、拐点、换步、转向闸门 |
| 玩法 | [Wave Sequencer](kits/gameplay/wave-sequencer/README.md) | 波次 State、出生节奏、快照 |
| 玩法 | [Ballistics & Aim](kits/gameplay/ballistics-aim/README.md) | 抛物线、八向吸附、伪 3D 瞄准 |

全部 15 个 deepseaoil 模块都可从 [案例研究](case-studies/deepseaoil/README.md) 进入。

## 仓库结构

```text
Game-DevKits/
├── README.md                    项目属性、入口与边界
├── LICENSE / ATTRIBUTION.md     MIT 与来源说明
├── GameDevKit.sln               10 个类库 + 10 个测试项目
├── catalog/                     按问题、模式、模块交叉索引
├── docs/                        分类法、术语、证据与编写规范
├── kits/
│   ├── gameplay/                State、Input、Rules、Grid、Flow
│   └── engineering/             Bootstrap、Events、Pool、Timer、Save
├── adapters/unity/              最小 Unity UPM 适配包
├── case-studies/deepseaoil/     15 模块卡、源码地图、模式账本
├── tools/verify.ps1             统一构建、测试、链接与边界检查
└── .github/workflows/verify.yml CI 验证入口
```

## 怎么选

- 知道问题但不知道模式：先看 [问题与模块索引](catalog/README.md)。
- 知道模式名称：看 [模式术语表](docs/pattern-glossary.md)。
- 想理解抽取标准：看 [架构分层](docs/taxonomy.md) 和 [证据政策](docs/source-evidence-policy.md)。
- 想贡献新 Kit：看 [编写规范](docs/authoring-guide.md)。

## 快速验证

```powershell
pwsh ./tools/verify.ps1
```

也可以直接运行全部测试：

```powershell
dotnet test GameDevKit.sln
```

每个 Kit 都是自己的类库与测试项目。复制某个 Kit 时，只需带走对应目录，不要求引用其它 Kit。

## 证据优先

模式声明分三级：

- **verified**：固定源码提交中可以定位角色关系或算法。
- **adapted**：问题与核心结构来自真实项目，代码和 API 已原创重写。
- **concept-only**：只借鉴设计思想，不声称严格实现了某个模式。

deepseaoil 案例固定到 `VReason/Update@254b9cf`，其编译门历史结果为 0 error / 0 warning。详细证据见 [pattern-ledger.md](case-studies/deepseaoil/pattern-ledger.md)。

## 明确边界

- 不是“所有游戏都能直接套用”的引擎。
- 不复制源项目代码、配置、美术、剧情或课程材料。
- 0 分配描述只适用于具体热路径，不宣称整个项目所有时段 0 GC。
- 流场、事件总线和组合根都有适用前提，README 会写明不适用场景。
- Unity 适配部分不能替代 Unity 编辑器内的最终编译和真机验证。

## 许可证

本项目使用 [MIT License](LICENSE)。来源与灵感说明见 [ATTRIBUTION.md](ATTRIBUTION.md)。
