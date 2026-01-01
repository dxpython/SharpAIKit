#!/usr/bin/env python3
"""
Complete Agent demo using SharpAIKit Python SDK.

This example demonstrates:
1. Creating an agent with tools
2. Running agent tasks
3. Streaming execution
4. Skill system integration
"""

import sys
import os
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sharpaikit import Agent
from sharpaikit.errors import SharpAIKitError


def main():
    """Main demo function"""
    print("=" * 60)
    print("SharpAIKit Python SDK - Agent Demo")
    print("=" * 60)
    print()
    
    # Get API configuration from environment
    api_key = os.getenv("OPENAI_API_KEY") or os.getenv("DEEPSEEK_API_KEY") or os.getenv("QWEN_API_KEY")
    if not api_key:
        print("⚠️  Warning: No API key found in environment variables")
        print("   Please set OPENAI_API_KEY, DEEPSEEK_API_KEY, or QWEN_API_KEY")
        print("   Or edit this script to set your API key directly")
        # Default to Qwen API key for testing (通义千问)
        api_key = "sk-502f0625194247d4adc2a9c7659c0ffe"
        print("   Using default Qwen API key for testing")
    
    # Use Qwen configuration if using Qwen API key
    if api_key == "YOUR-API-KEY" or os.getenv("QWEN_API_KEY"):
        base_url = os.getenv("LLM_BASE_URL", "https://dashscope.aliyuncs.com/compatible-mode/v1")
        model = os.getenv("LLM_MODEL", "qwen-turbo")
    else:
        base_url = os.getenv("LLM_BASE_URL", "https://api.openai.com/v1")
        model = os.getenv("LLM_MODEL", "gpt-3.5-turbo")
    
    try:
        # Create agent (this will auto-start the host if needed)
        print("🤖 Creating Agent...")
        print(f"   Model: {model}")
        print(f"   Base URL: {base_url}")
        print()
        
        agent = Agent(
            api_key=api_key,
            base_url=base_url,
            model=model,
            auto_start_host=True  # Automatically start host if not running
        )
        
        print(f"✅ Agent created with ID: {agent.agent_id}")
        print()
        
        # Define some tools for the agent
        tools = [
            {
                "name": "calculator",
                "description": "Performs basic arithmetic operations",
                "parameters": [
                    {
                        "name": "expression",
                        "type": "string",
                        "description": "Mathematical expression to evaluate (e.g., '2 + 2', '10 * 5')",
                        "required": True
                    }
                ]
            },
            {
                "name": "get_weather",
                "description": "Gets the current weather for a location",
                "parameters": [
                    {
                        "name": "location",
                        "type": "string",
                        "description": "City name or location",
                        "required": True
                    }
                ]
            }
        ]
        
        # Example 1: Simple task
        print("=" * 60)
        print("Example 1: Simple Task")
        print("=" * 60)
        task1 = "What is 25 * 37? Use the calculator tool if needed."
        print(f"Task: {task1}")
        print()
        print("Executing...")
        
        try:
            result = agent.run(
                task=task1,
                tools=tools
            )
            
            print()
            print("📊 Result:")
            print(f"  Success: {result.success}")
            print(f"  Output: {result.output}")
            print(f"  Steps: {len(result.steps)}")
            
            if result.steps:
                print()
                print("📝 Execution Steps:")
                for i, step in enumerate(result.steps, 1):
                    print(f"  Step {i}:")
                    if step.thought:
                        print(f"    Thought: {step.thought}")
                    if step.action:
                        print(f"    Action: {step.action}")
                    if step.tool_name:
                        print(f"    Tool: {step.tool_name}")
                        if step.tool_args:
                            print(f"    Args: {step.tool_args}")
                    if step.observation:
                        print(f"    Observation: {step.observation}")
            
            if result.skill_resolution:
                print()
                print("🎯 Skill Resolution:")
                print(f"  Activated Skills: {result.skill_resolution.activated_skill_ids}")
                if result.skill_resolution.decision_reasons:
                    print(f"  Decision Reasons: {result.skill_resolution.decision_reasons}")
        except SharpAIKitError as e:
            print(f"❌ Error: {e}")
        print()
        
        # Example 2: Complex task with streaming
        print("=" * 60)
        print("Example 2: Complex Task with Streaming")
        print("=" * 60)
        task2 = "Plan a weekend trip to Paris. Include: 1) Best time to visit, 2) Top 3 attractions, 3) Budget estimate for 2 days."
        print(f"Task: {task2}")
        print()
        print("Executing with streaming...")
        print()
        
        try:
            print("📡 Streaming output:")
            print("-" * 60)
            for chunk in agent.run_stream(
                task=task2,
                tools=tools
            ):
                if chunk.output:
                    print(chunk.output, end="", flush=True)
                if chunk.steps:
                    for step in chunk.steps:
                        if step.tool_name:
                            print(f"\n[Using tool: {step.tool_name}]", end="", flush=True)
            print()
            print("-" * 60)
            print("✅ Streaming completed")
        except SharpAIKitError as e:
            print(f"❌ Error: {e}")
        print()
        
        # Example 3: Task with context
        print("=" * 60)
        print("Example 3: Task with Context")
        print("=" * 60)
        task3 = "Based on the context, what should I do next?"
        context = {
            "previous_task": "Analyzed user requirements",
            "status": "Requirements gathered, ready for implementation",
            "next_steps": "Need to create implementation plan"
        }
        print(f"Task: {task3}")
        print(f"Context: {context}")
        print()
        print("Executing...")
        
        try:
            result = agent.run(
                task=task3,
                tools=tools,
                context=context
            )
            
            print()
            print("📊 Result:")
            print(f"  Output: {result.output}")
        except SharpAIKitError as e:
            print(f"❌ Error: {e}")
        print()
        
        print("=" * 60)
        print("✅ Demo completed successfully!")
        print("=" * 60)
        
    except KeyboardInterrupt:
        print("\n\n⚠️  Demo interrupted by user")
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        # Clean up
        print("\n🧹 Cleaning up...")
        try:
            agent.close()
        except:
            pass
        print("✅ Cleanup completed")


if __name__ == "__main__":
    main()

