# M15 存档与诊断

## 解决什么问题

直接覆盖 JSON 可能损坏旧档；读档必须区分版本、损坏与空槽；恢复顺序错误会让波次、关卡和地块互相覆盖。

## 结构

版本化 DTO 保存重建世界所需的最小事实；先写临时文件，再 Replace/Move；读档先校验，再清场并按依赖顺序回填。

## 模式与思想

- **Memento 思想**：完整世界快照。
- **原子发布**：临时文件成功后才替换正式文件。
- **Adapter**：序列化格式与业务 DTO 分离。

## 可复用精华

[Snapshot Save](../../../kits/engineering/snapshot-save/README.md)。

## 准确表述

项目借鉴 Memento 的快照/恢复思想，不是严格 Originator/Memento/Caretaker 模式。存档不是逐操作 Command Undo。

## 源码

`Logic/Services/SaveService.cs`、`Data/Save/GameSaveDTO.cs`、`Tests/Runtime/Editor/存档Tests.cs`。
