# M02 数据与配置

## 解决什么问题

策划数值、程序调参、生成代码和运行逻辑若互相直接依赖，一次导表字段变化会波及大量业务代码。

## 结构

Excel + Luban 是内容真源；ScriptableObject 承载手感调参；`ConfigModule` 是数据层唯一取值门面；`*Spec` 把生成行转换成上层稳定业务视图。

## 模式与思想

- **Facade**：收敛配置加载与查询。
- **Adapter / 防腐层**：Spec 隔离生成表结构。
- **单一真源、数据驱动、DIP**：同一参数只维护一处，上层依赖稳定契约。

## 可复用精华

[Reaction Rules](../../../kits/gameplay/reaction-rules/README.md) 抽取“配置数据 + O(1) 查表 + 显式兜底”的通用骨架。

## 项目专属

Luban、Excel 下拉框、镜像发布目录和具体 13 张表属于原工具链，不应复制进通用 Kit。

## 源码

`Assets/Scripts/Data/Config/ConfigModule.cs`、`Spec/*.cs`、`ConfigWorkspace/`。
