# SharpAIKit Python SDK

Official Python SDK for SharpAIKit - .NET AI/LLM Toolkit

## 🎯 Feature Overview

The Python SDK calls all C# services via gRPC, providing complete SharpAIKit functionality:

### Core Features

- ✅ **Agent Execution** - Synchronous/async/streaming execution, supports ReAct, Plan-Execute, and other modes
- ✅ **Skill System** - Complete Skill governance capabilities, constraint merging, audit trails
- ✅ **Tool Execution** - Tool registration, execution, validation, full support
- ✅ **Chain (LCEL)** - Chain invocation, pipe composition, parallel execution
- ✅ **Memory** - 5 memory strategies (Buffer/Window/Summary/Vector/Entity)
- ✅ **RAG** - Document indexing, vector search, intelligent Q&A, streaming responses
- ✅ **SharpGraph** - Graph orchestration, state management, loops and branches
- ✅ **Prompt** - Template system, variable substitution, Chat templates, Few-shot
- ✅ **Output Parser** - JSON/Boolean/List/XML/Regex parsers
- ✅ **Document Loader** - Multi-format support (Text/CSV/JSON/Markdown/Web)
- ✅ **Code Interpreter** - Native C# code execution, based on Roslyn
- ✅ **Optimizer** - DSPy-style automatic prompt optimization
- ✅ **Observability** - Observability, logging, metrics tracking
- ✅ **Context Management** - Strongly-typed context, context passing
- ✅ **Process Management** - Automatic start/stop of gRPC host

## 📦 Installation

### Using uv (Recommended)

```bash
cd python-client

# Install dependencies
uv pip install --system grpcio grpcio-tools

# Generate gRPC code
python3 generate_grpc.py

# Install package
uv pip install --system -e .
```

### Build Distribution Package

```bash
# Build wheel and source distribution
uv build

# Install from built package
uv pip install --system dist/sharpaikit-0.3.0-py3-none-any.whl
```

### Install from PyPI

```bash
pip install sharpaikit
```

## 🚀 Quick Start

```python
from sharpaikit import Agent

# Create agent (automatically starts host if needed)
agent = Agent(
    api_key="YOUR-API-KEY",
    model="gpt-4",
    auto_start_host=True
)

# Run a task
result = agent.run("Hello, world!")

print(result.output)
print(f"Success: {result.success}")
print(f"Steps: {len(result.steps)}")

# Cleanup
agent.close()
```

## 📖 Examples

### Basic Usage

```python
from sharpaikit import Agent

agent = Agent(
    api_key="YOUR-API-KEY",
    base_url="https://api.openai.com/v1",
    model="gpt-3.5-turbo",
    auto_start_host=True
)

result = agent.run("Hello, please introduce yourself in one sentence")
print(result.output)
agent.close()
```

### With Skills

```python
agent = Agent(
    api_key="YOUR-API-KEY",
    model="gpt-4",
    skills=["code-review", "security-policy"],
    auto_start_host=True
)

result = agent.run("Review this code for security issues")

# Check skill resolution
if result.skill_resolution:
    print(f"Activated skills: {result.skill_resolution.activated_skill_ids}")
    print(f"Denied tools: {result.denied_tools}")
```

### Streaming

```python
for chunk in agent.run_stream("Tell me a story"):
    if chunk.output:
        print(chunk.output, end="", flush=True)
```

### Error Handling

```python
from sharpaikit.errors import ExecutionError, ConnectionError

try:
    result = agent.run("Task")
except ExecutionError as e:
    print(f"Execution failed: {e}")
    if e.denied_tools:
        print(f"Denied tools: {e.denied_tools}")
except ConnectionError as e:
    print(f"Connection failed: {e}")
```

## 📚 Documentation

- [Feature Coverage](FEATURE_COVERAGE.md) - Detailed feature coverage analysis
- [Features Guide](README_FEATURES.md) - Feature descriptions and usage examples
- [Quick Test](QUICK_TEST.md) - Quick test guide
- [Summary](SUMMARY.md) - Feature summary

## 🎯 Comprehensive Demo

Run the comprehensive demo to see all features:

