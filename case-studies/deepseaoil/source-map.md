# deepseaoil 源码地图

> 基线：`VReason/Update@254b9cf`。下列路径均相对于 deepseaoil 工程根目录。

## 架构与启动

| 主题 | 关键路径 |
| --- | --- |
| 程序集约束 | `AGENTS.md`、`Docs/工程/架构约束.md` |
| 四个 asmdef | `Assets/Scripts/Foundation/DeepseaOil.Foundation.asmdef`、`Data/DeepseaOil.Data.asmdef`、`Logic/DeepseaOil.Logic.asmdef`、`Presentation/DeepseaOil.Presentation.asmdef` |
| 进程组合根 | `Assets/Scripts/Presentation/Root/GameRoot.cs` |
| 场景组合根 | `Assets/Scripts/Presentation/Root/CombatRoot.cs`、`Assets/Scripts/Presentation/ISceneRoot.cs` |
| Unity 时钟适配 | `Assets/Scripts/Presentation/Adapters/GameTime.cs` |

## 数据与规则

| 主题 | 关键路径 |
| --- | --- |
| 配置门面 | `Assets/Scripts/Data/Config/ConfigModule.cs` |
| Spec 防腐层 | `Assets/Scripts/Data/Config/Spec/*.cs` |
| 资源生命周期 | `Assets/Scripts/Data/Asset/AssetModule.cs`、`CacheStore.cs`、`RefCounter.cs` |
| 一级反应查表 | `Assets/Scripts/Logic/Element/ReactionResolver.cs` |

## 运行时与玩法

| 主题 | 关键路径 |
| --- | --- |
| 状态机基础 | `Assets/Scripts/Foundation/StateMachine.cs`、`StateGroup.cs`、`IState.cs` |
| 输入缓冲 | `Assets/Scripts/Logic/Input/InputBuffer.cs`、`InputSnapshot.cs` |
| 速度账本 | `Assets/Scripts/Logic/Movement/ActorLedger.cs` |
| 网格连锁 | `Assets/Scripts/Logic/Grid/TileChainReactor.cs`、`TileTickQueue.cs` |
| 流场 | `Assets/Scripts/Logic/Grid/NavField.cs`、`NavFieldSet.cs`、`NavMap.cs` |
| 敌人行为 | `Assets/Scripts/Logic/Actor/EnemyBrain.cs`、`EnemyNavigator.cs` |
| 波次与关卡 | `Assets/Scripts/Logic/Wave/WaveLogic.cs`、`Assets/Scripts/Logic/Level/LevelLogic.cs` |

## 表现与质量

| 主题 | 关键路径 |
| --- | --- |
| 强类型事件 | `Assets/Scripts/Logic/Event/EventBus.cs`、`Events.cs` |
| 特效策略 | `Assets/Scripts/Presentation/Effects/IEffectDriver.cs`、`Drivers/EffectDriverFactory.cs` |
| 网格光场 | `Assets/Scripts/Presentation/Lighting/TileGridLightmap.cs` |
| UI 管理 | `Assets/Scripts/Presentation/UI/UIMgr.cs`、`BasePanel.cs` |
| 音频 | `Assets/Scripts/Presentation/Audio/AudioManager.cs` |
| 存档 | `Assets/Scripts/Logic/Services/SaveService.cs`、`Assets/Scripts/Data/Save/GameSaveDTO.cs` |
| 测试 | `Assets/Tests/Runtime/Editor/*.cs` |
| 编译门 | `Tools/compile-gate.ps1` |

## 复盘证据

- Word 成品：`荒原复苏指南_开发者技术复盘.docx`
- v3 模块结构：`archive/v3-sources/README.md`
- 模式与算法手册：`archive/v2-sources/markdown/专题附录/A_模式与算法手册.md`
- 术语与事实校对：`archive/v2-sources/markdown/专题附录/D_术语与事实校对.md`

Word 和 Markdown 复盘材料不复制进本仓库。源码路径与提交号构成可复核的最小证据链。
