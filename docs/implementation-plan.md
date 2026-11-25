# v0.1 实施记录

## 目标

把 deepseaoil 的高复用机制整理成 10 个独立、可测试的 Kit，并完整编目其 15 个模块。

## 已实现

- 纯 C# Kit：State Machine、Input Buffer、Reaction Rules、Grid Propagation、Flow Field、Composition Root、Event Bus、Object Pool、Timer Wheel、Snapshot Save。
- deepseaoil 15 模块案例卡、源码地图和模式证据账本。
- 问题优先的 README、目录索引和模式术语表。
- `.NET 8` 测试与 warnings-as-errors 构建。
- MIT 许可证和来源说明。

## 验收

- 每个 Kit 都经历失败测试 -> 最小实现 -> 通过测试。
- 根解决方案汇总 10 个类库和 10 个测试项目。
- `tools/verify.ps1` 检查测试、核心源码的 Unity 依赖、必需文档和本机路径泄漏。
- Unity UPM 适配包只做源码交付；最终 Unity 编译需在 Unity 2022.3 中人工复核。

## 后续候选

- Unity grid-light renderer sample。
- MVC/MVVM HUD architecture comparison。

## v0.2 已完成

- 工程：Asset Lifecycle、Effect Orchestrator、Audio Arbitration、UI Stack、Config Validation、Diagnostics Snapshot。
- 玩法：Actor Motion、Steering & Path Following、Wave Sequencer、Ballistics & Aim。
- 新增 10 个二级深挖页，父模块链接、问题索引和模式账本已同步。
- Actor Motion 修复了原项目反向转向未按加速度限幅的已知问题。
- 总计 20 个独立 Kit；新增测试全部先失败、后通过。
