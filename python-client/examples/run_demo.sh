#!/bin/bash
# Run comprehensive demo using uv

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "🔨 Building and running SharpAIKit Python SDK comprehensive demo..."
echo ""

cd "$PROJECT_ROOT"

# Check if gRPC host is built
HOST_DLL="src/SharpAIKit.Grpc.Host/bin/Release/net8.0/SharpAIKit.Grpc.Host.dll"
if [ ! -f "$HOST_DLL" ]; then
    echo "📦 Building gRPC host..."
    dotnet build src/SharpAIKit.Grpc.Host/SharpAIKit.Grpc.Host.csproj -c Release
fi

cd python-client

# Ensure gRPC code is generated
if [ ! -f "sharpaikit/_grpc/agent_pb2.py" ] || [ ! -f "sharpaikit/_grpc/agent_pb2_grpc.py" ]; then
    echo "🔧 Generating gRPC code..."
    uv pip install --system grpcio-tools 2>&1 | grep -v "already satisfied" || true
    python3 generate_grpc.py
fi

# Install package if not installed
echo "📦 Installing sharpaikit..."
uv pip install --system -e . 2>&1 | grep -v "already satisfied" || true

# Run demo (will auto-start host)
echo ""
echo "🚀 Running comprehensive demo..."
echo "Note: gRPC host will be automatically started if needed"
echo ""
python3 examples/comprehensive_demo.py

