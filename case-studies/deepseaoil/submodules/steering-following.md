# 路径跟随与转向

父模块：[M08 敌人与寻路](../modules/M08-enemies.md)

## 原项目结构

EnemyPathFollower 使用固定前视、航点保持、拐点扫描和直线捷径；EnemySteeringGate 限制单帧翻面；EnemyNavigator 叠加入弯降速、软分离和卡死换步。

## 模式与层级

- Strategy：流场/路径提供者。
- 控制算法：前视、拐点、直线可行性和 separation。
- 卡死换步是局部恢复策略。

## 通用化取舍

[Steering & Path Following](../../../kits/gameplay/steering-path-following/README.md) 用 `IPathField` 编译独立，不直接依赖 Flow Field Kit。需要复用时通过适配器连接两者。

## 保留的坑

- 航点频繁切换会产生采样点级抖动。
- 只看前视点算刹车距离会冲过头。
- 分离方向必须做可站立探针，不能把同伴推进墙。
