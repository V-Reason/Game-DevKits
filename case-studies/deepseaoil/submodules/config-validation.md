# 配置校验与 Spec

父模块：[M02 数据与配置](../modules/M02-data.md)

## 原项目结构

ConfigModule 在启动时加载 Tables、抽样验证表属性、绑定 SO，并在装配期对关键表执行一次查询；各 `*Spec` 把生成行转换成逻辑层稳定契约。

## 模式与层级

- Facade：ConfigModule。
- Adapter：生成行到 Spec。
- 校验管线：启动期一次报告多个问题。

## 通用化取舍

[Config Validation](../../../kits/engineering/config-validation/README.md) 用 ConfigManifest、TableIndex、Validator Pipeline 和 IValueAdapter 表达这些职责，不绑定 Luban 或 Excel。

## 保留的坑

- 表清单手写时必须与生成物属性一致。
- 重复主键不能后写覆盖。
- Validator 异常不能吞掉后续校验。
