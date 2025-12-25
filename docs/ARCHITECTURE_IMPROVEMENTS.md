# SharpAIKit 架构改进文档

本文档详细说明了 SharpAIKit 框架的全面架构改进，旨在超越 LangChain 并充分利用 .NET 生态系统的优势。

## 📋 改进概览

### 1. 核心架构与抽象 (Architecture & Abstraction)

#### ✅ 统一上下文 (StrongContext)

**问题**: 之前 `GraphState` 和 `AgentStep` 使用 `Dictionary<string, object?>` 传递数据，类型不安全且难以维护。

**解决方案**: 引入了强类型的 `StrongContext` 对象，支持：
- **类型安全访问**: `context.Get<T>()` 和 `context.Set<T>(value)` 提供编译时类型检查
- **向后兼容**: 仍支持字典式访问 `context[key]`
- **序列化支持**: `ToJson()` 和 `FromJson()` 用于状态持久化
- **泛型上下文**: `StrongContext<T>` 用于特定类型的上下文

**示例**:
```csharp
var context = new StrongContext();
context.Set("user_id", 12345);
context.Set<UserProfile>(profile);

var userId = context.Get<int>("user_id");
var profile = context.Get<UserProfile>();
```

#### ✅ 模块化接口

**问题**: `AiAgent` 集成了规划、执行和解析等多个职责，耦合度高。

**解决方案**: 拆分为独立的接口和实现：

- **`IPlanner`**: 负责生成执行计划
  - `PlanAsync()`: 根据任务生成计划
  - `RefinePlanAsync()`: 根据执行结果优化计划
  - 实现: `SimplePlanner`, `LLMPlannerBase`

- **`IToolExecutor`**: 负责工具调用
  - `ExecuteAsync()`: 执行工具并返回结果
  - `RegisterTool()`: 注册工具定义
  - 实现: `DefaultToolExecutor`

- **`IMemory`**: 负责记忆管理（已存在，现集成到 `EnhancedAgent`）

**示例**:
```csharp
var planner = new SimplePlanner(llmClient);
var executor = new DefaultToolExecutor();
var memory = new BufferMemory();

var agent = new EnhancedAgent(llmClient, planner, executor, memory);
```

#### ✅ LLM 中间件系统

**问题**: LLM 调用缺乏统一的中间件机制，难以添加重试、限流、日志等功能。

**解决方案**: 实现了 `ILLMMiddleware` 接口和多个内置中间件：

- **`RetryMiddleware`**: 自动重试失败的请求
- **`RateLimitMiddleware`**: 限流控制
- **`LoggingMiddleware`**: 结构化日志记录
- **`CircuitBreakerMiddleware`**: 熔断器模式

**示例**:
```csharp
var client = LLMClientFactory.Create(apiKey, baseUrl, model);
// 中间件可以通过装饰器模式或工厂方法添加
```

### 2. Graph 引擎增强 (SharpGraph Engine)

#### ✅ 状态持久化 (State Persistence)

**实现**: `IGraphStateStore` 接口和两个实现：
- **`MemoryGraphStateStore`**: 内存存储（用于测试）
- **`FileGraphStateStore`**: 文件系统存储（用于生产）

**功能**:
- `SaveCheckpointAsync()`: 保存检查点
- `LoadCheckpointAsync()`: 加载检查点
- `ListCheckpointsAsync()`: 列出所有检查点
- `DeleteCheckpointAsync()`: 删除检查点

**示例**:
```csharp
var store = new FileGraphStateStore("./checkpoints");
var graph = new EnhancedSharpGraph("start");
graph.StateStore = store;
graph.AutoSaveCheckpoints = true;

// 执行过程中自动保存
var state = await graph.ExecuteAsync(initialState);

// 从检查点恢复
var checkpoint = await store.LoadCheckpointAsync(checkpointId);
var restoredState = await graph.RestoreFromCheckpointAsync(checkpointId, store);
```

#### ✅ 并行执行 (Parallel Execution)

**实现**: `ForkNode` 和 `JoinNode` 支持并行分支执行。

**功能**:
- **Fork**: 将执行流分割为多个并行分支
- **Join**: 等待所有分支完成并合并结果
- **Join 策略**: `All`（全部完成）、`Any`（任一完成）、`Count`（指定数量）

**示例**:
```csharp
var builder = new EnhancedSharpGraphBuilder("start");
builder
    .Fork("split", "branch1", "branch2", "branch3")
    .Join("merge", JoinStrategy.All, states => {
        // 合并所有分支的结果
        return MergeResults(states);
    });
```

#### ✅ 流式事件钩子 (Streaming & Events)

**实现**: `EnhancedSharpGraph` 提供完整的事件系统：

- **`OnNodeStart`**: 节点开始执行时触发
- **`OnNodeEnd`**: 节点执行完成时触发
- **`OnError`**: 执行出错时触发
- **`OnStreaming`**: 流式输出时触发

**示例**:
```csharp
var graph = new EnhancedSharpGraph("start");
graph.OnNodeStart += async (sender, e) => {
    Console.WriteLine($"Node {e.NodeName} started");
};
graph.OnNodeEnd += async (sender, e) => {
    Console.WriteLine($"Node {e.NodeName} completed in {e.ExecutionTime}");
};
graph.OnStreaming += async (sender, chunk) => {
    Console.Write(chunk);
};
```

