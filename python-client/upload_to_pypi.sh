#!/bin/bash
# Quick script to upload to PyPI

set -e

echo "=========================================="
echo "Uploading SharpAIKit to PyPI"
echo "=========================================="
echo

# Check if dist files exist
if [ ! -d "dist" ] || [ -z "$(ls -A dist/*.whl dist/*.tar.gz 2>/dev/null)" ]; then
    echo "❌ No distribution files found. Run 'uv build' first."
    exit 1
fi

echo "📦 Distribution files:"
ls -lh dist/
echo

# Check twine
if ! command -v twine &> /dev/null; then
    echo "Installing twine..."
    pip install twine
fi

# Check package
echo "🔍 Checking package..."
twine check dist/*
echo

# Upload
echo "🚀 Uploading to PyPI..."
echo "You'll be prompted for credentials:"
echo "  Username: __token__"
echo "  Password: Your PyPI API token (starts with pypi-)"
echo

read -p "Continue? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled"
    exit 0
fi

twine upload dist/*

echo
echo "✅ Upload complete!"
echo "📦 Package: https://pypi.org/project/sharpaikit/"
