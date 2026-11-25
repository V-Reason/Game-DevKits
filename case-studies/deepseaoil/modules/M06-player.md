# M06 玩家

## 解决什么问题

输入采样、状态决策、速度控制和物理写出若分散在多处，会出现吞键、同帧重复写速度和帧率相关手感。

## 结构

输入缓冲保存短时按下沿；移动状态组决定意图；ActorLedger 在帧首读取速度、累加冲量和力，帧末只写一次。

## 模式与思想

- **State**：Idle、Move、Dash、Hurt 等行为切换。
- **输入缓冲**：不是 GoF 模式，而是游戏循环惯用法。
- **速度账本**：唯一提交点，区分冲量与持续加速度。

## 可复用精华

[Input Buffer](../../../kits/gameplay/input-buffer/README.md)、[State Machine](../../../kits/gameplay/state-machine/README.md)。

## 项目专属

具体移动曲线、吸附方向、冲刺时长和角色尺寸属于调参与产品手感，不进入通用默认值。

## 源码

`Logic/Input/InputBuffer.cs`、`Logic/Movement/ActorLedger.cs`、`Logic/Player/MoveGroup.cs`。
