# deepseaoil 案例研究

本项目把《荒原复苏指南 / deepseaoil》当作“从真实项目提炼通用范式”的首个案例。案例材料验证问题来源、源码结构和设计取舍，但可运行 Kit 使用中性命名原创重写。

## 证据基线

- 仓库分支：`VReason/Update`
- 源码提交：`254b9cf`
- 编译门历史结果：0 error / 0 warning
- 复盘材料：2026-11-23 的开发者技术复盘及 v1/v2/v3 源稿

## 阅读入口

1. [模块总图](diagrams/overview.mmd)
2. [源码地图](source-map.md)
3. [模式证据账本](pattern-ledger.md)
4. 15 个模块卡：
   - [M01 工程底座](modules/M01-foundation.md)
   - [M02 数据与配置](modules/M02-data.md)
   - [M03 运行时骨架](modules/M03-runtime.md)
   - [M04 战斗总装](modules/M04-combat.md)
   - [M05 网格与地块](modules/M05-grid.md)
   - [M06 玩家](modules/M06-player.md)
   - [M07 元素与连锁](modules/M07-elements.md)
   - [M08 敌人与寻路](modules/M08-enemies.md)
   - [M09 关卡与流程](modules/M09-levels.md)
   - [M10 特效与反馈](modules/M10-effects.md)
   - [M11 光照](modules/M11-lighting.md)
   - [M12 界面与 HUD](modules/M12-ui.md)
   - [M13 剧情与对话](modules/M13-story.md)
   - [M14 音频](modules/M14-audio.md)
   - [M15 存档与诊断](modules/M15-save.md)

## 证据等级

- `verified`：在固定提交的源码中能定位到对应角色关系或算法。
- `adapted`：问题与核心结构来自原项目，但代码和接口为本仓库重写。
- `concept-only`：复盘或设计说明中有明确目标，但不应夸大为完整现成实现。

## 不在本案例中的内容

- 原项目业务数值、剧情文本、配表、Prefab、Shader 和美术资源。
- 对外部课程材料的复制。术语只做对齐，不重新分发课件正文。
- “整个项目采用 ECS”“全工程 0 GC”“完整任意深度 HSM”等过度声明。
