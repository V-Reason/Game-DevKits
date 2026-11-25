# 运动账本与门禁

父模块：[M06 玩家](../modules/M06-player.md)

## 原项目结构

ActorLedger 帧首读取速度，帧内累加冲量与力，帧末一次写回；StatusGroup 产出麻痹/受击/减速门禁；门禁优先级决定最终速度来源。

## 模式与层级

- 游戏循环惯用法：BeginStep → submissions → Commit。
- 门禁优先级：麻痹 > 受击强制速度 > 减速乘数。
- 是角色控制律，不强行贴 GoF 名称。

## 通用化取舍

[Actor Motion](../../../kits/gameplay/actor-motion/README.md) 改用 `System.Numerics.Vector2`，修复原作中反向转向分支未按加速度限幅的问题，并把状态门禁与账本放在同一可测试 Kit。

## 保留的坑

- 冲量不能乘 Δt，持续力必须乘 Δt。
- 速度乘数只影响目标速度，不覆盖受击外力。
- 门禁是每帧合同，不能永久缓存。
