# M05 网格与地块

## 解决什么问题

同帧多个反应可能同时改写地块；连锁与持续时间又会让状态更新再次触发状态更新。

## 结构

地块由状态对象表达；变化先进入双缓冲 Tick 队列，下一逻辑帧统一裁决；状态切换使用两阶段仲裁。

## 模式与思想

- **State**：地块行为随当前状态变化。
- **双缓冲**：本帧写、下帧读，避免重入。
- **队列/BFS**：连锁扩散使用显式容器。

## 可复用精华

[State Machine](../../../kits/gameplay/state-machine/README.md)、[Grid Propagation](../../../kits/gameplay/grid-propagation/README.md)。

## 边界

“有状态机基础设施”不等于实现了任意深度 HSM。业务通过多个状态组和局部规则形成受控层次行为。

## 源码

`Logic/Grid/GridLogic.cs`、`TileTickQueue.cs`、`TileStateMachine.cs`、`States/TableTileState.cs`。
