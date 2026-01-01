# SharpAIKit Python SDK

Official Python SDK for SharpAIKit - .NET AI/LLM Toolkit

## 🎯 功能概览

Python SDK 通过 gRPC 调用 C# 端的 `EnhancedAgent`，支持以下核心功能：

- ✅ **Agent 执行** - 同步/异步/流式执行
- ✅ **Skill 系统** - 完整的 Skill 治理能力
- ✅ **工具执行** - 通过 C# 端执行工具
- ✅ **上下文传递** - 支持上下文信息
- ✅ **错误处理** - 结构化异常处理
- ✅ **进程管理** - 自动启动/关闭 gRPC 主机

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

## 🚀 Quick Start

```python
from sharpaikit import Agent

# Create agent (automatically starts host if needed)
agent = Agent(
    api_key="your-api-key",
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
    api_key="sk-502f0625194247d4adc2a9c7659c0ffe",
    base_url="https://dashscope.aliyuncs.com/compatible-mode/v1",
    model="qwen-plus",
    auto_start_host=True
)

result = agent.run("你好，请用一句话介绍你自己")
print(result.output)
agent.close()
```

### With Skills

```python
agent = Agent(
    api_key="your-api-key",
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

- [Feature Coverage](FEATURE_COVERAGE.md) - 详细的功能覆盖分析
- [Features Guide](README_FEATURES.md) - 功能说明和使用示例
- [Quick Test](QUICK_TEST.md) - 快速测试指南
- [Summary](SUMMARY.md) - 功能总结

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

| Category | Status | Coverage |
|:--------|:------|:---------|
| Agent Execution | ✅ Full | 100% |
| Skill System | ✅ Full | 100% |
| Tool Execution | ⚠️ Partial | 70% |
| Chain (LCEL) | ❌ Not supported | 0% |
| Memory | ❌ Not supported | 0% |
| RAG | ❌ Not supported | 0% |
| Code Interpreter | ❌ Not supported | 0% |
| SharpGraph | ❌ Not supported | 0% |

**Overall Coverage: ~26%** (Core Agent features are complete)

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

Python SDK is ideal for:

- ✅ Agent task execution
- ✅ Skill-driven behavior governance
- ✅ Cross-language Agent calls
- ✅ Platform integration

Not suitable for:

- ❌ Complex chain orchestration (needs C# implementation)
- ❌ Document processing and RAG (needs extended interface)
- ❌ Graph orchestration (needs extended interface)

## 📄 License

Same as SharpAIKit project.
