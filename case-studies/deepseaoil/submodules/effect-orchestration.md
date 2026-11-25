# 特效编排

父模块：[M10 特效与反馈](../modules/M10-effects.md)

## 原项目结构

EffectModule 按 EffectId 找到 IEffectDriver；资源未就绪时记录待播请求；驱动由 EffectDriverFactory 按 DriverKind 创建；EffectHandle 携带驱动、实例 ID 和代次。

## 模式与层级

- Strategy：粒子、碎裂、高亮、震屏、HUD 等驱动。
- Simple Factory：按种类创建驱动。
- Facade：业务只调用 Play/Update/Stop。
- 句柄代次是资源与实例生命周期惯用法。

## 通用化取舍

[Effect Orchestrator](../../../kits/engineering/effect-orchestrator/README.md) 只保留路由、待播队列、句柄有效性委托和统计。Unity 实例、粒子与 ObjectPool 不进入核心。

## 保留的坑

- 待播队列必须有上限，避免资源长期缺失时无限增长。
- CleanAll 后旧句柄应失效。
- 未注册 ID 必须出声，不能落成无声 default。