```bash
# Using script
./run_demo.sh

# Or manually
python3 examples/comprehensive_demo.py
```

The demo includes:
1. Basic Agent execution
2. Skill system integration
3. Streaming execution
4. Context passing
5. Error handling
6. Skill resolution details

## 📊 Feature Coverage

| Category | Status | Coverage | Notes |
|:--------|:------|:---------|:------|
| **Agent Execution** | ✅ Full | 100% | Synchronous/async/streaming execution, full support |
| **Skill System** | ✅ Full | 100% | Complete Skill governance, constraint merging, audit trails |
| **Tool Execution** | ✅ Full | 100% | Tool registration, execution, validation, full support |
| **Chain (LCEL)** | ✅ Full | 100% | Chain invocation, pipe composition, parallel execution |
| **Memory** | ✅ Full | 100% | 5 memory strategies (Buffer/Window/Summary/Vector/Entity) |
| **RAG** | ✅ Full | 100% | Document indexing, vector search, intelligent Q&A, streaming responses |
| **SharpGraph** | ✅ Full | 100% | Graph orchestration, state management, loops and branches |
| **Prompt** | ✅ Full | 100% | Template system, variable substitution, Chat templates, Few-shot |
| **Output Parser** | ✅ Full | 100% | JSON/Boolean/List/XML/Regex parsers |
| **Document Loader** | ✅ Full | 100% | Multi-format support (Text/CSV/JSON/Markdown/Web) |
| **Code Interpreter** | ✅ Full | 100% | Native C# code execution, based on Roslyn |
| **Optimizer** | ✅ Full | 100% | DSPy-style automatic prompt optimization |
| **Observability** | ✅ Full | 100% | Observability, logging, metrics tracking |
| **Context Management** | ✅ Full | 100% | Strongly-typed context, context passing |

**Overall Coverage: ~100%** ⭐ **All core features are fully implemented**

### Feature Descriptions

- ✅ **Agent**: Complete Agent execution capabilities, supports ReAct, Plan-Execute, and other modes
- ✅ **Skills**: Enterprise-level behavior governance, supports tool constraints, execution limits, context injection
- ✅ **Chain**: LCEL-style chain invocation, supports pipe composition and parallel execution
- ✅ **Memory**: 5 memory strategies, supports conversation history management
- ✅ **RAG**: Complete retrieval-augmented generation, supports document indexing and vector search
- ✅ **Graph**: Finite state machine-based graph orchestration, supports loops and complex branches
- ✅ **Other Services**: Prompt, OutputParser, DocumentLoader, CodeInterpreter, Optimizer, Observability are all fully implemented

All services call C# implementations via gRPC, providing complete type safety and error handling.

See [FEATURE_COVERAGE.md](FEATURE_COVERAGE.md) for detailed analysis.

## 🔧 Requirements

- Python 3.8+
- .NET 8.0 SDK (for building gRPC host)
- grpcio >= 1.60.0
- grpcio-tools >= 1.60.0

## 📝 API Reference

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

### Methods

- `run(task, tools=None, context=None)` - Execute synchronously
- `run_async(task, tools=None, context=None)` - Execute asynchronously
- `run_stream(task, tools=None, context=None)` - Stream results
- `get_skill_resolution()` - Get last skill resolution
- `list_available_skills()` - List all available skills
- `close()` - Cleanup resources

## 🎯 Use Cases

The Python SDK supports all SharpAIKit features and is suitable for:

- ✅ **Agent Task Execution** - Complete Agent execution capabilities
- ✅ **Skill-Driven Governance** - Enterprise-level behavior constraints and governance
- ✅ **Chain Invocation** - LCEL-style chain composition
- ✅ **Conversation Memory** - Multiple memory strategy management
- ✅ **RAG Retrieval** - Document indexing and intelligent Q&A
- ✅ **Graph Orchestration** - Complex workflow graph orchestration
- ✅ **Code Execution** - Native C# code interpreter
- ✅ **Prompt Optimization** - Automatic prompt optimization
- ✅ **Cross-Language Calls** - Python calls C# services
- ✅ **Platform Integration** - Enterprise-level platform integration

**All features are fully implemented with no limitations!**

## 📄 License

Same as SharpAIKit project.
