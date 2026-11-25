# Steering & Path Following

> 问题域：流场航点跟随与转向平滑 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

直接追流场下一步会抖动：采样点频繁切换、拐角满速冲过头、卡住时一直顶墙、多个同伴挤进同一狭窄路径。

## 模式与算法

- **Strategy**：`IPathField` 提供可替换方向场。
- 固定前视、航点保持、拐点扫描和直线捷径。
- `SteeringGate` 限制单帧转角并保持前半平面。
- `SeparationResolver` 用同伴体半径和可站立探针做软分离。

## 公开接口

- `IPathField`：版本、采样位置、主/次下一步、点可站立。
- `PathFollower`：TryResolve、jamming 换步、前视和捷径。
- `SteeringGate`：Step、Reset。
- `SeparationResolver` 与 `SteeringBody`。

## 必须保持的契约

- 路径版本变化后旧航点失效。
- 拐点在扫描距离内时报告距离，供刹车使用。
- 直线捷径被阻挡时退回下一节点中心。
- 卡死模式使用次优邻节点，不继续沿首选方向顶墙。
- 转向输出不落到速度后半平面。

## deepseaoil 映射

参考 `Logic/Actor/EnemyPathFollower.cs`、`EnemyNavigator.cs` 和 `EnemySteeringGate.cs`。本 Kit 用通用 `IPathField` 代替 `NavField`，不依赖现有 Flow Field Kit。

对应深挖：[路径跟随与转向](../../../case-studies/deepseaoil/submodules/steering-following.md)。

## 验证

```powershell
dotnet test kits/gameplay/steering-path-following/tests/GameDevKit.SteeringPathFollowing.Tests.csproj
```
