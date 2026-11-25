# M14 音频

## 解决什么问题

战斗高频事件可能同时请求大量音效，若每次创建播放器会分配并互相盖住；BGM、SFX 和 UI 音还需要不同优先级与暂停规则。

## 结构

AudioManager 统一接收音频 ID，按播放器池、并发上限、优先级和冷却选择是否播放；资源路径由配置表提供。

## 模式与思想

- **Facade**：业务只提交音频意图。
- **对象池**：复用播放器实例。
- **优先级/队列**：高频事件不能全部无条件播放。

## 可复用精华

首版复用 [Object Pool](../../../kits/engineering/object-pool/README.md) 与 [Timer Wheel](../../../kits/engineering/timer-wheel/README.md)，独立音频仲裁 Kit 留给后续版本。

## 边界

具体响度、并发和 BGM 曲目属于产品与资源策略。

## 源码

`Presentation/Audio/AudioManager.cs`、`SfxPlayback.cs`、`Assets/Resources/audio/`。
