#!/usr/bin/env python3
"""
Generate Python gRPC code from .proto file
"""

import subprocess
import sys
from pathlib import Path

def generate_grpc_code():
    """Generate Python gRPC code"""
    # Get the script directory (python-client)
    script_dir = Path(__file__).parent.absolute()
    # Get project root (parent of python-client)
    project_root = script_dir.parent.absolute()
    
    proto_dir = project_root / "proto"
    output_dir = script_dir / "sharpaikit" / "_grpc"
    
    output_dir.mkdir(parents=True, exist_ok=True)
    
    proto_file = proto_dir / "sharpaikit.proto"
    
    if not proto_file.exists():
        print(f"Error: Proto file not found: {proto_file}")
        print(f"Looking in: {proto_dir}")
        sys.exit(1)
    
    cmd = [
        sys.executable, "-m", "grpc_tools.protoc",
        f"--proto_path={proto_dir}",
        f"--python_out={output_dir}",
        f"--grpc_python_out={output_dir}",
        str(proto_file),
    ]
    
    print(f"Generating gRPC code from {proto_file}...")
    print(f"Output directory: {output_dir}")
    print(f"Command: {' '.join(cmd)}")
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"Error generating gRPC code:")
        print(result.stderr)
        if "grpc_tools" in result.stderr.lower():
            print("\nPlease install grpc-tools first:")
            print("  pip install grpcio-tools")
            print("  or")
            print("  uv pip install grpcio-tools")
        sys.exit(1)
    
    # Verify generated files
    pb2_file = output_dir / "sharpaikit_pb2.py"
    pb2_grpc_file = output_dir / "sharpaikit_pb2_grpc.py"
    
    if not pb2_file.exists() or not pb2_grpc_file.exists():
        print(f"Error: Generated files not found!")
        print(f"Expected: {pb2_file}")
        print(f"Expected: {pb2_grpc_file}")
        sys.exit(1)
    
    print(f"✅ Generated files:")
    print(f"  - {pb2_file}")
    print(f"  - {pb2_grpc_file}")
    
    # Fix imports in sharpaikit_pb2_grpc.py (gRPC tools generate absolute imports)
    pb2_grpc_content = pb2_grpc_file.read_text(encoding="utf-8")
    # Replace absolute imports with relative imports
    pb2_grpc_content = pb2_grpc_content.replace(
        "import sharpaikit_pb2 as sharpaikit__pb2",
        "from . import sharpaikit_pb2 as sharpaikit__pb2"
    )
    pb2_grpc_file.write_text(pb2_grpc_content, encoding="utf-8")
    print(f"✅ Fixed imports in {pb2_grpc_file}")
    
    # Create/update __init__.py
    init_file = output_dir / "__init__.py"
    init_content = '''"""gRPC generated code for SharpAIKit"""

from . import sharpaikit_pb2
from . import sharpaikit_pb2_grpc

__all__ = ["sharpaikit_pb2", "sharpaikit_pb2_grpc"]
'''
    init_file.write_text(init_content)
    print(f"✅ Updated {init_file}")

if __name__ == "__main__":
    generate_grpc_code()

