# gRPC Setup Guide

## Overview

The SharpAIKit Python SDK uses gRPC for communication between Python and the C# runtime. This document explains how to set up and generate the required gRPC code.

## Prerequisites

1. **Protocol Buffers Compiler** (`protoc`)
   - Install from: https://grpc.io/docs/protoc-installation/
   - Or via package manager:
     - macOS: `brew install protobuf`
     - Ubuntu: `apt-get install protobuf-compiler`
     - Windows: Download from GitHub releases

2. **Python gRPC Tools**
   ```bash
   pip install grpcio-tools
   ```

## Generating gRPC Code

### For Python

```bash
cd python-client
python generate_grpc.py
```

This will generate:
- `sharpaikit/_grpc/agent_pb2.py` - Protocol buffer message classes
- `sharpaikit/_grpc/agent_pb2_grpc.py` - gRPC service stubs

### Manual Generation

If the script doesn't work, generate manually:

```bash
cd python-client
python -m grpc_tools.protoc \
    --proto_path=../../proto \
    --python_out=sharpaikit/_grpc \
    --grpc_python_out=sharpaikit/_grpc \
    ../../proto/agent.proto
```

### For C#

C# gRPC code is automatically generated during build via `Grpc.Tools` package.

## Verifying Generation

After generation, you should see:

```bash
ls sharpaikit/_grpc/
# agent_pb2.py
# agent_pb2_grpc.py
# __init__.py
```

## Troubleshooting

### "protoc: command not found"

Install Protocol Buffers compiler (see Prerequisites).

### "ModuleNotFoundError: No module named 'grpc_tools'"

```bash
pip install grpcio-tools
```

### Import Errors

Ensure generated files are in `sharpaikit/_grpc/` and `__init__.py` exists.

## Updating Proto Files

If you modify `proto/agent.proto`:

1. Regenerate Python code: `python generate_grpc.py`
2. Rebuild C# project: `dotnet build src/SharpAIKit.Grpc/`
3. Restart gRPC host

