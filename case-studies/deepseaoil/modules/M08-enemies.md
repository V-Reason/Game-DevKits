# M08 敌人与寻路

## 解决什么问题

大量敌人追同一玩家时，为每只敌人单独跑 A* 会重复搜索。角色半径、窄道、动态阻挡和元素偏好又要求可通行判断稳定。

## 结构

净空图按子格采样到障碍的距离；流场从目标格反推下一步；地形与目标版本控制重建；敌人读取共享场，再叠加软分离和卡死换步。

## 模式与思想

- **BFS**：可达步数和父指针。
- **Dijkstra 思想**：元素权重影响成本。
- **流场**：搜索成本按地图面积向多个单位摊销。
- **局部 DOD**：平行数组、复用队列和堆。
- `IEnemyBrain` 是 Strategy。

## 可复用精华

[Flow Field](../../../kits/gameplay/flow-field/README.md)。

## 边界

项目不是 ECS。流场适合“共享目标 + 地图变化可控”；目标频繁分化时应重新选算法。

## 源码

`Logic/Grid/NavMap.cs`、`NavField.cs`、`NavFieldSet.cs`、`Logic/Actor/EnemyBrain.cs`。
