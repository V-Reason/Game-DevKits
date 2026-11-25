# Wave Sequencer

> 问题域：波次阶段与出生节奏 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

波次不只是倒计时：准备期发配给、战斗期按间隔生成敌人、所有敌人生成且清空后才能进入结算，还要支持顺序关卡、循环模式和读档恢复。

## 模式与层级

- **State**：Prep、Battle、Settle。
- **Snapshot 思想**：导出阶段、计时和出生游标。
- 队列负责输出出生请求，业务层决定实际实例化。

## 公开接口

- `WavePlan<TEnemy,TGrant>` / `WaveEnemy<TEnemy>`。
- `WaveRunner<TEnemy,TGrant>`：Tick、Spawn、Grant、PhaseChange、Snapshot。
- `WaveRunnerOptions`：Loop、单帧最大转移数。
- `WaveSnapshot`。

## 必须保持的契约

- Prep 进入时发出一次 Grant。
- Battle 只有全部敌人已出生且场上无存活时才允许 Settle。
- 出生间隔有最小值保护，单帧最多生成一个。
- 单帧阶段转移有上限，避免零时长配置无限循环。
- 顺序完成与 Loop 重启互斥；快照恢复后不重复发 Grant。

## deepseaoil 映射

参考 `Logic/Wave/WaveLogic.cs` 与 `Tests/Runtime/Editor/WaveLogicTests.cs`。本 Kit 移除具体 SeedType 和事件总线依赖，使用泛型 Grant 和显式输出队列。

对应深挖：[波次调度](../../../case-studies/deepseaoil/submodules/wave-sequencer.md)。

## 验证

```powershell
dotnet test kits/gameplay/wave-sequencer/tests/GameDevKit.WaveSequencer.Tests.csproj
```
