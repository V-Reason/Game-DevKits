# Core Integrations

这些样片依赖对应核心 Kit。UPM 不会自动编译 `Samples~` 中的文件：

1. 先把对应 Kit 的 `src/` 源码加入 Unity 工程。
2. 导入所需样片到 `Assets/`。
3. 确保核心类型所在命名空间可被适配文件访问。

| 样片 | 依赖 |
| --- | --- |
| `ResourcesAssetSource.cs` | `GameDevKit.AssetLifecycle` |
| `CanvasPanelView.cs` | `GameDevKit.UiStack` |
| `RigidbodyMotionMotor.cs` | `GameDevKit.ActorMotion` |
| `DelegatePathField.cs` | `GameDevKit.SteeringPathFollowing` |

`UnityAudioOutput.cs` 不依赖核心类型，可作为 `AudioArbiter` 的播放后端。
