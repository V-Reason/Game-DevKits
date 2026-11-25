# Reaction Rules

> 问题域：数据驱动规则裁决 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

水碰火、土碰火、技能组合、伤害克制等规则若写成层层 `switch`，新增组合就必须修改裁决代码。本 Kit 用复合键规则表把“配置数据”与“查询算法”分开。

## 模式与原则

- **数据驱动规则表**：`(Left, Right) -> Result`，装配期建索引，运行期 O(1) 查询。
- 体现 **OCP** 的问题边界：新增数据行不改查询路径；新增“基础概念”仍可能需要代码。
- 不用 Strategy 作为主标签，因为这里替换的是数据，不是可互换算法对象。

## 公开接口

- `RuleKey<TLeft,TRight>`：类型安全的复合键。
- `RuleEntry<TLeft,TRight,TResult>`：一条完整规则。
- `RuleCatalog<TLeft,TRight,TResult>`：初始化、命中查询、显式 Fallback。

## 必须保持的契约

- 重复复合键在初始化时抛错，不允许后者静默覆盖前者。
- 未命中时可走调用方显式提供的 Fallback；没有 Fallback 则抛 `KeyNotFoundException`。
- 配置在运行时被视为只读。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Logic/Element/ReactionResolver.cs` 和 `Assets/Scripts/Data/Config/Spec/ElementRuleSpec.cs`。本 Kit 去掉了地形、伤害、击退等业务字段，只保留通用裁决骨架。

对应案例：[M02 数据与配置](../../../case-studies/deepseaoil/modules/M02-data.md)、[M07 元素与连锁](../../../case-studies/deepseaoil/modules/M07-elements.md)。

## 验证

```powershell
dotnet test kits/gameplay/reaction-rules/tests/GameDevKit.ReactionRules.Tests.csproj
```
