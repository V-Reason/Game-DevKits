# Audio Arbitration

> 问题域：音频 voice 仲裁 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

高频音效可能占满播放器、重复播放同一素材或留下长静音尾。BGM 与 UI 音还需要独立的淡化和节流规则。

## 模式与层级

- **Strategy**：`IAudioAdmissionPolicy` 决定 voice 满时拒绝或抢占。
- 对象池与队列是工程惯用法，承载实际播放器。
- `AudioArbiter` 只管理逻辑 voice，不引用 AudioSource。

## 公开接口

- `AudioCue<TClip>`：Clip、通道、优先级、时长、去重选项。
- `AudioArbiter<TClip>`：TryPlay、Tick、Stop、StopAll、FadeMultiplier、Stats。
- `RejectWhenFullPolicy` / `StealLowestPriorityPolicy`。
- `AudioFader` 与 `UiBlipLimiter`。

## 必须保持的契约

- 活跃 voice 达到上限时执行显式准入策略。
- 默认拒绝重复 Clip，除非请求允许重复。
- 超过最大时长截断，并在尾段给出 0..1 淡化系数。
- 活跃 voice 到期自动释放。
- UI Blip 按最小间隔节流，Pitch 保持在有限范围内。

## deepseaoil 映射

参考 `Presentation/Audio/AudioManager.cs` 与 `SfxPlayback.cs`。本 Kit 将 AudioSource 播放替换为抽象 voice 状态，并补上可替换抢占策略。

对应深挖：[音频仲裁](../../../case-studies/deepseaoil/submodules/audio-arbitration.md)。

## 验证

```powershell
dotnet test kits/engineering/audio-arbitration/tests/GameDevKit.AudioArbitration.Tests.csproj
```
