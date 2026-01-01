#!/bin/bash
# Build script for sharpaikit Python package using uv

set -e

echo "🔨 Building SharpAIKit Python SDK..."
echo ""

# Step 1: Install build dependencies
echo "📦 Step 1: Installing build dependencies..."
uv pip install --system grpcio-tools grpcio 2>&1 | grep -v "already satisfied" || true

# Step 2: Generate gRPC code
echo ""
echo "🔧 Step 2: Generating gRPC code..."
python3 generate_grpc.py

# Step 3: Verify gRPC files exist
echo ""
echo "✅ Step 3: Verifying gRPC code..."
if [ ! -f "sharpaikit/_grpc/agent_pb2.py" ] || [ ! -f "sharpaikit/_grpc/agent_pb2_grpc.py" ]; then
    echo "❌ Error: gRPC code generation failed"
    exit 1
fi
echo "✅ gRPC code generated successfully"

# Step 4: Build package
echo ""
echo "📦 Step 4: Building package..."
uv build

# Step 5: Show results
echo ""
echo "✅ Build complete!"
echo ""
echo "📦 Generated packages:"
ls -lh dist/
echo ""
echo "📝 To install:"
echo "   uv pip install dist/sharpaikit-0.3.0-py3-none-any.whl"
echo ""
echo "📝 Or install in development mode:"
echo "   uv pip install --system -e ."

