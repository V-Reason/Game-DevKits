# Flow Field

> 问题域：多单位共享寻路 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

大量敌人追同一目标时，为每只敌人重复跑 A* 会反复计算相同地图。流场从目标反推全图方向，让所有单位共享一次搜索成本。

## 模式与算法

- **BFS**：计算以目标为源的离散最短步数。
- **Dijkstra**：按地形步价构建加权方向场。
- **缓存失效**：只有目标、阻挡或地形版本变化才重建。
- **局部 DOD**：平行数组、复用工作缓冲、从权重网格读取数据。
- `IFlowCostPolicy` 是 Strategy 的最小用法：外部选择步价策略。

## 公开接口

- `FlowGrid`：尺寸、阻挡、地形版本和步价策略。
- `FlowField`：距离、加权代价、下一格。
- `FlowFieldCache`：按目标与地形版本复用场。
- `IFlowCostPolicy` / `UniformFlowCostPolicy`：可替换步价。

## 必须保持的契约

- 不可达格距离为 `-1`，代价为 `int.MaxValue`。
- 元素/地形偏好不能无限绕路，受 `MaxDetourSteps` 限制。
- 目标不变且地形版本不变时，不重复建场。
- 目标格自身没有“下一格”。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Logic/Grid/NavField.cs`、`NavFieldSet.cs` 和 `NavMap.cs`。原作包含体半径、净空采样和元素偏好；本 Kit 使用简化网格展示相同的流场与缓存思想。

对应案例：[M08 敌人与寻路](../../../case-studies/deepseaoil/modules/M08-enemies.md)。

## 不适用

单位极少、目标各不相同、地图持续剧烈变化时，A* 或局部导航通常更直接。流场收益依赖“共享目标”和“可控重建”。

## 验证

```powershell
dotnet test kits/gameplay/flow-field/tests/GameDevKit.FlowField.Tests.csproj
```
