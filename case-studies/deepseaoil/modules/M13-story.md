# M13 剧情与对话

## 解决什么问题

剧情段、段落、台词、排队触发和存档恢复若只靠全局布尔值，会重复播放或读档后丢进度。

## 结构

剧情流程以段和行下标表达；已播标记和待播队列进入存档；阶段变化由游戏事件触发。

## 模式与思想

- **State 思路**：剧情推进由当前位置和完成条件决定。
- **数据驱动**：对白内容来自表。
- 不是独立解释器或技能 DSL。

## 可复用精华

复用 [State Machine](../../../kits/gameplay/state-machine/README.md) 与 [Snapshot Save](../../../kits/engineering/snapshot-save/README.md)。

## 项目专属

段号、角色、台词和关卡触发点属于内容。

## 源码

`Presentation/Story/StoryFlow.cs`、`StorySession.cs`、`Presentation/UI/Panel/DialoguePanel.cs`。
