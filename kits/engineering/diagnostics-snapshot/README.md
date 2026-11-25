# Diagnostics Snapshot

> 问题域：运行指标与诊断快照 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

调试面板、日志和测试需要查看缓存队列、失败数、活动实例等运行事实，但不应该让各系统互相依赖。Pull 模型按需抓取只读快照最简单。

## 模式与层级

- **Facade + Snapshot**：Hub 聚合来源并产生值语义快照。
- 不是 EventBus：采集由观察者主动触发，不要求持续广播。

## 公开接口

- `IDiagnosticsSource`：命名来源和指标列表。
- `DiagnosticsHub`：Register、Unregister、Capture。
- `DiagnosticsSnapshot`：不可变条目、问题列表和按名称查询。
- `DiagnosticMetric` / `DiagnosticEntry` / `DiagnosticKind`。

## 必须保持的契约

- 来源名和指标名稳定排序。
- 单个来源失败只生成 Issue，不影响其它来源。
- 快照复制当时数值，后续源变化不改变旧快照。
- 重复来源名在注册时拒绝。

## deepseaoil 映射

参考 `Data/Metrics/DataMetrics.cs`、`DataSnapshot.cs`，以及 Diagnostics Harness 的调试思路。本 Kit 只抽取指标聚合，不包含 GUI。

对应深挖：[指标与诊断](../../../case-studies/deepseaoil/submodules/diagnostics-snapshot.md)。

## 验证

```powershell
dotnet test kits/engineering/diagnostics-snapshot/tests/GameDevKit.DiagnosticsSnapshot.Tests.csproj
```
