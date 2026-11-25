# 指标与诊断

父模块：[M15 存档与诊断](../modules/M15-save.md)

## 原项目结构

DataMetrics 通过拉模型输出配置、缓存、加载、失败、淘汰和命中率；诊断 Harness 与 Gizmos 直接读取运行态，而不把调试事件塞进正式事件总线。

## 模式与层级

- Facade + Snapshot：Hub 聚合多个只读来源。
- 拉模型：只在观察者请求时采集。
- 不是 EventBus，也不要求所有指标持续广播。

## 通用化取舍

[Diagnostics Snapshot](../../../kits/engineering/diagnostics-snapshot/README.md) 提供稳定排序、Counter/Gauge、来源异常隔离和值快照。

## 保留的坑

- 调试读取不能持有资源引用。
- 单个来源异常不能让整个诊断页失效。
- 快照必须是抓取时副本，不能随源对象后续变化。
