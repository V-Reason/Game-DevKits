# Grid Propagation

> 问题域：网格扩散与延迟提交 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

火海、导电、洪水、毒雾和区域连锁都可能形成环。递归遍历既容易重复结算，也可能无限扩散。本 Kit 提供显式队列、去重、访问预算和下一帧提交队列。

## 模式与算法

- **BFS / Flood Fill**：从起点逐层扩展可达格。
- **Queue + HashSet**：显式待访问集合和重复抑制。
- **双缓冲**：本帧计划的变化在 `Swap` 后统一交给下一轮处理。
- 这是数据结构/算法层，不是 GoF 模式。

## 公开接口

- `GridPoint`：整数二维坐标。
- `DeferredCellQueue`：提交、去重、翻帧。
- `BoundedFloodFill<TNode>`：可复用 scratch 缓冲、邻接访问器、访问预算。
- `FloodResult`：实际访问数和是否因预算截断。

## 必须保持的契约

- 每个可达节点最多访问一次。
- 邻接顺序由调用方定义，因此遍历顺序可复现。
- 达到预算立即停止，不把环变成无限循环。
- 被阻挡的起点不会触发访问。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Logic/Grid/TileChainReactor.cs` 与 `TileTickQueue.cs`。原作还包含地块状态、伤害和表现事件；本 Kit 只保留可复用遍历与提交机制。

对应案例：[M05 网格与地块](../../../case-studies/deepseaoil/modules/M05-grid.md)、[M07 元素与连锁](../../../case-studies/deepseaoil/modules/M07-elements.md)。

## 验证

```powershell
dotnet test kits/gameplay/grid-propagation/tests/GameDevKit.GridPropagation.Tests.csproj
```
