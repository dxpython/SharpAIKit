<div align="center">

## SharpAIKit：A Unified AI/LLM Toolkit for .NET
<img src="imgs/logo.jpg" alt="SharpAIKit Logo" width="900">


[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-v1.0.0-004880?style=for-the-badge&logo=nuget&logoColor=white)](https://www.nuget.org/)

[中文文档](README_CN.md) | [🏠 Home](README.md)

</div>

---

## 📋 Table of Contents

- [SharpAIKit：A Unified AI/LLM Toolkit for .NET](#sharpaikita-unified-aillm-toolkit-for-net)
- [📋 Table of Contents](#-table-of-contents)
- [✨ Features](#-features)
- [📦 Installation](#-installation)
- [🚀 Quick Start](#-quick-start)
  - [Core Concept](#core-concept)
  - [Method 1: Universal (Recommended)](#method-1-universal-recommended)
  - [Method 2: Preset Shortcuts](#method-2-preset-shortcuts)
  - [Chat](#chat)
- [🌐 Supported Providers](#-supported-providers)
- [🖥️ Local Models](#️-local-models)
  - [Ollama](#ollama)
  - [LM Studio](#lm-studio)
- [📚 RAG Engine](#-rag-engine)
- [🤖 AI Agent](#-ai-agent)
- [📁 Project Structure](#-project-structure)
- [📄 License](#-license)

---

## ✨ Features

| Feature | Description |
|:--------|:------------|
| 🔌 **Unified API** | One interface for all OpenAI-compatible LLMs |
| 🌊 **Streaming** | Real-time token-by-token output |
| 📚 **RAG Engine** | Built-in document indexing and Q&A |
| 🤖 **AI Agent** | Tool calling with automatic planning |
| 🔄 **Retry Policy** | Built-in Polly retry mechanism |
| ⚡ **Async/Await** | Full async support for .NET 8 |

---

## 📦 Installation

```bash
dotnet add package SharpAIKit
```

---

## 🚀 Quick Start

### Core Concept

> **Most LLM APIs are OpenAI-compatible. Just provide URL + API Key to support ANY model!**

### Method 1: Universal (Recommended)

```csharp
using SharpAIKit.LLM;

// Works with ANY OpenAI-compatible API
var client = LLMClientFactory.Create(
    apiKey: "your-api-key",
    baseUrl: "https://api.xxx.com/v1",
    model: "model-name"
);

// Examples
var openai = LLMClientFactory.Create("sk-xxx", "https://api.openai.com/v1", "gpt-4o");
var deepseek = LLMClientFactory.Create("sk-xxx", "https://api.deepseek.com/v1", "deepseek-chat");
var qwen = LLMClientFactory.Create("sk-xxx", "https://dashscope.aliyuncs.com/compatible-mode/v1", "qwen-turbo");
```

### Method 2: Preset Shortcuts

```csharp
var openai = LLMClientFactory.CreateOpenAI("your-api-key");
var deepseek = LLMClientFactory.CreateDeepSeek("your-api-key");
var qwen = LLMClientFactory.CreateQwen("your-api-key");
var mistral = LLMClientFactory.CreateMistral("your-api-key");
var groq = LLMClientFactory.CreateGroq("your-api-key");
```

### Chat

```csharp
// Simple chat
var response = await client.ChatAsync("Hello, how are you?");
Console.WriteLine(response);

// Streaming output
await foreach (var chunk in client.ChatStreamAsync("Tell me a story"))
{
    Console.Write(chunk);
}

// Multi-turn conversation
var messages = new List<ChatMessage>
{
    ChatMessage.System("You are a helpful assistant."),
    ChatMessage.User("What is C#?"),
};
var reply = await client.ChatAsync(messages);
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
| Baichuan | `https://api.baichuan-ai.com/v1` | `CreateBaichuan()` |
| Together AI | `https://api.together.xyz/v1` | `CreateTogether()` |
| **Any OpenAI-compatible** | Custom URL | `Create(key, url, model)` |

---

## 🖥️ Local Models

### Ollama

```csharp
// Supports: Llama, Qwen, Phi, Mistral, Gemma, DeepSeek, Yi...
var llama = LLMClientFactory.CreateOllama("llama3.2");
var qwen = LLMClientFactory.CreateOllama("qwen2.5");
var deepseek = LLMClientFactory.CreateOllama("deepseek-v2");
var phi = LLMClientFactory.CreateOllama("phi4");
```

### LM Studio

```csharp
var local = LLMClientFactory.CreateLMStudio();
```

---

## 📚 RAG Engine

```csharp
using SharpAIKit.RAG;

var rag = new RagEngine(client);

// Index documents
await rag.IndexDocumentAsync("docs/guide.txt");
await rag.IndexContentAsync("Your document content here...");

// Ask questions
var answer = await rag.AskAsync("How to use this feature?");
Console.WriteLine(answer);

// Streaming answer
await foreach (var chunk in rag.AskStreamAsync("What is RAG?"))
{
    Console.Write(chunk);
}
```

---

## 🤖 AI Agent

```csharp
using SharpAIKit.Agent;

var agent = new AiAgent(client);

// Add tools
agent.AddTool(new CalculatorTool());
agent.AddTool(new WebSearchTool());
agent.AddTool(new FileWriterTool());

// Run task
var result = await agent.RunAsync("Calculate 3 to the power of 5");
Console.WriteLine(result.Answer);  // 243

// View execution steps
foreach (var step in result.Steps)
{
    Console.WriteLine($"[{step.Type}] {step.ToolName}: {step.Result}");
}
```

---

## 📁 Project Structure

```
SharpAIKit/
├── 📂 src/SharpAIKit/
│   ├── 📂 LLM/
│   │   ├── ILLMClient.cs          # Interface
│   │   ├── BaseLLMClient.cs       # Base class
│   │   ├── OpenAIClient.cs        # Universal client
│   │   ├── OllamaClient.cs        # Local Ollama
│   │   └── LLMClientFactory.cs    # Factory
│   ├── 📂 RAG/
│   │   ├── RagEngine.cs           # RAG engine
│   │   ├── TextSplitter.cs        # Text chunking
│   │   ├── MemoryVectorStore.cs   # Vector store
│   │   └── Similarity.cs          # Similarity metrics
│   └── 📂 Agent/
│       ├── AiAgent.cs             # AI Agent
│       ├── ToolBase.cs            # Tool base class
│       └── CalculatorTool.cs      # Calculator tool
├── 📂 samples/                    # Example projects
│   ├── ChatDemo/
│   ├── RAGDemo/
│   └── AgentDemo/
└── 📂 tests/                      # Unit tests
```

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Made with ❤️ for the .NET community**

If this project helps you, please give it a ⭐ **Star**!

</div>

