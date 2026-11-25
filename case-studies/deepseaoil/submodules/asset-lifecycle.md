# 资产与缓存生命周期

父模块：[M02 数据与配置](../modules/M02-data.md)

## 原项目结构

`AssetModule` 在统一入口下组合 Source/Registry、缓存、引用计数、加载调度、LRU 与失败降级：

- 同 Key 挂起请求合并。
- 最多 4 个并发加载，失败重试。
- 引用归零后先进入 60 秒冷却，再允许淘汰。
- 预加载资源 Pin，不参与引用计数。
- `DataMetrics` 以拉模型读取缓存命中、队列、失败和淘汰。

## 模式与层级

- Facade：统一资源入口。
- 缓存、引用计数、LRU：工程惯用法。
- 异步句柄具有受控访问思想，但核心 Kit 以 RAII 租约表达，不宣称严格 Proxy。

## 通用化取舍

[Asset Lifecycle](../../../kits/engineering/asset-lifecycle/README.md) 使用 `IAssetSource<TKey,TAsset>`、`AssetLease` 和可注入时钟，移除 UnityEngine 与 Resources API。

## 保留的坑

- 取消一个等待者不能取消所有同 Key 请求。
- 淘汰回调应在锁外执行。
- 引用计数和 Pin 必须共同决定可淘汰性。