### 3. Agent 能力补全

#### ✅ 增强记忆系统

**集成**: `EnhancedAgent` 已集成现有的 `IMemory` 接口，支持：
- **Short-term**: `BufferMemory`, `WindowBufferMemory`
- **Long-term**: `VectorMemory`（向量数据库集成）
- **Summary**: `SummaryMemory`（摘要记忆）
- **Entity**: `EntityMemory`（实体记忆）

#### ✅ OpenAPI 工具生成

**实现**: `OpenAPIToolGenerator` 类可以从 OpenAPI/Swagger 规范自动生成工具定义。

**功能**:
- `GenerateFromOpenAPI()`: 从 JSON 字符串生成
- `GenerateFromUrlAsync()`: 从 URL 加载并生成

**示例**:
```csharp
var tools = OpenAPIToolGenerator.GenerateFromOpenAPI(swaggerJson);
foreach (var tool in tools)
{
    executor.RegisterTool(tool);
}
```

### 4. 工程化与可观测性

#### ✅ OpenTelemetry 集成

**实现**: `OpenTelemetrySupport` 类提供分布式追踪支持。

**功能**:
- `StartLLMActivity()`: 创建 LLM 操作的活动
- `StartToolActivity()`: 创建工具执行的活动
- `StartGraphNodeActivity()`: 创建图节点执行的活动

**示例**:
```csharp
using var activity = OpenTelemetrySupport.StartLLMActivity("Chat", model);
activity?.SetTag("llm.provider", "DeepSeek");
// ... LLM 调用 ...
```

#### ✅ 结构化日志

**实现**: `StructuredLogger` 类提供结构化日志记录。

**功能**:
- `LogLLMRequest()`: 记录 LLM 请求（包含模型、消息数、响应长度、耗时）
- `LogToolExecution()`: 记录工具执行（包含工具名、参数、结果）
- `LogGraphNode()`: 记录图节点执行（包含节点名、耗时、成功状态）

**示例**:
```csharp
var logger = new StructuredLogger(loggerFactory.CreateLogger<MyClass>());
logger.LogLLMRequest(model, messages, response, duration);
```

#### ✅ 容错机制

**实现**: 通过中间件系统实现：
- **重试**: `RetryMiddleware` 支持自定义重试策略
- **熔断**: `CircuitBreakerMiddleware` 防止级联故障
- **限流**: `RateLimitMiddleware` 控制请求频率

### 5. 易用性改进

#### ✅ Fluent API 优化

**实现**: `FluentGraphBuilder` 和 `FluentNode` 提供链式 API。

**示例**:
```csharp
var graph = FluentGraphExtensions
    .StartGraph("start")
    .Do(async state => {
        // 执行操作
        return state;
    })
    .Next("process")
    .If(state => state.Get<bool>("condition"), "true_path", "false_path")
    .End()
    .Build();
```

#### ✅ 预置模版

**实现**: `GraphTemplates` 类提供开箱即用的模式：

- **`CreateReActPattern()`**: ReAct（推理+行动）模式
- **`CreateMapReducePattern()`**: MapReduce（多文档处理）模式
- **`CreateReflectionPattern()`**: Reflection（自我纠错）模式

**示例**:
```csharp
var reactGraph = GraphTemplates.CreateReActPattern(llmClient, tools);
var result = await reactGraph.ExecuteAsync(initialState);
```

## 📊 改进对比

| 功能 | LangChain | SharpAIKit (改进后) |
|:-----|:----------|:-------------------|
| 类型安全 | ❌ 字典传递 | ✅ 强类型 Context |
| 模块化 | ⚠️ 部分模块化 | ✅ 完全模块化（IPlanner, IToolExecutor） |
| 中间件 | ❌ 无统一机制 | ✅ 完整的中间件系统 |
| 状态持久化 | ⚠️ 需要手动实现 | ✅ 内置支持（内存/文件） |
| 并行执行 | ⚠️ LangGraph 支持 | ✅ Fork/Join 节点 |
| 事件系统 | ❌ 无 | ✅ 完整生命周期事件 |
| OpenAPI 工具 | ❌ 无 | ✅ 自动生成 |
| OpenTelemetry | ⚠️ 需要手动集成 | ✅ 内置支持 |
| 结构化日志 | ⚠️ 需要手动实现 | ✅ 内置支持 |
| Fluent API | ⚠️ 部分支持 | ✅ 完整的链式 API |
| 预置模版 | ⚠️ 有限 | ✅ ReAct/MapReduce/Reflection |

## 🚀 使用建议

1. **新项目**: 直接使用 `EnhancedAgent` 和 `EnhancedSharpGraph`
2. **现有项目**: 逐步迁移到新的模块化接口
3. **生产环境**: 启用状态持久化和 OpenTelemetry 追踪
4. **开发调试**: 使用结构化日志和事件钩子

## 📝 后续计划

- [ ] 添加更多预置图模版
- [ ] 支持分布式图执行
- [ ] 增强 OpenAPI 工具生成（支持认证、复杂参数）
- [ ] 添加性能监控和指标收集
- [ ] 支持更多向量数据库后端

---

**版本**: v0.1.0  
**更新日期**: 2024-12-04

