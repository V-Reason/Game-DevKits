# M10 特效与反馈

## 解决什么问题

同一个“爆炸”事实可能需要粒子、震屏、音效、HUD 闪烁和光照脉冲。把每种表现写成业务分支会让逻辑层爆炸。

## 结构

EffectModule 接收效果 ID 和上下文，按 Catalog 找到 IEffectDriver；资源懒加载，实例通过对象池生命周期管理。

## 模式与思想

- **Strategy**：不同 IEffectDriver 实现播放与更新。
- **Simple Factory**：按 DriverKind 创建驱动。
- **Facade**：调用方不需要知道资源加载、实例池和驱动细节。

## 可复用精华

首版复用 [Event Bus](../../../kits/engineering/event-bus/README.md) 和 [Object Pool](../../../kits/engineering/object-pool/README.md)。独立 Effect Strategy Kit 留给后续版本。

## 边界

简单工厂不是 Factory Method；表现驱动不应反向写业务状态。

## 源码

`Presentation/Effects/EffectModule.cs`、`IEffectDriver.cs`、`Drivers/EffectDriverFactory.cs`。

## 二级拆解

- [特效编排](../submodules/effect-orchestration.md)
