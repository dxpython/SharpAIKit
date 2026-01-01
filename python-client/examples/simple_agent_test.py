#!/usr/bin/env python3
"""
Simple Agent test - Quick demo of SharpAIKit Python SDK
"""

import sys
import os
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sharpaikit import Agent
from sharpaikit.errors import SharpAIKitError


def main():
    print("=" * 60)
    print("SharpAIKit - Simple Agent Test")
    print("=" * 60)
    print()
    
    # Get API key from environment or use placeholder
    api_key = os.getenv("OPENAI_API_KEY") or os.getenv("DEEPSEEK_API_KEY")
    if not api_key:
        print("⚠️  Please set OPENAI_API_KEY or DEEPSEEK_API_KEY environment variable")
        print("   Example: export OPENAI_API_KEY='your-key-here'")
        return
    
    base_url = os.getenv("LLM_BASE_URL", "https://api.openai.com/v1")
    model = os.getenv("LLM_MODEL", "gpt-3.5-turbo")
    
    try:
        print("🤖 Creating Agent...")
        agent = Agent(
            api_key=api_key,
            base_url=base_url,
            model=model,
            auto_start_host=True
        )
        print(f"✅ Agent created: {agent.agent_id}")
        print()
        
        # Simple task
        print("📝 Task: What is 15 * 23?")
        print("⏳ Executing...")
        print()
        
        result = agent.run(
            task="What is 15 * 23? Please calculate it step by step.",
            tools=None
        )
        
        print("=" * 60)
        print("📊 Result:")
        print("=" * 60)
        print(f"Success: {result.success}")
        print(f"\nOutput:\n{result.output}")
        
        if result.steps:
            print(f"\n📝 Steps ({len(result.steps)}):")
            for i, step in enumerate(result.steps, 1):
                print(f"\n  Step {i}:")
                if step.thought:
                    print(f"    💭 {step.thought}")
                if step.action:
                    print(f"    ⚡ {step.action}")
                if step.observation:
                    print(f"    👁️  {step.observation}")
        
        print()
        print("=" * 60)
        print("✅ Test completed successfully!")
        print("=" * 60)
        
    except SharpAIKitError as e:
        print(f"\n❌ SharpAIKit Error: {e}")
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        try:
            agent.close()
        except:
            pass


if __name__ == "__main__":
    main()

