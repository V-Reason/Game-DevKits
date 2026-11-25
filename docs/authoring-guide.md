# Kit 编写规范

## 目录模板

```text
kits/<domain>/<kit>/
├── README.md
├── src/
│   ├── GameDevKit.<Kit>.csproj
│   └── <source files>
└── tests/
    ├── GameDevKit.<Kit>.Tests.csproj
    └── <behavior tests>
```

## README 必填内容

- 问题域、实现状态和证据等级。
- 解决什么问题。
- 模式/算法所属层级。
- 公开接口与最小用法。
- 必须保持的契约。
- 来自哪个真实项目，哪些是重写。
- 不适用场景。
- 独立测试命令。

## 代码约束

- 类库目标为 `netstandard2.1`，测试目标为 `netstandard2.1` 兼容环境之外的 `net8.0`。
- Kit 源码不得引用 `UnityEngine`、`UnityEditor` 或其它 Kit。
- 公开 API 使用英文标识，注释解释契约、边界和静默失败，不复述代码。
- 失败语义必须显式：返回 `false`、抛异常或进入 Fallback，三者不混用。
- 先写失败测试，再实现行为；每轮至少运行目标测试。

## 分类标签

标签必须带层级：

```text
架构：Composition Root
原则：DIP、OCP
GoF：Observer、State
惯用法：对象池、双缓冲
算法：BFS、Dijkstra、时间轮
```

## 完成标准

- 目标测试从红到绿。
- 全解决方案测试通过。
- Kit 可单独构建。
- README 没有悬空链接。
- `tools/verify.ps1` 退出码为 0。
