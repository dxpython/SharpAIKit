#!/bin/bash
# Script to build and publish SharpAIKit Python SDK to PyPI

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=========================================="
echo "SharpAIKit Python SDK - PyPI Publisher"
echo "=========================================="
echo

# Check prerequisites
if ! command -v python3 &> /dev/null; then
    echo "❌ Python 3 not found"
    exit 1
fi

# Check if uv is available
if command -v uv &> /dev/null; then
    USE_UV=true
    echo "✅ Using uv for build"
else
    USE_UV=false
    echo "⚠️  uv not found, using standard build tools"
    if ! python3 -m pip show build twine &> /dev/null; then
        echo "Installing build tools..."
        python3 -m pip install --quiet build twine
    fi
fi

echo

# Step 1: Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf dist/ build/ *.egg-info/ .eggs/
echo "✅ Cleaned"

# Step 2: Generate gRPC code
echo
echo "🔧 Generating gRPC code..."
if [ ! -f "generate_grpc.py" ]; then
    echo "❌ generate_grpc.py not found"
    exit 1
fi

python3 generate_grpc.py
if [ $? -ne 0 ]; then
    echo "❌ Failed to generate gRPC code"
    exit 1
fi
echo "✅ gRPC code generated"

# Step 3: Verify gRPC files exist
echo
echo "🔍 Verifying gRPC files..."
if [ ! -f "sharpaikit/_grpc/sharpaikit_pb2.py" ] || [ ! -f "sharpaikit/_grpc/sharpaikit_pb2_grpc.py" ]; then
    echo "❌ gRPC files not found. Please run generate_grpc.py first"
    exit 1
fi
echo "✅ gRPC files verified"

# Step 4: Build package
echo
echo "📦 Building package..."
if [ "$USE_UV" = true ]; then
    uv build
else
    python3 -m build
fi

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi
echo "✅ Build successful"

# Step 5: Check package
echo
echo "🔍 Checking package..."
if command -v twine &> /dev/null; then
    twine check dist/*
    if [ $? -ne 0 ]; then
        echo "❌ Package check failed"
        exit 1
    fi
    echo "✅ Package check passed"
else
    echo "⚠️  twine not found, skipping check"
fi

# Step 6: Show built files
echo
echo "📦 Built files:"
ls -lh dist/

# Step 7: Ask for upload
echo
echo "=========================================="
read -p "Upload to PyPI? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo
    echo "Uploading to PyPI..."
    echo "You'll be prompted for credentials."
    echo "Username: __token__"
    echo "Password: Your PyPI API token"
    echo
    
    twine upload dist/*
    
    if [ $? -eq 0 ]; then
        echo
        echo "✅ Upload successful!"
        echo "📦 Package available at: https://pypi.org/project/sharpaikit/"
        echo
        echo "Test installation:"
        echo "  pip install sharpaikit"
    else
        echo "❌ Upload failed"
        exit 1
    fi
else
    echo
    echo "⏭️  Skipping upload"
    echo "To upload manually, run:"
    echo "  twine upload dist/*"
fi

echo
echo "=========================================="
echo "✅ Done!"
echo "=========================================="

