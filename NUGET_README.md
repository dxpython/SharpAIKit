# SharpAIKit

**A unified .NET large-model application and agentic AI development framework.**

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-0.3.0-004880?logo=nuget)](https://www.nuget.org/packages/SharpAIKit)

## 🎯 Why SharpAIKit?

**More Powerful Than LangChain, Simpler Than LangChain**

SharpAIKit is a production-ready .NET framework for building AI applications with large language models. It provides a unified API that works with any OpenAI-compatible service, while offering enterprise-grade features like type safety, modular architecture, and comprehensive observability.

### Key Advantages

- ✅ **Type Safety**: C# strong typing with compile-time checking
- ✅ **Native Performance**: 10-100x faster than Python-based solutions
- ✅ **Minimal Dependencies**: Lightweight, no dependency hell
- ✅ **Unified API**: Works with any OpenAI-compatible API
- ✅ **Enterprise Ready**: Built for production .NET applications
- ✅ **Modular Design**: Clean separation of concerns, easy to extend

## 🚀 Quick Start

### Installation

```bash
dotnet add package SharpAIKit
```

Or via Package Manager:
```
Install-Package SharpAIKit
```

### Basic Usage

```csharp
using SharpAIKit.LLM;

// Works with ANY OpenAI-compatible API
var client = LLMClientFactory.Create(
    "your-api-key", 
    "https://api.deepseek.com/v1", 
    "deepseek-chat"
);

// Simple chat
var response = await client.ChatAsync("Hello, how are you?");
Console.WriteLine(response);

// Streaming response
await foreach (var chunk in client.ChatStreamAsync("Tell me a story"))
{
    Console.Write(chunk);
}
```

## 🔮 Killer Features

### 1. Native C# Code Interpreter

Execute C# code directly using Roslyn - no Python dependency, blazing fast!

```csharp
using SharpAIKit.CodeInterpreter;

var interpreter = new RoslynCodeInterpreter();

// Math calculations
var result = await interpreter.ExecuteAsync<double>("Math.Pow(3, 5)");
Console.WriteLine($"3^5 = {result}");  // Output: 243

// Complex data processing
var code = """
    var numbers = Enumerable.Range(1, 100);
    var sum = numbers.Where(n => n % 2 == 0).Sum();
    sum
    """;
var sumResult = await interpreter.ExecuteAsync(code);
Console.WriteLine($"Sum of even numbers: {sumResult.Output}");
```

**Why it's killer**: LangChain's Code Interpreter depends on Python, making deployment complex and slow. SharpAIKit uses .NET's Roslyn compiler for in-memory execution, delivering superior performance.

### 2. SharpGraph - Graph Orchestration

Handle complex workflows with loops and conditional branches using Finite State Machine.

```csharp
using SharpAIKit.Graph;

// Self-correcting workflow: write → execute → check → fix → retry
var graph = new SharpGraphBuilder("start", maxIterations: 20)
    .Node("start", async state => {
        state.Set("attempts", 0);
        state.NextNode = "write_code";
        return state;
    })
    .Node("write_code", async state => {
        // Generate code using LLM
        state.Set("code", await GenerateCodeAsync());
        state.NextNode = "execute_code";
        return state;
    })
    .Node("execute_code", async state => {
        var code = state.Get<string>("code");
        var result = await ExecuteCodeAsync(code);
        state.Set("result", result);
        state.NextNode = "check_result";
        return state;
    })
    .Node("check_result", async state => {
        var isValid = ValidateResult(state.Get<string>("result"));
        if (isValid) {
            state.ShouldEnd = true;
        } else {
            state.NextNode = "fix_code";  // Loop back
        }
        return state;
    })
    .Node("fix_code", async state => {
        state.NextNode = "write_code";  // Retry
        return state;
    })
    .Build();

var result = await graph.ExecuteAsync();
```

**Why it's killer**: LangChain's chains are linear (DAG), making loops difficult. SharpGraph uses Finite State Machine to handle complex, self-correcting workflows.

### 3. DSPy-style Optimizer

Automatically optimize prompts through iterative improvement - gets smarter over time!

```csharp
using SharpAIKit.Optimizer;

var optimizer = new DSPyOptimizer(client)
{
    MaxIterations = 10,
    TargetScore = 0.9
};

// Add training examples
optimizer
    .AddExample("What is C#?", "C# is an object-oriented programming language...")
    .AddExample("What is Python?", "Python is an interpreted programming language...")
    .AddExample("What is JavaScript?", "JavaScript is a dynamic programming language...");

// Set evaluation metric
optimizer.SetMetric(Metrics.Contains);

// Optimize prompt
var initialPrompt = "Answer questions about programming languages: {input}";
var result = await optimizer.OptimizeAsync(initialPrompt);

Console.WriteLine($"Optimized: {result.OptimizedPrompt}");
Console.WriteLine($"Best score: {result.BestScore:F2}");
// The optimizer automatically adds few-shot examples and improves the prompt!
```

**Why it's killer**: LangChain's prompts are hardcoded, requiring manual tweaking. DSPy Optimizer automatically finds the best prompt through iterations.

## 🏗️ Architecture Improvements (v0.3.0)

### Strong Typed Context

Type-safe data passing with compile-time checking:

```csharp
using SharpAIKit.Common;

var context = new StrongContext();
context.Set("user_id", 12345);
context.Set<UserProfile>(profile);

// Type-safe access with IntelliSense support
var userId = context.Get<int>("user_id");
var profile = context.Get<UserProfile>();

// Serialization support for persistence
var json = context.ToJson();
var restored = StrongContext.FromJson(json);
```

### Modular Architecture

Clean separation of concerns with replaceable components:

```csharp
using SharpAIKit.Agent;

// Planner: Generate execution plans
var planner = new SimplePlanner(llmClient);
var plan = await planner.PlanAsync("Complete data analysis task", context);

// Tool Executor: Execute tool calls
var executor = new DefaultToolExecutor();
executor.RegisterTool(myTool);
var result = await executor.ExecuteAsync("tool_name", args, context);

// Enhanced Agent: Combines all components
var agent = new EnhancedAgent(llmClient, planner, executor, memory);
var agentResult = await agent.RunAsync("Complex task");
```

### LLM Middleware System

Unified middleware for retry, rate limiting, logging, and circuit breaking:

```csharp
using SharpAIKit.LLM;

// Retry middleware
var retryMiddleware = new RetryMiddleware(
    maxRetries: 3, 
    delay: TimeSpan.FromSeconds(1)
);

// Rate limit middleware
var rateLimitMiddleware = new RateLimitMiddleware(
    maxRequests: 10, 
    TimeSpan.FromMinutes(1)
);

// Circuit breaker middleware
var circuitBreaker = new CircuitBreakerMiddleware(
    failureThreshold: 5,
    timeout: TimeSpan.FromMinutes(1)
);
```

### Graph Engine Enhancements

#### State Persistence

Save and restore graph execution state:

```csharp
using SharpAIKit.Graph;

var store = new FileGraphStateStore("./checkpoints");
var graph = new EnhancedSharpGraph("start");
graph.StateStore = store;
graph.AutoSaveCheckpoints = true;

// Auto-save during execution
var state = await graph.ExecuteAsync(initialState);

// Restore from checkpoint
var checkpoint = await store.LoadCheckpointAsync(checkpointId);
var restoredState = await graph.RestoreFromCheckpointAsync(checkpointId, store);
```

#### Parallel Execution

Execute multiple branches in parallel:

```csharp
var builder = new EnhancedSharpGraphBuilder("start");
builder
    .Fork("split", "branch1", "branch2", "branch3")
    .Join("merge", JoinStrategy.All, states => {
        // Merge results from all branches
        return MergeResults(states);
    });
```

#### Event System

Lifecycle hooks for monitoring and debugging:

```csharp
var graph = new EnhancedSharpGraph("start");
graph.OnNodeStart += async (sender, e) => {
    Console.WriteLine($"Node {e.NodeName} started");
};
graph.OnNodeEnd += async (sender, e) => {
    Console.WriteLine($"Node {e.NodeName} completed in {e.ExecutionTime}");
};
graph.OnError += async (sender, e) => {
    Console.WriteLine($"Error in {e.NodeName}: {e.Error?.Message}");
};
```

### OpenTelemetry Integration

Built-in distributed tracing support:

```csharp
using SharpAIKit.Observability;

// LLM operation tracing
using var activity = OpenTelemetrySupport.StartLLMActivity("Chat", model);
activity?.SetTag("llm.provider", "DeepSeek");
var response = await client.ChatAsync("Hello");

// Tool execution tracing
using var toolActivity = OpenTelemetrySupport.StartToolActivity("calculator");
// ... execute tool ...

// Graph node tracing
using var nodeActivity = OpenTelemetrySupport.StartGraphNodeActivity("process");
// ... execute node ...
```

### OpenAPI Tool Generation

Auto-generate tool definitions from Swagger/OpenAPI specs:

```csharp
using SharpAIKit.Agent;

// Load from URL
var tools = await OpenAPIToolGenerator.GenerateFromUrlAsync(
    "https://api.example.com/swagger.json"
);

// Or generate from JSON string
var tools = OpenAPIToolGenerator.GenerateFromOpenAPI(swaggerJson);

// Register to executor
foreach (var tool in tools)
{
    executor.RegisterTool(tool);
}
```

## 📚 Core Modules

| Module | Description |
|:-------|:------------|
| **Chain** | LCEL-style pipeline composition with `\|` operator |
| **Memory** | Buffer, Window, Summary, Vector, Entity strategies |
| **Prompt** | Type-safe templates with variable substitution |
| **Output Parser** | Strongly-typed JSON, Boolean, List, XML parsers |
| **Document Loader** | Multi-format support (Text, CSV, JSON, Markdown, Web) |
| **Callback** | Full-trace observability (Console, Logging, Metrics, File) |
| **MultiModal** | Image support (URL, local file, Base64) |
| **Agent** | ReAct, Plan-Execute, Multi-Agent systems |
| **RAG** | Document indexing, vector search, intelligent Q&A |

## 🌐 Supported Providers

SharpAIKit works with **any OpenAI-compatible API**:

| Provider | Base URL |
|:---------|:--------|
| OpenAI | `https://api.openai.com/v1` |
| DeepSeek | `https://api.deepseek.com/v1` |
| Qwen (Alibaba) | `https://dashscope.aliyuncs.com/compatible-mode/v1` |
| Mistral | `https://api.mistral.ai/v1` |
| Yi (01.AI) | `https://api.lingyiwanwu.com/v1` |
| Groq | `https://api.groq.com/openai/v1` |
| Moonshot (Kimi) | `https://api.moonshot.cn/v1` |
| Ollama (Local) | `http://localhost:11434` |
| **Any OpenAI-compatible** | Custom URL |

## 📖 Documentation

- **GitHub**: https://github.com/dxpython/SharpAIKit
- **中文文档**: [README_CN.md](https://github.com/dxpython/SharpAIKit/blob/main/README_CN.md)
- **English Docs**: [README_EN.md](https://github.com/dxpython/SharpAIKit/blob/main/README_EN.md)
- **Architecture Guide**: [ARCHITECTURE_IMPROVEMENTS.md](https://github.com/dxpython/SharpAIKit/blob/main/docs/ARCHITECTURE_IMPROVEMENTS.md)
- **Issues**: https://github.com/dxpython/SharpAIKit/issues

## 🆚 SharpAIKit vs LangChain

| Feature | SharpAIKit | LangChain |
|:--------|:----------|:----------|
| **Type Safety** | ✅ C# Strong typing | ❌ Python weak typing |
| **Performance** | ✅ Native compilation | ❌ Interpreted |
| **Code Interpreter** | ✅ Native C# (Roslyn) | ❌ Python dependency |
| **Graph Orchestration** | ✅ SharpGraph (FSM) | ⚠️ LangGraph (new) |
| **Auto Optimization** | ✅ DSPy-style | ❌ None |
| **State Persistence** | ✅ Built-in | ⚠️ Manual |
| **Parallel Execution** | ✅ Fork/Join | ⚠️ Limited |
| **Event System** | ✅ Lifecycle hooks | ❌ None |
| **OpenTelemetry** | ✅ Built-in | ⚠️ Manual |
| **Dependencies** | ✅ Minimal | ❌ Many |

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Built with ❤️ for the .NET community.

---

**⭐ Star this project if it helps you!**
