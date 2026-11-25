# 投掷与瞄准

父模块：[M06 玩家](../modules/M06-player.md)

## 原项目结构

ProjectileTrajectory 将逻辑落点保持在贴地直线，只把弧高加到视觉采样；WeaponAimController 使用椭圆身体轴、透视挤压、原始角层级和距离相关抬升；玩家移动支持八向吸附。

## 模式与层级

- 纯数学算法：抛物线、角度、椭圆、透视缩放。
- 八向吸附是输入处理惯用法。
- 无 GoF 模式，不需要为了模式而模式。

## 通用化取舍

[Ballistics & Aim](../../../kits/gameplay/ballistics-aim/README.md) 使用 `System.Numerics.Vector2` 和注入函数替代 AnimationCurve，同时保留四组独立数学契约。

## 保留的坑

- 最近投掷距离为 0 时不能归一化零向量。
- 视觉高度不能改变真实落点。
- 层级判定不能使用视觉抬升角。
- 垂直瞄准时抬升必须归零。
