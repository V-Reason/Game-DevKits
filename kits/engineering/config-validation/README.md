# Config Validation

> 问题域：配置清单、索引与启动校验 · 实现状态：可运行 · 来源证据：adapted

## 解决什么问题

生成表或配置对象最容易出现“表没加载、主键重复、引用缺失”这类静默故障。启动阶段应一次收集全部问题，并让业务只接触稳定视图。

## 模式与层级

- **Facade**：Config Manifest 与 Pipeline 收敛启动校验。
- **Adapter**：`IValueAdapter` 将生成/原始行转换为业务 Spec。
- 多个 Validator 组成聚合校验管线，不采用 fail-fast。

## 公开接口

- `ConfigManifest`：必需表清单和缺失检查。
- `TableIndex<TKey,TValue>`：重复键拒绝和稳定查询。
- `ConfigValidationPipeline<TConfig>`：聚合 Validator。
- `ConfigValidationReport` / `ConfigValidationIssue`。
- `IValueAdapter<TSource,TView>` / `DelegateValueAdapter`。

## 必须保持的契约

- 重复主键在装配期报错。
- Validator 异常转为 Error 问题，不中断后续 Validator。
- 缺失表与缺失引用以可读路径报告。
- 上层只持有 Adapt 后的视图，不直接依赖源结构。

## deepseaoil 映射

参考 `Data/Config/ConfigModule.cs`、`StartupValidator.cs`、`TablesMeta.cs` 与 `Spec/*.cs`。Luban 与具体表名不属于本 Kit。

对应深挖：[配置校验与 Spec](../../../case-studies/deepseaoil/submodules/config-validation.md)。

## 验证

```powershell
dotnet test kits/engineering/config-validation/tests/GameDevKit.ConfigValidation.Tests.csproj
```
