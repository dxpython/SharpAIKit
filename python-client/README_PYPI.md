# SharpAIKit Python SDK

[![PyPI version](https://badge.fury.io/py/sharpaikit.svg)](https://badge.fury.io/py/sharpaikit)
[![Python 3.8+](https://img.shields.io/badge/python-3.8+-blue.svg)](https://www.python.org/downloads/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Official Python SDK for **SharpAIKit** - A powerful .NET AI/LLM toolkit that surpasses LangChain in functionality, performance, and developer experience.

## 🚀 Features

- ✅ **Agent Execution** - Synchronous, asynchronous, and streaming execution
- ✅ **Skill System** - Enterprise-grade behavior governance
- ✅ **Chain (LCEL)** - LangChain-style chain composition
- ✅ **Memory** - Multiple memory strategies (Buffer, Window, Summary, Vector, Entity)
- ✅ **RAG** - Retrieval-Augmented Generation with document indexing
- ✅ **SharpGraph** - Graph-based agent orchestration
- ✅ **Code Interpreter** - Native C# code execution
- ✅ **DSPy Optimizer** - Automatic prompt optimization
- ✅ **Multi-Modal** - Image and vision support
- ✅ **Observability** - Logging, metrics, and tracing

## 📦 Installation

```bash
pip install sharpaikit
```

### Prerequisites

- Python 3.8 or higher
- .NET 8.0 SDK (for building the gRPC host - see setup below)

## 🎯 Quick Start

### Basic Usage

```python
from sharpaikit import Agent

# Create agent (automatically starts gRPC host if needed)
agent = Agent(
    api_key="your-api-key",
    base_url="https://api.openai.com/v1",
    model="gpt-3.5-turbo",
    auto_start_host=True
)

# Run a task
result = agent.run("What is 2 + 2?")
print(result.output)
print(f"Success: {result.success}")
print(f"Steps: {len(result.steps)}")

# Cleanup
agent.close()
```

### With Tools

```python
tools = [
    {
        "name": "calculator",
        "description": "Performs arithmetic operations",
        "parameters": [
            {
                "name": "expression",
                "type": "string",
                "description": "Mathematical expression",
                "required": True
            }
        ]
    }
]

result = agent.run(
    task="Calculate 25 * 37",
    tools=tools
)
```

### Streaming

```python
for chunk in agent.run_stream("Tell me a story"):
    if chunk.output:
        print(chunk.output, end="", flush=True)
```

### Using Other Services

```python
from sharpaikit import Chain, Memory, RAG, Graph
from sharpaikit import UnifiedGrpcClient

client = UnifiedGrpcClient(host="localhost", port=50051)

# Chain service
chain = Chain(client)
chain.create_llm_chain("my_chain", api_key="...", model="gpt-4")
result = chain.invoke("my_chain", context={"input": "Hello"})

# Memory service
memory = Memory(client)
memory.create("my_memory", "Buffer")
memory.add_message("my_memory", ChatMessage(role="user", content="Hello"))

# RAG service
rag = RAG(client)
rag.create("my_rag", api_key="...")
rag.index_content("my_rag", "Your document content here")
answer = rag.ask("my_rag", "What is this about?")
```

## 🔧 Setup gRPC Host

The Python SDK communicates with a C# gRPC host. You need to build and run it:

```bash
# Clone the repository
git clone https://github.com/dxpython/SharpAIKit.git
cd SharpAIKit

# Build the gRPC host
dotnet build src/SharpAIKit.Grpc.Host/SharpAIKit.Grpc.Host.csproj -c Release

# Run the host (or let the SDK auto-start it)
dotnet run --project src/SharpAIKit.Grpc.Host/SharpAIKit.Grpc.Host.csproj
```

The SDK can automatically start the host if `auto_start_host=True` (default).

## 📚 Documentation

- [GitHub Repository](https://github.com/dxpython/SharpAIKit)
- [Full Documentation](https://github.com/dxpython/SharpAIKit#readme)
- [Examples](https://github.com/dxpython/SharpAIKit/tree/main/python-client/examples)

## 🎯 Supported Services

| Service | Status | Description |
|:-------|:------|:------------|
| Agent | ✅ Full | Agent execution with tools and skills |
| Chain | ✅ Full | LCEL-style chain composition |
| Memory | ✅ Full | Conversation memory management |
| RAG | ✅ Full | Retrieval-Augmented Generation |
| Graph | ✅ Full | SharpGraph orchestration |
| Prompt | ✅ Full | Prompt template management |
| OutputParser | ✅ Full | Output parsing utilities |
| DocumentLoader | ✅ Full | Multi-format document loading |
| CodeInterpreter | ✅ Full | C# code execution |
| Optimizer | ✅ Full | DSPy-style prompt optimization |
| Tool | ✅ Full | Tool registration and management |
| Observability | ✅ Full | Logging, metrics, and tracing |

## 🔑 API Reference

### Agent Class

```python
agent = Agent(
    api_key: str,
    model: str = "gpt-3.5-turbo",
    base_url: str = "https://api.openai.com/v1",
    skills: Optional[List[str]] = None,
    agent_id: Optional[str] = None,
    host: str = "localhost",
    port: int = 50051,
    auto_start_host: bool = True,
)
```

**Methods:**
- `run(task, tools=None, context=None)` - Execute synchronously
- `run_stream(task, tools=None, context=None)` - Stream results
- `run_async(task, tools=None, context=None)` - Execute asynchronously
- `get_skill_resolution()` - Get last skill resolution
- `list_available_skills()` - List all available skills
- `close()` - Cleanup resources

### Other Services

All services follow a similar pattern:

```python
from sharpaikit import Chain, Memory, RAG, Graph, Prompt, OutputParser
from sharpaikit import DocumentLoader, CodeInterpreter, Optimizer, Tool, Observability

client = UnifiedGrpcClient()
service = ServiceName(client)
# Use service methods...
```

## 🛠️ Error Handling

```python
from sharpaikit.errors import (
    SharpAIKitError,
    ExecutionError,
    ConnectionError,
    AgentNotFoundError,
    SkillResolutionError,
)

try:
    result = agent.run("Task")
except ExecutionError as e:
    print(f"Execution failed: {e}")
    if e.denied_tools:
        print(f"Denied tools: {e.denied_tools}")
except ConnectionError as e:
    print(f"Connection failed: {e}")
```

## 📋 Requirements

- Python 3.8+
- grpcio >= 1.60.0
- grpcio-tools >= 1.60.0
- httpx >= 0.24.0
- .NET 8.0 SDK (for gRPC host)

## 🤝 Contributing

Contributions are welcome! Please see the [main repository](https://github.com/dxpython/SharpAIKit) for contribution guidelines.

## 📄 License

MIT License - see LICENSE file for details.

## 🙏 Acknowledgments

- Built on top of SharpAIKit (.NET)
- Inspired by LangChain and other AI frameworks
- Powered by gRPC for high-performance cross-language communication

## 🔗 Links

- **GitHub**: https://github.com/dxpython/SharpAIKit
- **PyPI**: https://pypi.org/project/sharpaikit/
- **Documentation**: https://github.com/dxpython/SharpAIKit#readme

## 📞 Support

For issues and questions:
- GitHub Issues: https://github.com/dxpython/SharpAIKit/issues
- Discussions: https://github.com/dxpython/SharpAIKit/discussions

---

**Made with ❤️ by the SharpAIKit Team**

