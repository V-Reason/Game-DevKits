# UI 栈与生命周期

父模块：[M12 UI 与 HUD](../modules/M12-ui.md)

## 原项目结构

UIMgr 按五层维护面板栈；异步加载面板合并同面板请求；面板可 Hide/Destroy；返回键关闭最上层，遇到不可关闭面板停止；场景切换与游戏状态通过事件驱动面板变化。

## 模式与层级

- Facade：UIMgr 收敛面板操作。
- 分层栈：System、Top、Middle、Bottom、Transition。
- 生命周期接口：Show、Hide、Dispose。
- 不宣称严格 MVC。

## 通用化取舍

[UI Stack](../../../kits/engineering/ui-stack/README.md) 只保留通用面板 ID、异步 Loader、层栈和关闭语义，移除 UGUI、TMP、字体、场景切换和具体面板。

## 保留的坑

- 重复 Open 不能在加载完成后压两次栈。
- 加载中关闭要取消显示并释放实例。
- 不可关闭遮罩必须阻断返回，而不是继续关下层。
