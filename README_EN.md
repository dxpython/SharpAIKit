<div align="center">

## SharpAIKit: A Unified AI/LLM Toolkit for .NET

### 🎯 More Powerful Than LangChain, Simpler Than LangChain

<img src="imgs/logo.jpg" alt="SharpAIKit Logo" width="900">

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-v2.0.0-004880?style=for-the-badge&logo=nuget&logoColor=white)](https://www.nuget.org/)

[中文文档](README_CN.md) | [🏠 Home](README.md)

</div>

---

## 🆚 SharpAIKit vs LangChain

| Feature | SharpAIKit | LangChain |
|:--------|:----------:|:---------:|
| **Chain (LCEL)** | ✅ Pipe operator | ✅ |
| **Memory** | ✅ 5 strategies | ✅ |
| **Prompt Templates** | ✅ Type-safe | ✅ |
| **Output Parsers** | ✅ Strongly-typed | ✅ |
| **Document Loaders** | ✅ Multi-format | ✅ |
| **Observability** | ✅ Full tracing | ✅ |
| **MultiModal** | ✅ Image support | ✅ |
| **Advanced Agents** | ✅ ReAct/Multi-Agent | ✅ |
| **Code Interpreter** | ✅ **Native C# (Roslyn)** | ❌ Python dependency |
| **Graph Orchestration** | ✅ **SharpGraph (FSM)** | ⚠️ LangGraph (new) |
| **Auto Optimization** | ✅ **DSPy-style** | ❌ None |
| **Type Safety** | ✅ **C# Strong typing** | ❌ Python weak typing |
| **Performance** | ✅ **Native compilation** | ❌ Interpreted |
| **Code Simplicity** | ✅ **Minimal API** | ❌ Heavy abstractions |
| **Dependencies** | ✅ **Minimal** | ❌ Many dependencies |
| **Strong Typed Context** | ✅ **StrongContext** | ❌ Dictionary passing |
| **Modular Architecture** | ✅ **IPlanner/IToolExecutor** | ⚠️ Partially modular |
| **Middleware System** | ✅ **Full support** | ❌ No unified mechanism |
| **State Persistence** | ✅ **Built-in** | ⚠️ Manual implementation |
| **Parallel Execution** | ✅ **Fork/Join** | ⚠️ LangGraph support |
| **Event System** | ✅ **Lifecycle hooks** | ❌ None |
| **OpenAPI Tools** | ✅ **Auto-generation** | ❌ None |
| **OpenTelemetry** | ✅ **Built-in** | ⚠️ Manual integration |
| **Structured Logging** | ✅ **Built-in** | ⚠️ Manual implementation |
| **Fluent API** | ✅ **Chain building** | ⚠️ Partial support |
| **Pre-built Templates** | ✅ **ReAct/MapReduce/Reflection** | ⚠️ Limited |

---

## 📋 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Chain (LCEL)](#-chain-lcel)
- [Memory](#-memory)
- [Prompt Templates](#-prompt-templates)
- [Output Parsers](#-output-parsers)
- [Document Loaders](#-document-loaders)
- [Observability (Callback)](#-observability-callback)
- [MultiModal](#-multimodal)
- [Advanced Agents](#-advanced-agents)
- [🎯 Agent Skills Mechanism](#-agent-skills-mechanism--enterprise-governance-feature) ⭐ **Enterprise Governance Feature**
- [RAG Engine](#-rag-engine)
- [🔮 Native C# Code Interpreter](#-native-c-code-interpreter) ⭐ **Killer Feature**
- [🕸️ SharpGraph](#️-sharpgraph) ⭐ **Killer Feature**
- [🧬 DSPy-style Optimizer](#-dspy-style-optimizer) ⭐ **Killer Feature**
- [🏗️ Architecture Improvements](#️-architecture-improvements) ⭐ **Enterprise Features**
- [Supported Providers](#-supported-providers)

---

## ✨ Features

| Module | Feature | Description |
|:-------|:--------|:------------|
| 🔗 **Chain** | Chain Composition | LCEL-style pipe composition with `\|` operator |
| 🧠 **Memory** | Conversation Memory | Buffer, Window, Summary, Vector, Entity strategies |
| 📝 **Prompt** | Prompt Templates | Variable substitution, Chat templates, Few-shot learning |
| 📤 **Output** | Output Parsers | JSON, Boolean, List, XML, Regex parsers |
| 📄 **Loader** | Document Loading | Text, CSV, JSON, Markdown, Web multi-format |
| 📊 **Callback** | Observability | Console, Logging, Metrics, File full-trace |
| 🖼️ **MultiModal** | MultiModal | Image URL, local files, Base64 support |
| 🤖 **Agent** | Intelligent Agents | ReAct, Plan-Execute, Multi-Agent systems |
| 🎯 **Skills** | Behavior Governance | **Enterprise behavior constraint mechanism**, discoverable, activatable, auditable ⭐ NEW |
| 📚 **RAG** | Retrieval-Augmented | Document indexing, vector search, Q&A |
| 🔮 **Code Interpreter** | Code Execution | **Native C# code execution**, no Python, based on Roslyn |
| 🕸️ **SharpGraph** | Graph Orchestration | **Finite State Machine**, supports loops and complex branches |
| 🧬 **DSPy Optimizer** | Auto Optimization | **Automatic prompt optimization**, gets smarter over time |
| 🏗️ **StrongContext** | Strong Typed Context | **Type-safe data passing**, compile-time checking |
| 🔧 **Modular Architecture** | Interface Separation | **IPlanner/IToolExecutor/IMemory**, clear responsibilities |
| 🔌 **Middleware System** | LLM Middleware | **Retry/RateLimit/Logging/CircuitBreaker**, unified mechanism |
| 💾 **State Persistence** | Checkpoint Support | **Memory/File storage**, task recovery support |
| ⚡ **Parallel Execution** | Fork/Join | **Multi-branch parallel**, performance boost |
| 📡 **Event System** | Lifecycle Hooks | **OnNodeStart/End/Error**, full tracing |
| 🔗 **OpenAPI Tools** | Auto Generation | **Generate tool definitions from Swagger** |
| 📊 **OpenTelemetry** | Distributed Tracing | **Built-in support**, Jaeger/Aspire visualization |
| 📝 **Structured Logging** | Logging | **Structured attributes**, easy debugging |
| 🎨 **Fluent API** | Chain Building | **Elegant API**, better DX |
| 📦 **Pre-built Templates** | Out-of-box | **ReAct/MapReduce/Reflection** patterns |
| 🐍 **Python SDK** | Python Client | **Official Python SDK**, calls C# services via gRPC ⭐ NEW |

---

## 📦 Installation

### .NET Package (NuGet)

```bash
dotnet add package SharpAIKit
```

### Python SDK (PyPI) ⭐ **New**

SharpAIKit now provides an official Python SDK that calls C# services via gRPC.

```bash
pip install sharpaikit
```

Or using `uv`:

```bash
uv pip install sharpaikit
```

**Python SDK Features**:
- ✅ Agent execution (sync/async/streaming)
- ✅ Full Skill system support
- ✅ Tool execution
- ✅ Context passing
- ✅ Automatic process management (auto start/stop gRPC host)

**Quick Start**:

```python
from sharpaikit import Agent

# Create agent (automatically starts gRPC host)
agent = Agent(
    api_key="your-api-key",
    model="gpt-4",
    base_url="https://api.openai.com/v1",
    auto_start_host=True
)

# Execute task
result = agent.run("Hello, world!")
print(result.output)

# Use Skills
agent = Agent(
    api_key="your-api-key",
    model="gpt-4",
    skills=["code-review", "security-policy"],
    auto_start_host=True
)

result = agent.run("Review this code for security issues")
if result.skill_resolution:
    print(f"Activated Skills: {result.skill_resolution.activated_skill_ids}")

# Cleanup
agent.close()
```

**More Information**:
- PyPI Package: https://pypi.org/project/sharpaikit/
- Python SDK Documentation: `python-client/README.md`

---

## 🚀 Quick Start

```csharp
using SharpAIKit.LLM;

// Three lines of code, works with any OpenAI-compatible API
var client = LLMClientFactory.Create("your-api-key", "https://api.deepseek.com/v1", "deepseek-chat");
var response = await client.ChatAsync("Hello!");
Console.WriteLine(response);

// Streaming output
await foreach (var chunk in client.ChatStreamAsync("Tell me a story"))
{
    Console.Write(chunk);
}
```

---

## 🔗 Chain (LCEL)

**LCEL-style pipe composition, elegant like LangChain!**

```csharp
using SharpAIKit.Chain;
using SharpAIKit.Prompt;

// Create chain components
var prompt = PromptTemplate.FromTemplate("Translate the following to {language}: {input}");
var llm = new LLMChain(client);

// Pipe composition (using | operator, like LangChain LCEL!)
var chain = prompt.Pipe(llm);

// Execute
var result = await chain.InvokeAsync(new ChainContext()
    .Set("language", "French")
    .Set("input", "Hello, world"));

Console.WriteLine(result.Output);

// Parallel execution
var parallel = ChainExtensions.Parallel(
    new LLMChain(client, "You are an optimist"),
    new LLMChain(client, "You are a realist"),
    new LLMChain(client, "You are a critic")
);

// Conditional branching
var branched = chain.Branch(
    ctx => ctx.Output.Contains("error"),
    trueBranch: errorHandlerChain,
    falseBranch: successChain
);
```

---

## 🧠 Memory

**5 memory strategies, richer than LangChain!**

```csharp
using SharpAIKit.Memory;

// 1. Buffer Memory - Keep last N messages
var buffer = new BufferMemory { MaxMessages = 20 };

// 2. Window Buffer - Keep last N conversation turns
var window = new WindowBufferMemory { WindowSize = 5 };

// 3. Summary Memory - Auto-summarize old conversations
var summary = new SummaryMemory(client) { RecentMessagesCount = 6 };

// 4. Vector Memory - Semantic search over conversation history
var vector = new VectorMemory(client) { TopK = 5 };

// 5. Entity Memory - Extract and track entities
var entity = new EntityMemory(client);

// Usage
await buffer.AddExchangeAsync("What is Python?", "Python is a programming language...");
var context = await buffer.GetContextStringAsync();
```

---

## 📝 Prompt Templates

**Type-safe template system!**

```csharp
using SharpAIKit.Prompt;

// Simple template
var template = PromptTemplate.FromTemplate("You are {role}, answer: {input}");
var prompt = template.Format(("role", "AI Assistant"), ("input", "What is C#?"));

// With partial variables
var withPartials = PromptTemplate.FromTemplate("Current time: {time}\nUser: {input}")
    .WithPartial("time", () => DateTime.Now.ToString());

// Chat template
var chatTemplate = new ChatPromptTemplate()
    .AddSystemMessage("You are a {role} assistant")
    .AddHistoryPlaceholder("history")
    .AddUserMessage("{input}");

// Few-shot template
var fewShot = new FewShotPromptTemplate(
    prefix: "Classify sentiment:",
    suffix: "Text: {input}\nSentiment:",
    exampleTemplate: "Text: {input}\nSentiment: {output}"
)
.AddExample("I love this product!", "Positive")
.AddExample("This is terrible", "Negative");
```

---

## 📤 Output Parsers

**Strongly-typed generic parsing, safer than LangChain!**

```csharp
using SharpAIKit.Output;

// JSON parsing to strongly-typed object
var jsonParser = new JsonOutputParser<ProductReview>();
var review = jsonParser.Parse(llmOutput);
Console.WriteLine(review.Rating);  // Strongly-typed access!

// Boolean parsing
var boolParser = new BooleanParser();
bool isTrue = boolParser.Parse("yes");  // true

// List parsing
var listParser = new CommaSeparatedListParser();
List<string> items = listParser.Parse("apple, banana, orange");

// XML tag parsing
var xmlParser = new XMLTagParser("answer", "reasoning");
var result = xmlParser.Parse("<answer>42</answer><reasoning>...</reasoning>");

// Get format instructions (for Prompt)
string instructions = jsonParser.GetFormatInstructions();
```

---

## 📄 Document Loaders

**Multi-format support, ready to use!**

```csharp
using SharpAIKit.DocumentLoader;

// Text file
var textLoader = new TextFileLoader("document.txt");

// CSV file (column-aware)
var csvLoader = new CsvLoader("data.csv")
{
    OneDocumentPerRow = true,
    ContentColumns = new[] { "title", "content" },
    MetadataColumns = new[] { "id", "category" }
};

// Markdown (supports header splitting)
var mdLoader = new MarkdownLoader("readme.md")
{
    SplitByHeaders = true,
    SplitHeaderLevel = 2
};

// Directory batch loading
var dirLoader = new TextDirectoryLoader("./docs", "*.txt", recursive: true);

// Web loading
var webLoader = new WebLoader("https://example.com/api/data");

// Load documents
var documents = await csvLoader.LoadAsync();
foreach (var doc in documents)
{
    Console.WriteLine($"Content: {doc.Content}");
    Console.WriteLine($"Source: {doc.Metadata["source"]}");
}
```

---

## 📊 Observability (Callback)

**Full-trace observability, essential for production!**

```csharp
using SharpAIKit.Callback;

// Console output (for debugging)
var consoleCallback = new ConsoleCallbackHandler { UseColors = true };

// Logging
var loggingCallback = new LoggingCallbackHandler(logger);

// Performance metrics
var metricsCallback = new MetricsCallbackHandler();

// File persistence
var fileCallback = new FileCallbackHandler("llm_logs.jsonl");

// Callback manager
var manager = new CallbackManager()
    .AddHandler(consoleCallback)
    .AddHandler(metricsCallback);

// View metrics
var metrics = metricsCallback.GetSummary();
Console.WriteLine($"Calls: {metrics.LLMCalls}");
Console.WriteLine($"Avg Latency: {metrics.AverageLatencyMs}ms");
Console.WriteLine($"Total Tokens: {metrics.TotalTokens}");
```

---

## 🖼️ MultiModal

**Image understanding, Vision support!**

```csharp
using SharpAIKit.MultiModal;

// Create image message from URL
var message = MultiModalMessage.User(
    "What's in this image?",
    "https://example.com/image.jpg"
);

// Fluent builder
var multiModal = new MultiModalMessageBuilder()
    .WithRole("user")
    .AddText("Compare these two images:")
    .AddImage("https://example.com/img1.jpg")
    .AddImage("https://example.com/img2.jpg")
    .Build();

// From local file (auto Base64 encoding)
var localImage = MultiModalExtensions.CreateVisionMessageFromFile(
    "Describe this image in detail",
    "./my-image.png"
);
```

---

## 🤖 Advanced Agents

**ReAct, Plan-Execute, Multi-Agent three modes!**

```csharp
using SharpAIKit.Agent;

// 1. ReAct Agent - Reasoning + Acting loop
var reactAgent = new ReActAgent(client)
    .AddTool(new CalculatorTool())
    .AddTool(new WebSearchTool());

var result = await reactAgent.RunAsync("Search for latest AI news and summarize");
// Output includes: Thought -> Action -> Observation loop

// 2. Plan-and-Execute Agent - Plan first, then execute
var planAgent = new PlanAndExecuteAgent(client)
    .AddTool(new CalculatorTool());

var planResult = await planAgent.RunAsync("Analyze this data and generate a report");
// Output includes: Plan steps + execution results for each step

// 3. Multi-Agent System - Multi-agent collaboration
var multiAgent = new MultiAgentSystem(client)
    .AddAgent("researcher", "Research Expert", "You are a professional researcher...")
    .AddAgent("writer", "Content Writer", "You are an excellent technical writer...")
    .AddAgent("reviewer", "Quality Reviewer", "You are a meticulous reviewer...");

var teamResult = await multiAgent.RunAsync("Write a technical blog about AI");
// Output includes: Task delegation + agent responses + synthesized answer
```

---

## 🎯 Agent Skills Mechanism ⭐ **Enterprise Governance Feature**

**🎯 Core Value: Decouple Agent behavior specifications from Prompts, providing discoverable, activatable, and constrainable behavior modules for enterprise/platform-level Agent governance scenarios.**

### Why Skills?

- **Pain Point**: Traditional approaches hardcode behavior specifications in Prompts, making them hard to manage, reuse, and audit
- **Advantage**: Skills are independent behavior constraint modules that can be dynamically activated, combined, and audited
- **Effect**: Enterprises can centrally manage security policies, compliance rules, code review standards, etc., without modifying Agent core code

### Core Concepts

**Skills are behavior constraints, not execution entities**:
- Skills do not directly execute tasks, only influence "how" and "what" the Agent is allowed to execute
- Skills restrict tool usage, execution steps, execution time, etc. through constraints
- Skills can inject context information to influence Agent decision-making

### Basic Usage

```csharp
using SharpAIKit.Agent;
using SharpAIKit.Skill;
using SharpAIKit.Skill.Examples;

// 1. Create Skill Resolver
var skillResolver = new DefaultSkillResolver();

// 2. Register Skills
skillResolver.RegisterSkill(new SecurityPolicySkill());
skillResolver.RegisterSkill(new CodeReviewSkill());

// 3. Create Agent and inject Skill Resolver
var client = LLMClientFactory.CreateDeepSeek("your-api-key");
var agent = new EnhancedAgent(
    llmClient,
    skillResolver: skillResolver // Inject Skill Resolver
);

// 4. Run task (Skills automatically activate and apply)
var result = await agent.RunAsync("Review this code for security issues");

// 5. View Skill resolution result
if (agent.LastSkillResolution != null)
{
    Console.WriteLine($"Activated Skills: {string.Join(", ", agent.LastSkillResolution.ActivatedSkillIds)}");
    Console.WriteLine($"Decision Reasons:\n{string.Join("\n", agent.LastSkillResolution.DecisionReasons)}");
}
```

### Skill Constraint Types

```csharp
public class SkillConstraints
{
    // 1. Tool whitelist (only allow specified tools)
    public IReadOnlySet<string>? AllowedTools { get; init; }
    
    // 2. Tool blacklist (forbid specified tools)
    public IReadOnlySet<string> ForbiddenTools { get; init; }
    
    // 3. Maximum execution steps
    public int? MaxSteps { get; init; }
    
    // 4. Maximum execution time
    public TimeSpan? MaxExecutionTime { get; init; }
    
    // 5. Context modifications (injected into Agent context)
    public IReadOnlyDictionary<string, object?> ContextModifications { get; init; }
    
    // 6. Custom validator (validated before tool execution)
    public Func<string, Dictionary<string, object?>, StrongContext, bool>? CustomValidator { get; init; }
}
```

### Constraint Merging Rules (Deterministic Algorithm)

Constraints from multiple Skills are merged according to the following rules:

| Constraint Type | Merging Strategy | Description |
|:---------------|:----------------|:------------|
| **AllowedTools** | **Intersection** | Multiple Skills' whitelists are intersected, most restrictive limit applies |
| **ForbiddenTools** | **Union** | Multiple Skills' blacklists are unioned, any deny means deny |
| **MaxSteps** | **Minimum** | Take the minimum value from all Skill restrictions |
| **MaxExecutionTime** | **Minimum** | Take the minimum value from all Skill restrictions |
| **ContextModifications** | **High Priority Override** | High priority Skills' context modifications override low priority |
| **CustomValidator** | **AND Logic** | All validators must pass |

**Conflict Resolution**: `ForbiddenTools` always override `AllowedTools` (Deny-overrides-Allow), ensuring security takes precedence.

### Example: Code Review Skill

```csharp
public class CodeReviewSkill : ISkill
{
    public SkillMetadata Metadata => new()
    {
        Id = "code_review",
        Name = "Code Review Skill",
        Description = "Enforces code review best practices",
        Version = "1.0.0",
        Scope = "code_review",
        Priority = 10
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        var keywords = new[] { "review", "code review", "analyze code", "inspect" };
        return keywords.Any(k => task.ToLowerInvariant().Contains(k));
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            // Only allow code analysis tools
            AllowedTools = new HashSet<string>
            {
                "code_analyzer",
                "syntax_checker",
                "linter",
                "security_scanner"
            },
            // Forbid file writing tools
            ForbiddenTools = new HashSet<string> { "file_writer", "code_modifier" },
            // Limit maximum steps
            MaxSteps = 5,
            // Inject code review context
            ContextModifications = new Dictionary<string, object?>
            {
                ["review_mode"] = true,
                ["focus_areas"] = new[] { "security", "performance", "maintainability" }
            }
        };
    }
    
    public string? ApplyToPlanning(string planningPrompt, StrongContext context)
    {
        return planningPrompt + "\n\nNote: This is a code review task. Focus on analysis, not modification.";
    }
}
```

### Example: Security Policy Skill

```csharp
public class SecurityPolicySkill : ISkill
{
    public SkillMetadata Metadata => new()
    {
        Id = "security_policy",
        Name = "Security Policy Skill",
        Description = "Enforces security policies",
        Version = "1.0.0",
        Scope = "security",
        Priority = 100 // High priority
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        // Security policy always activates (or based on user role)
        var userRole = context.Get<string>("user_role");
        return userRole != "admin";
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            // Forbid high-risk tools
            ForbiddenTools = new HashSet<string>
            {
                "file_deleter",
                "system_command",
                "database_writer",
                "network_request"
            },
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            // Custom validator: check if tool arguments contain sensitive information
            CustomValidator = (toolName, args, ctx) =>
            {
                var sensitivePatterns = new[] { "password", "token", "secret", "key" };
                var argsStr = string.Join(" ", args.Values.Select(v => v?.ToString() ?? ""));
                return !sensitivePatterns.Any(p => argsStr.Contains(p, StringComparison.OrdinalIgnoreCase));
            }
        };
    }
}
```

### Observability & Auditability

The Skills system provides complete observability and auditability:

```csharp
var result = await agent.RunAsync("Review code");
var skillResolution = agent.LastSkillResolution;

if (skillResolution != null)
{
    // View activated Skills
    Console.WriteLine($"Activated Skills: {string.Join(", ", skillResolution.ActivatedSkillIds)}");
    
    // View decision reasons (complete audit trail)
    foreach (var reason in skillResolution.DecisionReasons)
    {
        Console.WriteLine($"  - {reason}");
    }
    
    // View final constraints
    var constraints = skillResolution.FinalConstraints;
    Console.WriteLine($"Allowed Tools: {constraints.AllowedTools?.Count ?? 0} (null=no restriction)");
    Console.WriteLine($"Forbidden Tools: {constraints.ForbiddenTools.Count}");
    Console.WriteLine($"Max Steps: {constraints.MaxSteps?.ToString() ?? "unlimited"}");
}
```

**Decision Reason Example**:
```
Skill 'code_review' (Code Review Skill) activated: task matches activation conditions
Skill 'code_review' sets allowed tools: code_analyzer, syntax_checker, linter
Skill 'security_policy' adds forbidden tools: file_deleter, system_command
Conflict resolution: Deny-overrides-Allow. Tools file_writer are in both allowed and forbidden lists, they will be forbidden.
Final constraints: AllowedTools=3, ForbiddenTools=2, MaxSteps=5, MaxExecutionTime=00:05:00
```

### Architectural Advantages

- ✅ **Decoupling**: Skills are completely decoupled from Agent Core, Agents can run without Skills
- ✅ **Extensibility**: Adding new Skills only requires implementing `ISkill` interface, no Core modifications needed
- ✅ **Type Safety**: All constraints and metadata are strongly typed, compile-time checked
- ✅ **Backward Compatible**: Existing code runs without modification, Skills system is optional
- ✅ **Enterprise Governance**: Provides complete audit trails and observability

### Use Cases

- **Security Policies**: Restrict high-risk tool usage, prevent data leaks
- **Compliance Requirements**: Enforce GDPR, HIPAA, and other compliance rules
- **Code Review**: Restrict to code analysis tools only, forbid code modification
- **Organizational Standards**: Inject organization-specific context and style guides
- **Resource Limits**: Limit execution steps and time, prevent resource abuse

---

---

## 📚 RAG Engine

```csharp
using SharpAIKit.RAG;

var rag = new RagEngine(client);

// Index documents
await rag.IndexDocumentAsync("docs/guide.txt");
await rag.IndexContentAsync("Your document content...");
await rag.IndexDirectoryAsync("./knowledge", "*.md");

// Intelligent Q&A
var answer = await rag.AskAsync("How to use this feature?");

// Streaming answer
await foreach (var chunk in rag.AskStreamAsync("What is RAG?"))
{
    Console.Write(chunk);
}

// Retrieve only (no generation)
var docs = await rag.RetrieveAsync("relevant query", topK: 5);
```

---

## 🔮 Native C# Code Interpreter

**🎯 Killer Feature: Let agents write and execute C# code directly, no Python needed!**

### Why This Is a Killer Feature?

- **Pain Point**: LangChain's Code Interpreter depends on Python, deployment is troublesome and slow
- **Advantage**: Uses .NET's Roslyn compiler technology, executes in-memory, extremely fast
- **Effect**: Agents are no longer "language giants, math dwarfs", can write code to solve math and data processing problems

### Basic Usage

```csharp
using SharpAIKit.CodeInterpreter;

var interpreter = new RoslynCodeInterpreter();

// Math calculation
var mathCode = """
    var a = 3;
    var b = 5;
    var result = Math.Pow(a, b);
    result
    """;
var result = await interpreter.ExecuteAsync<double>(mathCode);
Console.WriteLine($"3^5 = {result}");  // Output: 243

// Fibonacci sequence
var fibCode = """
    var n = 10;
    var fib = new List<int> { 0, 1 };
    for (int i = 2; i < n; i++)
    {
        fib.Add(fib[i-1] + fib[i-2]);
    }
    string.Join(", ", fib)
    """;
var fibResult = await interpreter.ExecuteAsync(fibCode);
Console.WriteLine(fibResult.Output);  // Output: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34
```

### Data Processing

```csharp
// List processing
var dataCode = """
    var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    var evens = numbers.Where(n => n % 2 == 0).ToList();
    var sum = evens.Sum();
    var avg = evens.Average();
    $"Evens: {string.Join(", ", evens)}, Sum: {sum}, Avg: {avg}"
    """;
var dataResult = await interpreter.ExecuteAsync(dataCode);
Console.WriteLine(dataResult.Output);
```

### Integration with Agent

```csharp
using SharpAIKit.Agent;

// Create tool using Code Interpreter
public class CodeInterpreterTool : ToolBase
{
    private readonly ICodeInterpreter _interpreter;

    public CodeInterpreterTool(ICodeInterpreter interpreter)
    {
        _interpreter = interpreter;
    }

    [Tool("execute_code", "Execute C# code and return result")]
    public async Task<string> ExecuteCode(
        [Parameter("C# code to execute")] string code)
    {
        var result = await _interpreter.ExecuteAsync(code);
        return result.Success ? result.Output : $"Error: {result.Error}";
    }
}

// Use in Agent
var agent = new AiAgent(client);
agent.AddTool(new CodeInterpreterTool(interpreter));

var answer = await agent.RunAsync("Calculate the sum of squares of even numbers from 1 to 100");
// Agent will automatically write code and execute!
```

---

## 🕸️ SharpGraph

**🎯 Killer Feature: Graph-based orchestration engine using Finite State Machine (FSM), supports loops and complex branches!**

### Why This Is a Killer Feature?

- **Pain Point**: LangChain's Chain is linear (DAG), hard to handle loops (e.g., write code → run → error → fix → run again)
- **Advantage**: Implements graph orchestration based on Finite State Machine, can define States and Edges, let agents "walk" in the graph
- **Effect**: Easily build complex agents with "self-correcting" capabilities

### Basic Usage

```csharp
using SharpAIKit.Graph;

// Create a simple graph
var graph = new SharpGraphBuilder("start", maxIterations: 20)
    .Node("start", async state =>
    {
        Console.WriteLine("Starting task");
        state.Set("task", "Calculate Fibonacci");
        state.NextNode = "process";
        return state;
    })
    .Node("process", async state =>
    {
        Console.WriteLine("Processing task");
        state.Set("result", "Done");
        state.NextNode = "end";
        return state;
    })
    .Node("end", async state =>
    {
        Console.WriteLine("Task completed");
        state.Output = state.Get<string>("result");
        state.ShouldEnd = true;
        return state;
    })
    .Build();

var result = await graph.ExecuteAsync();
Console.WriteLine($"Result: {result.Output}");
```

### Loops and Conditional Branches

```csharp
// Create a "write code → run → check error → fix" loop graph
var graph = new SharpGraphBuilder("start", maxIterations: 20)
    .Node("start", async state =>
    {
        state.Set("attempts", 0);
        state.NextNode = "write_code";
        return state;
    })
    .Node("write_code", async state =>
    {
        var attempts = state.Get<int>("attempts");
        Console.WriteLine($"Attempt #{attempts + 1}: Writing code");
        state.Set("code", "var x = 10; x * 2");
        state.NextNode = "execute_code";
        return state;
    })
    .Node("execute_code", async state =>
    {
        var code = state.Get<string>("code");
        try
        {
            var result = await interpreter.ExecuteAsync(code);
            state.Set("result", result.Output);
            state.Set("error", (string?)null);
            state.NextNode = "check_result";
        }
        catch (Exception ex)
        {
            state.Set("error", ex.Message);
            state.NextNode = "fix_code";
        }
        return state;
    })
    .Node("check_result", async state =>
    {
        var result = state.Get<string>("result");
        if (!string.IsNullOrEmpty(result))
        {
            state.Output = result;
            state.ShouldEnd = true;
        }
        else
        {
            state.NextNode = "fix_code";
        }
        return state;
    })
    .Node("fix_code", async state =>
    {
        var attempts = state.Get<int>("attempts") + 1;
        state.Set("attempts", attempts);
        
        if (attempts >= 3)
        {
            state.ShouldEnd = true;
            state.Output = "Max attempts reached";
        }
        else
        {
            state.NextNode = "write_code";  // Loop back
        }
        return state;
    })
    .Build();

var finalState = await graph.ExecuteAsync();
```

---

## 🧬 DSPy-style Optimizer

**🎯 Killer Feature: Automatically optimize prompts, let the framework write prompts itself, gets smarter over time!**

### Why This Is a Killer Feature?

- **Pain Point**: LangChain's Prompts are hardcoded strings, poor effects require manual tweaking, like "alchemy"
- **Advantage**: Implements DSPy-like mechanism, define tasks and evaluation metrics, framework automatically optimizes through iterations
- **Effect**: Automatically optimize from "Help me" to "You are an expert... [Few-Shot Examples]..."

### Basic Usage

```csharp
using SharpAIKit.Optimizer;

var optimizer = new DSPyOptimizer(client)
{
    MaxIterations = 10,
    TargetScore = 0.9,
    FewShotExamples = 3
};

// Add training examples
optimizer
    .AddExample("What is C#?", "C# is an object-oriented programming language developed by Microsoft")
    .AddExample("What is Python?", "Python is an interpreted, object-oriented high-level programming language")
    .AddExample("What is Java?", "Java is a cross-platform object-oriented programming language");

// Set evaluation metric
optimizer.SetMetric(Metrics.Contains);

// Optimize prompt
var initialPrompt = "Answer questions about programming languages: {input}";
var result = await optimizer.OptimizeAsync(initialPrompt);

Console.WriteLine($"Optimized prompt:\n{result.OptimizedPrompt}");
Console.WriteLine($"Best score: {result.BestScore:F2}");
Console.WriteLine($"Iterations: {result.Iterations}");
```

### Evaluation Metrics

```csharp
// 1. Exact match
optimizer.SetMetric(Metrics.ExactMatch);

// 2. Contains match
optimizer.SetMetric(Metrics.Contains);

// 3. Semantic similarity (using embeddings)
optimizer.SetMetric(Metrics.SemanticSimilarity(client));

// 4. Custom metric
optimizer.SetMetric(Metrics.Custom(async (input, output, expected) =>
{
    var score = 0.0;
    if (output.Contains(expected)) score += 0.5;
    if (output.Length > 50) score += 0.3;
    if (output.Contains("programming language")) score += 0.2;
    return score;
}));
```

---

## 🏗️ Architecture Improvements

SharpAIKit v0.1.0 introduces comprehensive architecture improvements, designed to surpass LangChain and fully leverage the .NET ecosystem.

### 🔷 1. Strong Typed Context (StrongContext)

**Problem**: Previously used `Dictionary<string, object?>` for data passing, type-unsafe and hard to maintain.

**Solution**: Introduced strongly-typed `StrongContext` object:

```csharp
using SharpAIKit.Common;

var context = new StrongContext();
context.Set("user_id", 12345);
context.Set<UserProfile>(profile);

// Type-safe access
var userId = context.Get<int>("user_id");
var profile = context.Get<UserProfile>();

// Serialization support
var json = context.ToJson();
var restored = StrongContext.FromJson(json);
```

**Advantages**:
- ✅ Compile-time type checking
- ✅ IntelliSense support
- ✅ Backward compatible dictionary access
- ✅ Serialization/deserialization support

### 🔷 2. Modular Architecture

**Problem**: `AiAgent` integrated planning, execution, and parsing responsibilities, high coupling.

**Solution**: Split into independent interfaces:

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

**Advantages**:
- ✅ Clear responsibilities, easy to test
- ✅ Replaceable components
- ✅ Dependency injection support

### 🔷 3. LLM Middleware System

**Problem**: LLM calls lacked unified middleware mechanism.

**Solution**: Implemented complete middleware system:

```csharp
using SharpAIKit.LLM;

// Retry middleware
var retryMiddleware = new RetryMiddleware(maxRetries: 3, delay: TimeSpan.FromSeconds(1));

// Rate limit middleware
var rateLimitMiddleware = new RateLimitMiddleware(maxRequests: 10, TimeSpan.FromMinutes(1));

// Logging middleware
var loggingMiddleware = new LoggingMiddleware(logger);

// Circuit breaker middleware
var circuitBreaker = new CircuitBreakerMiddleware(failureThreshold: 5);
```

**Advantages**:
- ✅ Unified retry, rate limit, logging mechanism
- ✅ Circuit breaker prevents cascading failures
- ✅ Easy to extend

### 🔷 4. Graph Engine Enhancements

#### State Persistence

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

### 🔷 5. OpenAPI Tool Generation

Auto-generate tool definitions from Swagger/OpenAPI specs:

```csharp
using SharpAIKit.Agent;

// Load from URL
var tools = await OpenAPIToolGenerator.GenerateFromUrlAsync("https://api.example.com/swagger.json");

// Generate from JSON string
var tools = OpenAPIToolGenerator.GenerateFromOpenAPI(swaggerJson);

// Register to executor
foreach (var tool in tools)
{
    executor.RegisterTool(tool);
}
```

### 🔷 6. OpenTelemetry Integration

Distributed tracing support:

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

### 🔷 7. Structured Logging

```csharp
using SharpAIKit.Observability;

var logger = new StructuredLogger(loggerFactory.CreateLogger<MyClass>());

// Log LLM request
logger.LogLLMRequest(model, messages, response, duration);

// Log tool execution
logger.LogToolExecution(toolName, arguments, result, success: true);

// Log graph node execution
logger.LogGraphNode(nodeName, duration, success: true);
```

### 🔷 8. Fluent API

Elegant chain-style building:

```csharp
using SharpAIKit.Graph;

var graph = FluentGraphExtensions
    .StartGraph("start")
    .Do(async state => {
        // Execute operation
        return state;
    })
    .Next("process")
    .If(state => state.Get<bool>("condition"), "true_path", "false_path")
    .End()
    .Build();
```

### 🔷 9. Pre-built Templates

Out-of-box graph patterns:

```csharp
using SharpAIKit.Graph;

// ReAct pattern
var reactGraph = GraphTemplates.CreateReActPattern(llmClient, tools);

// MapReduce pattern
var mapReduceGraph = GraphTemplates.CreateMapReducePattern(llmClient, documents);

// Reflection pattern (self-correcting)
var reflectionGraph = GraphTemplates.CreateReflectionPattern(llmClient);
```

---

## 🌐 Supported Providers

| Provider | Base URL | Preset Method |
|:---------|:---------|:--------------|
| OpenAI | `https://api.openai.com/v1` | `CreateOpenAI()` |
| DeepSeek | `https://api.deepseek.com/v1` | `CreateDeepSeek()` |
| Qwen (Alibaba) | `https://dashscope.aliyuncs.com/compatible-mode/v1` | `CreateQwen()` |
| Mistral | `https://api.mistral.ai/v1` | `CreateMistral()` |
| Yi (01.AI) | `https://api.lingyiwanwu.com/v1` | `CreateYi()` |
| Groq | `https://api.groq.com/openai/v1` | `CreateGroq()` |
| Moonshot (Kimi) | `https://api.moonshot.cn/v1` | `CreateMoonshot()` |
| Zhipu GLM | `https://open.bigmodel.cn/api/paas/v4` | `CreateZhipu()` |
| Ollama (Local) | `http://localhost:11434` | `CreateOllama()` |
| **Any OpenAI-compatible** | Custom | `Create(key, url, model)` |

---

## 📁 Project Structure

```
SharpAIKit/
├── 📂 src/SharpAIKit/
│   ├── 📂 LLM/              # LLM clients
│   ├── 📂 Chain/            # Chain composition ⭐ NEW
│   ├── 📂 Memory/           # Conversation memory ⭐ NEW
│   ├── 📂 Prompt/           # Prompt templates ⭐ NEW
│   ├── 📂 Output/           # Output parsers ⭐ NEW
│   ├── 📂 DocumentLoader/   # Document loading ⭐ NEW
│   ├── 📂 Callback/         # Observability ⭐ NEW
│   ├── 📂 MultiModal/       # MultiModal ⭐ NEW
│   ├── 📂 Agent/            # Intelligent agents (ReAct/MultiAgent)
│   ├── 📂 RAG/              # RAG engine
│   ├── 📂 CodeInterpreter/  # Code interpreter 🔮 Killer Feature
│   ├── 📂 Graph/            # Graph orchestration 🕸️ Killer Feature
│   └── 📂 Optimizer/        # Auto optimization 🧬 Killer Feature
├── 📂 samples/              # Example projects
└── 📂 tests/                # Unit tests
```

---

## 📄 License

This project is licensed under the **MIT License**.

---

<div align="center">

### 🚀 SharpAIKit - LangChain for .NET, But Better!

**🎯 Three Killer Features LangChain Doesn't Have:**
- 🔮 **Native C# Code Interpreter** - No Python needed, native C# code execution
- 🕸️ **SharpGraph** - Graph orchestration with loops and complex branches
- 🧬 **DSPy Optimizer** - Auto-optimize prompts, gets smarter over time

**Made with ❤️ for the .NET community**

If this project helps you, please give it a ⭐ **Star**!

</div>
