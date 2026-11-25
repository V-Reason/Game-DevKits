# Snapshot Save

> 问题域：存档版本与原子写入 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

直接覆盖存档文件时，写入中断可能同时毁掉旧档和新档。版本升级、损坏数据和非法槽名也需要可诊断的失败路径。

## 模式与原则

- **Memento 思想**：保存重建世界所需的最小事实快照，但不宣称严格 Originator/Memento/Caretaker 三角关系。
- **Adapter**：序列化由 `ISnapshotCodec<T>` 注入，文件系统由 `ISnapshotStore` 隔离。
- **原子发布**：写临时文件，成功后再 Replace/Move。

## 公开接口

- `ISnapshotCodec<T>`：业务序列化器。
- `ISnapshotStore`：槽读取、写入、删除。
- `AtomicFileSnapshotStore`：同目录临时文件 + 原子替换。
- `SnapshotService<T>`：帧头版本校验 + Codec。
- `Utf8StringCodec`：最小示例 Codec。

## 必须保持的契约

- 不支持版本在解码前失败。
- 损坏载荷不得返回半成品状态。
- 槽名仅允许字母、数字、点、下划线和连字符，不允许路径分隔符。
- 写入失败清理临时文件，已存在的有效文件保持不变。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Logic/Services/SaveService.cs` 与 `Assets/Scripts/Data/Save/GameSaveDTO.cs`。原作保存玩家、关卡、波次、地块、敌人、掉落等业务事实；本 Kit 只抽取版本化、Codec 注入和原子文件机制。

对应案例：[M15 存档与诊断](../../../case-studies/deepseaoil/modules/M15-save.md)。

## 验证

```powershell
dotnet test kits/engineering/snapshot-save/tests/GameDevKit.SnapshotSave.Tests.csproj
```
