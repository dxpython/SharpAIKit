# SharpAIKit v0.3.0 Release Notes

**发布日期**: 2024-12-25  
**版本**: 0.3.0

## 🎉 重大更新

SharpAIKit v0.3.0 引入了全面的架构改进，使框架更加强大、类型安全、模块化，并提供了企业级的可观测性和容错能力。

## ✨ 新功能

### 🏗️ 核心架构改进

#### 1. 强类型上下文 (StrongContext)
- ✅ 类型安全的数据传递，编译时检查
- ✅ 支持泛型访问 `context.Get<T>()`
- ✅ 序列化/反序列化支持
- ✅ 向后兼容字典访问

```csharp
var context = new StrongContext();
context.Set("user_id", 12345);
var userId = context.Get<int>("user_id");  // 类型安全！
```

#### 2. 模块化架构
- ✅ **IPlanner**: 独立的规划器接口，负责生成执行计划
- ✅ **IToolExecutor**: 独立的工具执行器，负责工具调用
- ✅ **EnhancedAgent**: 组合所有组件的增强型 Agent
- ✅ 支持依赖注入和组件替换

```csharp
var planner = new SimplePlanner(llmClient);
var executor = new DefaultToolExecutor();
var agent = new EnhancedAgent(llmClient, planner, executor, memory);
```

#### 3. LLM 中间件系统
- ✅ **RetryMiddleware**: 自动重试失败的请求
- ✅ **RateLimitMiddleware**: 请求限流控制
- ✅ **LoggingMiddleware**: 结构化日志记录
- ✅ **CircuitBreakerMiddleware**: 熔断器模式，防止级联故障

### 🕸️ Graph 引擎增强

#### 4. 状态持久化
- ✅ **IGraphStateStore**: 状态存储接口
- ✅ **MemoryGraphStateStore**: 内存存储实现
- ✅ **FileGraphStateStore**: 文件系统存储实现
- ✅ 支持检查点保存和恢复，适合长时间运行的任务

```csharp
var store = new FileGraphStateStore("./checkpoints");
var graph = new EnhancedSharpGraph("start");
graph.StateStore = store;
graph.AutoSaveCheckpoints = true;
```

#### 5. 并行执行
- ✅ **ForkNode**: 支持将执行流分割为多个并行分支
- ✅ **JoinNode**: 等待所有分支完成并合并结果
- ✅ **JoinStrategy**: All/Any/Count 多种合并策略

```csharp
builder
    .Fork("split", "branch1", "branch2", "branch3")
    .Join("merge", JoinStrategy.All, states => MergeResults(states));
```

#### 6. 事件系统
- ✅ **OnNodeStart**: 节点开始执行时触发
- ✅ **OnNodeEnd**: 节点执行完成时触发
- ✅ **OnError**: 执行出错时触发
- ✅ **OnStreaming**: 流式输出时触发

```csharp
graph.OnNodeStart += async (sender, e) => {
    Console.WriteLine($"Node {e.NodeName} started");
};
```

### 🔧 工具和集成

#### 7. OpenAPI 工具生成
- ✅ 从 Swagger/OpenAPI 规范自动生成工具定义
- ✅ 支持从 URL 或 JSON 字符串加载
- ✅ 自动解析参数和类型

```csharp
var tools = await OpenAPIToolGenerator.GenerateFromUrlAsync(
    "https://api.example.com/swagger.json"
);
```

#### 8. OpenTelemetry 集成
- ✅ 内置分布式追踪支持
- ✅ 支持 LLM、Tool、Graph 操作追踪
- ✅ 兼容 Jaeger、Aspire 等工具

```csharp
using var activity = OpenTelemetrySupport.StartLLMActivity("Chat", model);
```

#### 9. 结构化日志
- ✅ **StructuredLogger**: 结构化日志记录类
- ✅ 记录 LLM 请求、工具执行、图节点执行
- ✅ 包含完整的元数据（模型、耗时、参数等）

### 🎨 易用性改进

#### 10. Fluent API
- ✅ 优雅的链式构建语法
- ✅ 支持条件分支和循环
- ✅ 更直观的 API 设计

```csharp
var graph = FluentGraphExtensions
    .StartGraph("start")
    .Do(async state => { /* ... */ })
    .Next("process")
    .If(state => condition, "true_path", "false_path")
    .End()
    .Build();
```

#### 11. 预置模版
- ✅ **ReAct Pattern**: 推理+行动模式
- ✅ **MapReduce Pattern**: 多文档处理模式
- ✅ **Reflection Pattern**: 自我纠错模式

```csharp
var reactGraph = GraphTemplates.CreateReActPattern(llmClient, tools);
var mapReduceGraph = GraphTemplates.CreateMapReducePattern(llmClient, documents);
var reflectionGraph = GraphTemplates.CreateReflectionPattern(llmClient);
```

## 📊 改进对比

| 功能 | v0.2.0 | v0.3.0 |
|:-----|:-------|:-------|
| 类型安全 | ⚠️ 字典传递 | ✅ StrongContext |
| 模块化 | ⚠️ 耦合度高 | ✅ 完全模块化 |
| 中间件 | ❌ 无 | ✅ 完整支持 |
| 状态持久化 | ❌ 无 | ✅ 内置支持 |
| 并行执行 | ❌ 无 | ✅ Fork/Join |
| 事件系统 | ❌ 无 | ✅ 生命周期钩子 |
| OpenAPI 工具 | ❌ 无 | ✅ 自动生成 |
| OpenTelemetry | ❌ 无 | ✅ 内置支持 |
| 结构化日志 | ❌ 无 | ✅ 内置支持 |
| Fluent API | ⚠️ 部分 | ✅ 完整支持 |
| 预置模版 | ⚠️ 无 | ✅ 3 种模式 |

## 🐛 Bug 修复

- 修复了 `RoslynCodeInterpreter` 的异步执行问题
- 修复了 `GraphState.Clone()` 方法缺失的问题
- 修复了 XML 注释格式问题

## 📚 文档更新

- ✅ 更新了 README_CN.md 和 README_EN.md
- ✅ 添加了架构改进详细文档
- ✅ 创建了 NuGet README
- ✅ 添加了测试报告

## 🧪 测试

- ✅ 所有示例项目编译通过
- ✅ 新功能测试全部通过
- ✅ 创建了 NewFeaturesDemo 测试项目

## 📦 安装

```bash
dotnet add package SharpAIKit --version 0.3.0
```

## 🔗 相关链接

- **NuGet**: https://www.nuget.org/packages/SharpAIKit
- **GitHub**: https://github.com/dxpython/SharpAIKit
- **文档**: [README_CN.md](README_CN.md) | [README_EN.md](README_EN.md)

## 🙏 致谢

感谢所有贡献者和用户的支持！

---

**完整更新日志**: 查看 [ARCHITECTURE_IMPROVEMENTS.md](docs/ARCHITECTURE_IMPROVEMENTS.md) 了解详细的技术改进。

