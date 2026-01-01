# SharpAIKit Python SDK Examples

This directory contains example scripts demonstrating how to use the SharpAIKit Python SDK.

## Prerequisites

1. **.NET SDK**: Make sure you have .NET SDK installed (for building the gRPC host)
2. **Python 3.8+**: Python 3.8 or higher is required
3. **uv**: Python package manager (recommended) or pip
4. **API Key**: You'll need an API key for your LLM provider (OpenAI, DeepSeek, etc.)

## Setup

1. **Build the C# gRPC host**:
   ```bash
   cd ../..
   dotnet build src/SharpAIKit.Grpc.Host/SharpAIKit.Grpc.Host.csproj -c Release
   ```

2. **Generate gRPC Python code**:
   ```bash
   cd python-client
   python3 generate_grpc.py
   ```

3. **Install Python dependencies**:
   ```bash
   # Using uv (recommended)
   uv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   uv pip install -e .

   # Or using pip
   pip install -e .
   ```

4. **Set your API key**:
   ```bash
   export OPENAI_API_KEY="your-api-key-here"
   # Or for DeepSeek
   export DEEPSEEK_API_KEY="your-api-key-here"
   ```

## Running Examples

### Simple Agent Test

Quick test to verify everything is working:

```bash
python3 examples/simple_agent_test.py
```

### Complete Agent Demo

Full demo with multiple examples:

```bash
python3 examples/agent_demo.py
```

Or use the provided script:

```bash
./examples/run_agent_demo.sh
```

## Example Output

When you run the simple test, you should see output like:

```
============================================================
SharpAIKit - Simple Agent Test
============================================================

🤖 Creating Agent...
✅ Agent created: abc123-def456-...

📝 Task: What is 15 * 23?
⏳ Executing...

============================================================
📊 Result:
============================================================
Success: True

Output:
15 * 23 = 345

📝 Steps (2):

  Step 1:
    💭 I need to calculate 15 * 23
    ⚡ I'll multiply 15 by 23
    👁️  Calculating...

  Step 2:
    💭 The result is 345
    ⚡ Providing the answer
    👁️  15 * 23 = 345

============================================================
✅ Test completed successfully!
============================================================
```

## Troubleshooting

### Host not starting

If the gRPC host fails to start:
1. Make sure .NET SDK is installed: `dotnet --version`
2. Make sure the host project is built: `dotnet build src/SharpAIKit.Grpc.Host/SharpAIKit.Grpc.Host.csproj`
3. Check if port 50051 is already in use: `lsof -i :50051` (macOS/Linux) or `netstat -an | findstr 50051` (Windows)

### Connection errors

If you get connection errors:
1. Make sure the host is running: Check the process list
2. Try manually starting the host:
   ```bash
   cd ../..
   dotnet run --project src/SharpAIKit.Grpc.Host/SharpAIKit.Grpc.Host.csproj
   ```

### API key errors

If you get API key errors:
1. Make sure you've set the environment variable: `echo $OPENAI_API_KEY`
2. Or edit the script to set the API key directly (not recommended for production)

## Next Steps

- Explore the full API in `sharpaikit/agent.py`
- Check out other services: Chain, Memory, RAG, Graph, etc.
- Read the main README for more details

