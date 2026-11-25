# Composition Root

> 问题域：启动、装配与生命周期 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

对象创建、初始化顺序、每帧驱动和销毁顺序散落在多个组件后，调试启动失败会变成猜顺序。组合根把装配和生命周期集中到明确入口。

## 模式与原则

- **Composition Root**：进程或场景入口负责创建对象并注入依赖。
- **手动 DI**：构造函数和显式参数优先，不引入容器。
- **生命周期编排**：所有服务由统一 Pipeline 按 Order 启动、Tick、逆序停止。
- 不把组合根变成业务规则仓库，也不默认采用 Service Locator。

## 公开接口

- `IRuntimeService`：Order、Start、Tick、Stop。
- `RuntimeBootstrap`：注册、启动、驱动、停止。

## 必须保持的契约

- Order 唯一，启动与 Tick 升序，停止逆序。
- 重复 Order 在装配期失败。
- 启动中途失败时，回滚已经启动的服务。
- 运行后不允许继续添加服务。

## 来自 deepseaoil 的映射

参考 `Assets/Scripts/Presentation/Root/GameRoot.cs` 与 `ISceneRoot.cs`。原作同时承担 Unity 常驻对象和场景根编排；本 Kit 去除 MonoBehaviour 与全局单例，仅保留可测试的生命周期模型。

对应案例：[M01 工程底座](../../../case-studies/deepseaoil/modules/M01-foundation.md)、[M03 运行时骨架](../../../case-studies/deepseaoil/modules/M03-runtime.md)。

## 验证

```powershell
dotnet test kits/engineering/composition-root/tests/GameDevKit.CompositionRoot.Tests.csproj
```
