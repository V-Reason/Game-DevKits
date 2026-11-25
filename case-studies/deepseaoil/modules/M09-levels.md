# M09 关卡与流程

## 解决什么问题

波次的准备、战斗、结算，以及关卡的启动、运行、胜利、失败必须按顺序推进；重复结算和事件乱序会让胜负不可复现。

## 结构

关卡和波次各自持有明确状态与计时；存活数从权威列表重算；阶段变化通过强类型事实通知表现。

## 模式与思想

- **State 思路**：行为随阶段变化。
- **事件顺序**：顺序也是状态的一部分，不能靠多个组件自行猜测。
- 首版没有独立代码 Kit，复用 [State Machine](../../../kits/gameplay/state-machine/README.md) 和 [Event Bus](../../../kits/engineering/event-bus/README.md)。

## 项目专属

具体波次表、敌人配额、关卡编号和剧情触发点属于内容设计，不应通用化。

## 源码

`Logic/Wave/WaveLogic.cs`、`Logic/Level/LevelLogic.cs`、`Tests/Runtime/Editor/WaveLogicTests.cs`。
