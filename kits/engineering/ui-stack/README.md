# UI Stack

> 问题域：面板栈与异步生命周期 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

面板可能异步加载、重复打开、加载完成前又被关闭；多个层级需要“关闭最上层”；遮罩型面板必须阻止返回键继续穿透。

## 模式与层级

- **Facade + 分层栈**：业务只提交 Open/Close，不管理加载协程。
- **生命周期接口**：Show、Hide、Dispose。
- 不宣称严格 MVC；Panel 的模型与视图连接由使用方决定。

## 公开接口

- `UiPanelStack<TPanelId,TPanel>`：Open、Close、CloseTopmost、CloseAll。
- `IUiPanelLoader<TPanelId,TPanel>`：异步加载器。
- `UiPanelSpec<TPanelId>`：层、是否允许返回关闭。
- `IUiPanel` 与 `UiPanelState`。

## 必须保持的契约

- 同面板并发 Open 合并为一次加载。
- 加载中关闭时，加载完成后不显示并立即 Dispose。
- 返回关闭按 System → Overlay → Standard → Background → Transition。
- 最上层不可关闭时停止，不继续关下层。
- CloseAll 按层级和栈顶逆序调用 Hide。

## deepseaoil 映射

参考 `Presentation/UI/UIMgr.cs` 与 `BasePanel.cs`。本 Kit 移除 UGUI/TMP 类型，只保留面板状态机、层栈和加载生命周期。

对应深挖：[UI 栈与生命周期](../../../case-studies/deepseaoil/submodules/ui-stack.md)。

## 验证

```powershell
dotnet test kits/engineering/ui-stack/tests/GameDevKit.UiStack.Tests.csproj
```
