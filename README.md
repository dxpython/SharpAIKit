<div align="center">

## SharpAIKit: A Unified AI/LLM Toolkit for .NET
<img src="imgs/logo.jpg" alt="SharpAIKit Logo" width="900">

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-v2.0.0-004880?style=for-the-badge&logo=nuget&logoColor=white)](https://www.nuget.org/)
[![Build](https://img.shields.io/badge/Build-Passing-brightgreen?style=for-the-badge)](https://github.com/)

<br/>

**One API, All LLMs** — Supports OpenAI, DeepSeek, Qwen, Mistral, Yi, Groq and any OpenAI-compatible API

**🎯 Killer Features Beyond LangChain:**
- 🔮 **Native C# Code Interpreter** - Execute C# code directly, no Python needed
- 🕸️ **SharpGraph** - Graph orchestration with loops and complex branches
- 🧬 **DSPy Optimizer** - Auto-optimize prompts, gets smarter over time

<br/>

---

### 📖 Documentation

<br/>

[![English](https://img.shields.io/badge/English-Documentation-blue?style=for-the-badge&logo=readme&logoColor=white)](README_EN.md)
[![中文](https://img.shields.io/badge/中文-文档-red?style=for-the-badge&logo=readme&logoColor=white)](README_CN.md)

<br/>

---

</div>

## ⚡ Quick Example

```csharp
using SharpAIKit.LLM;

// Works with ANY OpenAI-compatible API
var client = LLMClientFactory.Create("api-key", "https://api.deepseek.com/v1", "deepseek-chat");

// Chat
var response = await client.ChatAsync("Hello!");

// Streaming
await foreach (var chunk in client.ChatStreamAsync("Tell me a story"))
{
    Console.Write(chunk);
}
```

## 📦 Installation

```bash
dotnet add package SharpAIKit
```

## 🌐 Supported Providers

| Provider | URL |
|:---------|:----|
| OpenAI | `https://api.openai.com/v1` |
| DeepSeek | `https://api.deepseek.com/v1` |
| Qwen (Alibaba) | `https://dashscope.aliyuncs.com/compatible-mode/v1` |
| Mistral | `https://api.mistral.ai/v1` |
| Yi (01.AI) | `https://api.lingyiwanwu.com/v1` |
| Groq | `https://api.groq.com/openai/v1` |
| Moonshot (Kimi) | `https://api.moonshot.cn/v1` |
| Ollama (Local) | `http://localhost:11434` |
| **Any OpenAI-compatible** | Custom URL |

---

<div align="center">

**⭐ Star this project if it helps you!**

---

## 🎯 Killer Features

### 🔮 Native C# Code Interpreter
Execute C# code directly using Roslyn - no Python dependency, blazing fast!

```csharp
var interpreter = new RoslynCodeInterpreter();
var result = await interpreter.ExecuteAsync<double>("Math.Pow(3, 5)");
// Result: 243
```

### 🕸️ SharpGraph
Graph-based orchestration with FSM - handle loops and complex workflows!

```csharp
var graph = new SharpGraphBuilder("start")
    .Node("start", async state => { ... })
    .Edge("start", "next", condition: state => ...)
    .Build();
```

### 🧬 DSPy-style Optimizer
Automatically optimize prompts through iterative improvement!

```csharp
var optimizer = new DSPyOptimizer(client)
    .AddExample("input", "expected output")
    .SetMetric(Metrics.Contains);
var result = await optimizer.OptimizeAsync("initial prompt");
```

See [中文文档](README_CN.md) for detailed examples.

</div>
