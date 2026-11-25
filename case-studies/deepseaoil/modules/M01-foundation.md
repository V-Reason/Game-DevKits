# M01 工程底座

## 解决什么问题

把“只靠口头约定”的层级边界变成编译失败，并为编译、注释、导表提供可重复的质量门。

## 结构

`Foundation -> Data -> Logic -> Presentation` 四层程序集单向引用。Foundation 是叶子；Presentation 是唯一可以看见全部业务层的组合层。

## 模式与思想

- **分层架构**：架构风格，不是 GoF“分层模式”。
- **DIP**：高层依赖窄端口，而不是生成表或 Unity 实现。
- **ISP**：逻辑层使用窄接口，例如移动、伤害和事件消费者。

## 可复用精华

[Composition Root](../../../kits/engineering/composition-root/README.md) 展示如何把生命周期和依赖显式化。

## 不应直接复用

`compile-gate.ps1` 的逻辑与原 Unity 工程、Luban 镜像和 asmdef 名称绑定。通用原则可借鉴，脚本不能直接搬走。

## 源码

`AGENTS.md`、`Docs/工程/架构约束.md`、四个核心 `.asmdef`、`Tools/compile-gate.ps1`。
