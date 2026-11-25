# Game DevKit Unity Adapters

目标：Unity 2022.3 LTS。该包只提供薄适配，不包含游戏资产或 Kit 源码。

## 内容

- `UnityRuntimeDriver`：把 Unity 的 `Update` / `FixedUpdate` 转为显式事件。
- `UnityTimeSource`：统一提供缩放、未缩放、固定和真实时间。
- `UnityVectorAdapter`：`UnityEngine.Vector2` 与 `System.Numerics.Vector2` 互转。
- `Samples~/CoreIntegrations/`：Asset、UI、Motion、Path 与 Audio 的接入样例；导入前需先加入对应 Kit。

## 本地导入

1. 在测试工程的 `Packages/manifest.json` 中加入本地包路径，例如：

```json
"com.gamedevkit.unity": "file:../../Game-DevKits/adapters/unity/com.gamedevkit.unity"
```

2. 等待 Unity 编译。
3. 创建空物体并挂 `UnityRuntimeDriver`。
4. 订阅 `FrameTick` 和 `PhysicsTick`，不要在业务组件中继续散落 `Update`。

## 核心 Kit 接入

`Samples~` 默认不编译。需要某个适配时，先加入对应 Kit 的 `src/` 源码，再把样片导入 `Assets/`。
具体依赖见 `Samples~/CoreIntegrations/README.md`。

## 人工验收

- Unity 2022.3 控制台 0 error。
- 运行时帧与物理帧事件按顺序到达。
- 暂停时 `DeltaTime` 为 0，`UnscaledDeltaTime` 继续。
- 双向 Vector2 转换保持 x/y 数值。

本仓库当前环境没有 Unity 编辑器，因此这些项目完成的是源码与静态契约验证，不能替代 Unity 内实际编译和 Play Mode 验证。
