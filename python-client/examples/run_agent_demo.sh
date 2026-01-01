#!/bin/bash
# Script to build and run the Agent demo

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PYTHON_CLIENT_DIR="$SCRIPT_DIR/.."

echo "=========================================="
echo "SharpAIKit Python SDK - Agent Demo"
echo "=========================================="
echo

# Check if .NET SDK is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET SDK first."
    exit 1
fi

# Build C# gRPC host
echo "🔨 Building C# gRPC host..."
cd "$PROJECT_ROOT"
dotnet build src/SharpAIKit.Grpc.Host/SharpAIKit.Grpc.Host.csproj -c Release
echo "✅ Build completed"
echo

# Generate gRPC Python code
echo "🔧 Generating gRPC Python code..."
cd "$PYTHON_CLIENT_DIR"
python3 generate_grpc.py
echo "✅ gRPC code generated"
echo

# Install Python dependencies
echo "📦 Installing Python dependencies..."
if [ ! -d "venv" ]; then
    echo "Creating virtual environment..."
    uv venv
fi

source venv/bin/activate || . venv/bin/activate

uv pip install -e . --quiet
echo "✅ Dependencies installed"
echo

# Set environment variables if not set
if [ -z "$OPENAI_API_KEY" ] && [ -z "$DEEPSEEK_API_KEY" ]; then
    echo "⚠️  Warning: No API key found in environment variables"
    echo "   Please set OPENAI_API_KEY or DEEPSEEK_API_KEY"
    echo "   You can also edit the demo script to set your API key"
    echo
fi

# Run the demo
echo "🚀 Running Agent demo..."
echo
python3 examples/agent_demo.py

