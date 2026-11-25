# 波次调度

父模块：[M09 关卡与流程](../modules/M09-levels.md)

## 原项目结构

WaveLogic 管理 Prep/Battle/Settle、阶段计时、敌种展开、出生间隔、存活阻断、顺序/循环和存档游标；单次 Tick 限制最多三次阶段转移。

## 模式与层级

- State：三个波次阶段。
- Snapshot 思想：阶段、计时、出生游标进入存档。
- 队列：对外输出 SpawnRequest。

## 通用化取舍

[Wave Sequencer](../../../kits/gameplay/wave-sequencer/README.md) 用泛型敌人与 Grant 替换具体 EnemyId/SeedType，移除 EventBus，阶段变化与出生由调用方拉取。

## 保留的坑

- 不能只看计时结束，必须检查所有敌人已生成且已清场。
- 零时长阶段需要单帧转移上限。
- 读档恢复后不能重复发准备期 Grant。
