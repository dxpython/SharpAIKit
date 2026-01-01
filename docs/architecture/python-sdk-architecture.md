# SharpAIKit Python SDK Architecture

## Overview

The SharpAIKit Python SDK (`sharpaikit`) provides a production-grade interface for Python applications to interact with the SharpAIKit .NET runtime via gRPC. This document describes the architecture, design decisions, and implementation details.

## Architecture Principles

### Separation of Concerns

- **SharpAIKit (C# Runtime)**: Behavior runtime, Skill resolution, Tool execution, Agent orchestration
- **sharpaikit (Python SDK)**: Control plane, process management, gRPC client, API encapsulation

### Python SDK Responsibilities

✅ **Allowed**:
- Process lifecycle management (start/stop host)
- gRPC communication
- API encapsulation and convenience methods
- Error handling and structured exceptions
- Audit information propagation

❌ **Not Allowed**:
- Implementing Skills (Skills are C# runtime concerns)
- Modifying behavior (behavior is controlled by Skills in C#)
- Prompt engineering (prompts are internal to C# runtime)
- Decision making (decisions are made by Skill resolver in C#)

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│              Python Application Layer                    │
│  from sharpaikit import Agent                           │
│  agent = Agent(...)                                      │
│  result = agent.run("task")                              │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│           Python SDK Layer (sharpaikit)                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   agent.py   │  │  client.py   │  │ process.py   │  │
│  │ (High-level) │  │ (gRPC Client)│  │ (Lifecycle)  │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │ gRPC
┌────────────────────▼────────────────────────────────────┐
│         gRPC Host Layer (C# .NET)                       │
│  ┌──────────────────────────────────────────────────┐  │
│  │  AgentServiceImpl (gRPC Service)                 │  │
│  │  - CreateAgent                                    │  │
│  │  - Execute                                        │  │
│  │  - ExecuteStream                                  │  │
│  │  - ListAvailableSkills                           │  │
│  │  - GetLastSkillResolution                        │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│         SharpAIKit Runtime (C# .NET)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │EnhancedAgent │  │SkillResolver │  │ToolExecutor  │  │
│  │              │  │              │  │              │  │
│  │- RunAsync    │  │- Resolve     │  │- Execute     │  │
│  │- Planning    │  │- Merge       │  │- Validate    │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## gRPC Protocol

### Service Definition

The gRPC service is defined in `proto/agent.proto`:

```protobuf
service AgentService {
  rpc CreateAgent(CreateAgentRequest) returns (CreateAgentResponse);
  rpc Execute(ExecuteRequest) returns (ExecuteResponse);
  rpc ExecuteStream(ExecuteRequest) returns (stream ExecuteStreamChunk);
  rpc ListAvailableSkills(ListSkillsRequest) returns (ListSkillsResponse);
  rpc GetLastSkillResolution(GetSkillResolutionRequest) returns (GetSkillResolutionResponse);
  rpc HealthCheck(HealthCheckRequest) returns (HealthCheckResponse);
}
```

### Message Flow

1. **CreateAgent**: Python creates an agent instance on the C# host
2. **Execute**: Python sends a task, C# executes and returns complete result
3. **ExecuteStream**: Python sends a task, C# streams execution chunks
4. **ListAvailableSkills**: Python queries available skills
5. **GetLastSkillResolution**: Python retrieves skill resolution for audit

### Skill Resolution Information

All execution responses include `SkillResolutionInfo`:

```protobuf
message SkillResolutionInfo {
  repeated string activated_skill_ids = 1;
  repeated string decision_reasons = 2;
  SkillConstraintsInfo constraints = 3;
  map<string, string> tool_denial_reasons = 4;
}
```

This ensures Python can:
- See which Skills were activated
- Understand why decisions were made
- Know which tools were denied and why
- Audit the complete decision process

## Process Management

### Host Lifecycle

The Python SDK manages the C# gRPC host process:

1. **Detection**: Checks if host is running (port check)
2. **Startup**: Launches host process if needed
3. **Readiness**: Waits for host to be ready (health check)
4. **Cleanup**: Gracefully shuts down host on Python exit

### Implementation Details

- Uses `subprocess.Popen` for process management
- Supports both compiled executable and `dotnet run`
- Automatic cleanup via `atexit` and context managers
- Timeout-based readiness checks

## Error Model

### Structured Exceptions

```python
SharpAIKitError (base)
├── ConnectionError          # Host connection failed
├── HostStartupError         # Host process failed to start
├── AgentNotFoundError       # Agent ID not found
├── ExecutionError           # Execution failed
│   └── denied_tools: List[str]  # Tools that were denied
└── SkillResolutionError     # Skill resolution failed
```

### Error Codes

All errors include error codes for programmatic handling:
- `CONNECTION_ERROR`
- `HOST_STARTUP_ERROR`
- `AGENT_NOT_FOUND`
- `EXECUTION_ERROR`
- `SKILL_RESOLUTION_ERROR`

## Python SDK API

### Synchronous API

```python
agent = Agent(api_key="...", model="gpt-4")
result = agent.run("Review this PR")
print(result.output)
print(result.skill_trace)
```

### Asynchronous API

```python
agent = Agent(api_key="...", model="gpt-4")
result = await agent.run_async("Review this PR")
```

### Streaming API

```python
agent = Agent(api_key="...", model="gpt-4")
for chunk in agent.run_stream("Generate a story"):
    if chunk.output:
        print(chunk.output, end="", flush=True)
```

## Skill System Integration

### Skill Activation

Skills are activated in the C# runtime based on:
- Task content (intent matching)
- Context information
- Skill activation conditions

Python cannot directly activate Skills, but can:
- Request specific Skills via `skill_ids` parameter
- Query available Skills via `list_available_skills()`
- Inspect Skill resolution via `get_skill_resolution()`

### Skill Constraints

Skill constraints are applied in C# runtime:
- Tool allow/deny lists
- Max steps
- Max execution time
- Custom validators

Python receives:
- List of denied tools
- Reasons for denial
- Applied constraints
- Decision reasons

## Observability & Auditability

### Skill Decision Trail

Every execution includes:
- Activated Skill IDs
- Decision reasons (human-readable)
- Applied constraints
- Tool denial reasons

### Example

```python
result = agent.run("Write to file")
if result.denied_tools:
    print(f"Denied tools: {result.denied_tools}")
    if result.skill_resolution:
        for tool in result.denied_tools:
            reason = result.skill_resolution.tool_denial_reasons.get(tool)
            print(f"  {tool}: {reason}")
```

## Implementation Details

### gRPC Code Generation

1. **C#**: Generated during build via `Grpc.Tools`
2. **Python**: Generated via `grpc_tools.protoc` (see `generate_grpc.py`)

### Host Process Detection

- Port check: `socket.connect_ex((host, port))`
- Health check: gRPC `HealthCheck` call
- Timeout: Configurable (default 30s)

### Error Handling

- All gRPC errors are caught and converted to structured exceptions
- Connection errors are retried (future enhancement)
- Host startup failures include stderr output

## Testing Strategy

### Unit Tests

- Mock gRPC client
- Test error handling
- Test process management

### Integration Tests

- Real gRPC host process
- Real Skill resolution
- Real tool execution

### End-to-End Tests

- Full Python → gRPC → C# Runtime flow
- Skill constraint enforcement
- Audit trail verification

## Performance Considerations

### Connection Pooling

- gRPC channels are reused
- One channel per Agent instance
- Channels are closed on cleanup

### Process Overhead

- Host process startup: ~1-2 seconds
- gRPC call latency: <10ms (local)
- Skill resolution: <50ms (typical)

### Memory

- Host process: ~50-100MB
- Python SDK: <10MB
- Per-agent: <1MB

## Security Considerations

### API Key Handling

- API keys are passed per-request
- Not stored in Python SDK
- Not logged (C# runtime handles logging)

### Process Isolation

- Host runs in separate process
- No shared memory
- gRPC provides secure communication

### Skill Constraints

- Skills enforce constraints in C# runtime
- Python cannot bypass constraints
- All denials are logged and reported

## Future Enhancements

- [ ] Connection pooling across agents
- [ ] Automatic retry with backoff
- [ ] Metrics and telemetry
- [ ] Async gRPC client (asyncio)
- [ ] WebSocket support for real-time updates
- [ ] Skill marketplace integration

## Conclusion

The SharpAIKit Python SDK provides a clean, production-ready interface for Python applications to leverage the full power of SharpAIKit's behavior runtime and Skill system, while maintaining clear separation of concerns and complete observability.

