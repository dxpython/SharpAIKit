"""
Setup script for sharpaikit (fallback for pip)
"""

from setuptools import setup, find_packages
from pathlib import Path

# Read README
readme_file = Path(__file__).parent / "README.md"
long_description = readme_file.read_text(encoding="utf-8") if readme_file.exists() else ""

setup(
    name="sharpaikit",
    version="0.3.0",
    description="Official Python SDK for SharpAIKit - .NET AI/LLM Toolkit",
    long_description=long_description,
    long_description_content_type="text/markdown",
    author="SharpAIKit Team",
    url="https://github.com/dxpython/SharpAIKit",
    packages=find_packages(exclude=["examples", "tests"]),
    python_requires=">=3.8",
    install_requires=[
        "grpcio>=1.60.0",
        "grpcio-tools>=1.60.0",
    ],
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
    ],
    keywords=["ai", "llm", "agent", "sharpai", "grpc"],
)

