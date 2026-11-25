# 音频仲裁

父模块：[M14 音频](../modules/M14-audio.md)

## 原项目结构

AudioManager 维护最多 16 个 SFX voice，同素材在播时跳过；长尾一次性音效截到 1.5 秒并淡出；BGM 单独 AudioSource，支持暂停恢复、淡入淡出；UI Blip 按 55ms 节流并做三档轻变调。

## 模式与层级

- Strategy：Kit 将“voice 满时如何处理”抽象为准入策略。
- Facade：音频请求统一进入管理器。
- 队列、voice 池与淡化是工程惯用法。

## 通用化取舍

[Audio Arbitration](../../../kits/engineering/audio-arbitration/README.md) 不持有 AudioSource，只管理逻辑 voice、时长、去重、抢占、淡化和 UI 节流。Unity AudioSource 由适配层播放。

## 保留的坑

- 长静音尾会占满 voice，时长上限必须显式。
- 同素材高频触发会制造无意义重叠。
- 暂停场景中的 UI 音必须使用未缩放时间。
