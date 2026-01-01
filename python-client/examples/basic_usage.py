#!/usr/bin/env python3
"""
Basic usage example for SharpAIKit Python SDK
"""

import asyncio
from sharpaikit import Agent

# Configuration
API_KEY = "sk-502f0625194247d4adc2a9c7659c0ffe"
BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
MODEL = "qwen-plus"


def example_sync():
    """Example: Synchronous execution"""
    print("=== Synchronous Execution ===")
    
    agent = Agent(
        api_key=API_KEY,
        base_url=BASE_URL,
        model=MODEL,
        auto_start_host=True,
    )
    
    try:
        result = agent.run("你好，请用一句话介绍你自己")
        
        print(f"Output: {result.output}")
        print(f"Success: {result.success}")
        print(f"Steps: {len(result.steps)}")
        
        if result.skill_resolution:
            print(f"Activated Skills: {result.skill_resolution.activated_skill_ids}")
            print(f"Decision Reasons: {result.skill_resolution.decision_reasons}")
        
        if result.denied_tools:
            print(f"Denied Tools: {result.denied_tools}")
    
    finally:
        agent.close()


def example_stream():
    """Example: Streaming execution"""
    print("\n=== Streaming Execution ===")
    
    agent = Agent(
        api_key=API_KEY,
        base_url=BASE_URL,
        model=MODEL,
        auto_start_host=True,
    )
    
    try:
        print("Response: ", end="", flush=True)
        for chunk in agent.run_stream("数数从1到5"):
            if chunk.output:
                print(chunk.output, end="", flush=True)
            if chunk.steps:
                for step in chunk.steps:
                    print(f"\n[Step {step.step_number}] {step.action}")
        print()
    
    finally:
        agent.close()


async def example_async():
    """Example: Asynchronous execution"""
    print("\n=== Asynchronous Execution ===")
    
    agent = Agent(
        api_key=API_KEY,
        base_url=BASE_URL,
        model=MODEL,
        auto_start_host=True,
    )
    
    try:
        result = await agent.run_async("1+1等于几？")
        print(f"Output: {result.output}")
    
    finally:
        agent.close()


def example_skills():
    """Example: Using skills"""
    print("\n=== Using Skills ===")
    
    agent = Agent(
        api_key=API_KEY,
        base_url=BASE_URL,
        model=MODEL,
        skills=[],  # Add skill IDs here when available
        auto_start_host=True,
    )
    
    try:
        # List available skills
        skills = agent.list_available_skills()
        print(f"Available skills: {len(skills)}")
        for skill in skills:
            print(f"  - {skill.id}: {skill.name} ({skill.description})")
        
        result = agent.run("Test task")
        print(f"Output: {result.output}")
        
        # Get skill resolution
        skill_res = agent.get_skill_resolution()
        if skill_res and skill_res.skill_resolution:
            print(f"Activated Skills: {skill_res.skill_resolution.activated_skill_ids}")
    
    finally:
        agent.close()


if __name__ == "__main__":
    # Make sure to build the gRPC host first:
    # cd ../src/SharpAIKit.Grpc.Host && dotnet build
    
    try:
        example_sync()
        example_stream()
        asyncio.run(example_async())
        example_skills()
    except Exception as e:
        print(f"\nError: {e}")
        import traceback
        traceback.print_exc()

