# SharpAIKit Python SDK Installation Guide

## Prerequisites

1. **Python 3.8+**
2. **.NET 8.0 SDK** (for building the gRPC host)
3. **uv** (recommended) or **pip**

## Installation Steps

### Step 1: Build the gRPC Host

```bash
cd ../src/SharpAIKit.Grpc.Host
dotnet build -c Release
```

### Step 2: Generate Python gRPC Code

```bash
cd ../../python-client

# Install grpc-tools if not already installed
uv pip install grpcio-tools

# Generate gRPC code
python generate_grpc.py
```

This will generate:
- `sharpaikit/_grpc/agent_pb2.py`
- `sharpaikit/_grpc/agent_pb2_grpc.py`

### Step 3: Install the Python SDK

#### Using uv (Recommended)

```bash
uv pip install -e .
```

#### Using pip

```bash
pip install -e .
```

## Verification

```python
from sharpaikit import Agent

# This will automatically start the host if needed
agent = Agent(
    api_key="YOUR-API-KEY",
    model="gpt-3.5-turbo",
    auto_start_host=True
)

# Test connection
result = agent.run("Hello")
print(result.output)
```

## Troubleshooting

### gRPC Code Not Generated

If you see `ImportError: gRPC code not generated`, run:

```bash
python generate_grpc.py
```

### Host Process Fails to Start

1. Ensure .NET 8.0 SDK is installed: `dotnet --version`
2. Build the host: `cd ../src/SharpAIKit.Grpc.Host && dotnet build`
3. Check port 50051 is available

### Connection Errors

1. Verify host is running: `lsof -i :50051`
2. Check firewall settings
3. Verify host address (default: localhost:50051)

