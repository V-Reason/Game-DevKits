# Asset Lifecycle

> 问题域：资源加载与缓存生命周期 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

同一资源被多个系统同时请求时，不能重复加载；不同系统持有同一资源时，需要独立租约；资源空闲后需要冷却期并受 LRU 上限约束；失败应重试并允许降级。

## 模式与层级

- **Facade**：`AssetCache` 收敛加载、缓存、引用和淘汰。
- **RAII 租约**：`AssetLease` 保证成对 Retain/Release。
- 缓存和 LRU 是工程惯用法；本 Kit 不把异步租约强行称为严格 Proxy。

## 公开接口

- `IAssetSource<TKey,TAsset>`：可替换的异步资源来源。
- `AssetCache<TKey,TAsset>`：Acquire、Preload、Fallback、Maintain、Stats。
- `AssetLease<TKey,TAsset>`：一次性释放的资源租约。
- `IAssetClock` / `ManualAssetClock`：可测试冷却时间。

## 必须保持的契约

- 同 Key 并发请求只触发一次 Source 加载。
- 引用大于零、被 Pin 或在冷却期内的条目不能淘汰。
- 失败按配置重试；没有 Fallback 时抛 `AssetLoadException`。
- 调用方取消等待不能污染共享加载或未来请求。

## deepseaoil 映射

参考 `Data/Asset/AssetModule.cs`、`CacheStore.cs`、`RefCounter.cs`、`LifecycleMgr.cs`、`LoadScheduler.cs` 和 `FailureHandler.cs`。本 Kit 去掉 UnityEngine 类型，以通用 Source 和租约表达同一机制。

对应深挖：[资产与缓存生命周期](../../../case-studies/deepseaoil/submodules/asset-lifecycle.md)。

## 验证

```powershell
dotnet test kits/engineering/asset-lifecycle/tests/GameDevKit.AssetLifecycle.Tests.csproj
```
