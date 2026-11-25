# M07 元素与连锁

## 解决什么问题

水、土、火、电、冰、植物的组合若写成连续数值模拟，玩家难理解、开发难调试。项目选择离散查表和首跳当帧。

## 结构

一级反应按 `(原格地貌, 投掷物)` 查表；二级反应判断发生器相邻或导通；连锁按导通/可燃属性做有界泛洪或时间波前。

## 模式与思想

- **数据驱动规则表**：规则组合是数据。
- **BFS / Flood Fill**：导电网络和可燃区域扩散。
- **预算与去重**：环形连网也必须终止。

## 可复用精华

[Reaction Rules](../../../kits/gameplay/reaction-rules/README.md)、[Grid Propagation](../../../kits/gameplay/grid-propagation/README.md)。

## 边界

具体反应表与“二级优先”业务顺序属于原项目玩法契约。通用 Kit 只提供裁决与遍历机制。

## 源码

`Logic/Element/ReactionResolver.cs`、`Logic/Grid/TileChainReactor.cs`、`Tests/Runtime/Editor/网格连锁Tests.cs`。
