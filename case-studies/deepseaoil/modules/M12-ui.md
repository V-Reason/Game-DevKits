# M12 界面与 HUD

## 解决什么问题

HUD 若自己保存生命、弹药、波次等事实，就会成为第二份游戏状态，并与逻辑状态逐渐不一致。

## 结构

逻辑层发布事实事件；面板订阅并刷新视图；UIMgr 统一面板显隐和关闭最上层；UI 不拥有业务真值。

## 模式与思想

- **Observer**：视图响应事实。
- **MVC/MVVM 分离思想**：业务状态、输入协调和显示分离。
- **Facade**：UIMgr 收敛面板操作。

## 可复用精华

复用 [Event Bus](../../../kits/engineering/event-bus/README.md)。不要为每个小面板强行增加 Controller/Presenter 转发层。

## 准确表述

项目不是严格 MVC。面板自行订阅并刷新，更接近弱 MVC/MVVM 混合。

## 源码

`Presentation/UI/UIMgr.cs`、`BasePanel.cs`、`Panel/*.cs`。

## 二级拆解

- [UI 栈与生命周期](../submodules/ui-stack.md)
