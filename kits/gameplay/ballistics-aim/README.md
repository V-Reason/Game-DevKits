# Ballistics & Aim

> 问题域：投掷轨迹与俯视角瞄准 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

投掷距离需要最小/最大夹取；视觉抛物线不能改变地面逻辑落点；八向移动必须归一化；侧挂武器在俯视角下需要椭圆身体轨道、透视挤压和前后层级。

## 模式与算法

- 抛物线、角度归一化和椭圆轨道都是纯数学算法。
- 本 Kit 不引入 Unity AnimationCurve，抬升曲线由 `Func<float,float>` 注入。

## 公开接口

- `ThrowSettings` / `BallisticArc`：距离夹取、时长、地面/视觉采样。
- `EightWayDirection.Snap`：零向量安全、单位化、八向吸附。
- `AimSettings` / `AimSolution` / `AimSolver`。

## 必须保持的契约

- 零距离默认向右，不产生 NaN。
- 逻辑落点始终贴地；高度只进入视觉采样。
- 八向关闭时仍归一化，开启时输出固定八方向。
- 层级只看原始瞄准角，视觉抬升不参与前后判断。
- 正上/正下瞄准时视觉抬升归零。

## deepseaoil 映射

参考 `Foundation/Ballistics.cs`、`Logic/Projectile/ProjectileTrajectory.cs`、`Presentation/Combat/WeaponAimController.cs` 和 `WeaponAimTests.cs`。

对应深挖：[投掷与瞄准](../../../case-studies/deepseaoil/submodules/ballistics-aim.md)。

## 验证

```powershell
dotnet test kits/gameplay/ballistics-aim/tests/GameDevKit.BallisticsAim.Tests.csproj
```
