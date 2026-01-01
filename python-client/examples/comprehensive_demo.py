#!/usr/bin/env python3
"""
Comprehensive demo showing all SharpAIKit features available through Python SDK

This example demonstrates:
1. Basic Agent execution
2. Skill system integration
3. Tool execution
4. Streaming execution
5. Skill resolution and constraints
6. Error handling
7. Context passing
"""

import sys
import os
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from sharpaikit import Agent
from sharpaikit.errors import (
    SharpAIKitError,
    ConnectionError,
    ExecutionError,
    AgentNotFoundError,
)


def print_section(title: str):
    """Print a section header"""
    print("\n" + "=" * 70)
    print(f"  {title}")
    print("=" * 70 + "\n")


def demo_basic_agent():
    """Demo 1: Basic Agent execution"""
    print_section("Demo 1: Basic Agent Execution")
    
    # Configuration
    API_KEY = "YOUR_API_KEY"
    BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
    MODEL = "qwen-plus"
    
    try:
        # Create agent
        agent = Agent(
            api_key=API_KEY,
            base_url=BASE_URL,
            model=MODEL,
            auto_start_host=True
        )
        
        # Execute a simple task
        result = agent.run("你好，请用一句话介绍你自己")
        
        print(f"✅ 执行成功: {result.success}")
        print(f"📝 输出: {result.output}")
        print(f"📊 执行步骤数: {len(result.steps)}")
        
        # Show execution steps
        if result.steps:
            print("\n执行步骤:")
            for i, step in enumerate(result.steps, 1):
                print(f"  {i}. [{step.type}] {step.action}")
                if step.observation:
                    print(f"     观察: {step.observation[:100]}...")
        
        agent.close()
        
    except Exception as e:
        print(f"❌ 错误: {e}")
        return False
    
    return True


def demo_skill_system():
    """Demo 2: Skill system integration"""
    print_section("Demo 2: Skill System Integration")
    
    API_KEY = "sk-502f0625194247d4adc2a9c7659c0ffe"
    BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
    MODEL = "qwen-plus"
    
    try:
        # Create agent with skills
        agent = Agent(
            api_key=API_KEY,
            base_url=BASE_URL,
            model=MODEL,
            skills=["code-review", "security-policy"],  # Skill IDs
            auto_start_host=True
        )
        
        # List available skills
        print("📋 可用 Skills:")
        skills = agent.list_available_skills()
        for skill in skills:
            print(f"  - {skill.id}: {skill.name} (优先级: {skill.priority})")
        
        # Execute task
        result = agent.run("Review this code for security issues: print(user_input)")
        
        print(f"\n✅ 执行成功: {result.success}")
        print(f"📝 输出: {result.output}")
        
        # Show skill resolution
        if result.skill_resolution:
            print("\n🎯 Skill 解析信息:")
            print(f"  激活的 Skills: {', '.join(result.skill_resolution.activated_skill_ids)}")
            print(f"  决策原因: {', '.join(result.skill_resolution.decision_reasons)}")
            
            if result.skill_resolution.constraints:
                constraints = result.skill_resolution.constraints
                if constraints.allowed_tools:
                    print(f"  允许的工具: {', '.join(constraints.allowed_tools)}")
                if constraints.forbidden_tools:
                    print(f"  禁止的工具: {', '.join(constraints.forbidden_tools)}")
        
        # Show denied tools
        if result.denied_tools:
            print(f"\n🚫 被拒绝的工具: {', '.join(result.denied_tools)}")
            if result.skill_resolution:
                for tool in result.denied_tools:
                    reason = result.skill_resolution.tool_denial_reasons.get(tool)
                    print(f"  {tool}: {reason}")
        
        agent.close()
        
    except Exception as e:
        print(f"❌ 错误: {e}")
        return False
    
    return True


def demo_streaming():
    """Demo 3: Streaming execution"""
    print_section("Demo 3: Streaming Execution")
    
    API_KEY = "sk-502f0625194247d4adc2a9c7659c0ffe"
    BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
    MODEL = "qwen-plus"
    
    try:
        agent = Agent(
            api_key=API_KEY,
            base_url=BASE_URL,
            model=MODEL,
            auto_start_host=True
        )
        
        print("📡 流式输出:")
        print("-" * 70)
        
        full_output = ""
        step_count = 0
        
        for chunk in agent.run_stream("请写一首关于春天的短诗"):
            if chunk.output:
                print(chunk.output, end="", flush=True)
                full_output += chunk.output
            
            if chunk.steps:
                for step in chunk.steps:
                    step_count += 1
                    print(f"\n[步骤 {step.step_number}] {step.action}")
        
        print("\n" + "-" * 70)
        print(f"\n✅ 完成，共 {step_count} 个步骤")
        print(f"📝 完整输出长度: {len(full_output)} 字符")
        
        agent.close()
        
    except Exception as e:
        print(f"❌ 错误: {e}")
        return False
    
    return True


def demo_context_passing():
    """Demo 4: Context passing"""
    print_section("Demo 4: Context Passing")
    
    API_KEY = "sk-502f0625194247d4adc2a9c7659c0ffe"
    BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
    MODEL = "qwen-plus"
    
    try:
        agent = Agent(
            api_key=API_KEY,
            base_url=BASE_URL,
            model=MODEL,
            auto_start_host=True
        )
        
        # Pass context to agent
        context = {
            "user_name": "张三",
            "language": "中文",
            "task_type": "翻译"
        }
        
        result = agent.run(
            "请将以下英文翻译成中文: Hello, how are you?",
            context=context
        )
        
        print(f"✅ 执行成功: {result.success}")
        print(f"📝 输出: {result.output}")
        print(f"\n📋 使用的上下文:")
        for key, value in context.items():
            print(f"  {key}: {value}")
        
        agent.close()
        
    except Exception as e:
        print(f"❌ 错误: {e}")
        return False
    
    return True


def demo_error_handling():
    """Demo 5: Error handling"""
    print_section("Demo 5: Error Handling")
    
    API_KEY = "sk-502f0625194247d4adc2a9c7659c0ffe"
    BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
    MODEL = "qwen-plus"
    
    try:
        agent = Agent(
            api_key=API_KEY,
            base_url=BASE_URL,
            model=MODEL,
            auto_start_host=True
        )
        
        # This should work
        try:
            result = agent.run("Hello")
            print(f"✅ 正常执行成功: {result.success}")
        except ExecutionError as e:
            print(f"⚠️  执行错误: {e}")
            if hasattr(e, 'denied_tools') and e.denied_tools:
                print(f"   被拒绝的工具: {e.denied_tools}")
        
        # Test with invalid agent ID (should fail)
        try:
            invalid_agent = Agent(
                api_key=API_KEY,
                base_url=BASE_URL,
                model=MODEL,
                agent_id="invalid-agent-id",
                auto_start_host=False  # Don't auto-start
            )
            # This will fail because agent doesn't exist
            result = invalid_agent.run("Test")
        except (AgentNotFoundError, ConnectionError) as e:
            print(f"✅ 正确捕获错误: {type(e).__name__}: {e}")
        
        agent.close()
        
    except Exception as e:
        print(f"❌ 未预期的错误: {e}")
        return False
    
    return True


def demo_skill_resolution():
    """Demo 6: Skill resolution details"""
    print_section("Demo 6: Skill Resolution Details")
    
    API_KEY = "sk-502f0625194247d4adc2a9c7659c0ffe"
    BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
    MODEL = "qwen-plus"
    
    try:
        agent = Agent(
            api_key=API_KEY,
            base_url=BASE_URL,
            model=MODEL,
            auto_start_host=True
        )
        
        # Execute task
        result = agent.run("Write a simple Python function to add two numbers")
        
        # Get skill resolution
        skill_resolution = agent.get_skill_resolution()
        
        if skill_resolution and skill_resolution.skill_resolution:
            sr = skill_resolution.skill_resolution
            print("🎯 Skill 解析详情:")
            print(f"  激活的 Skills: {sr.activated_skill_ids}")
            print(f"  决策原因: {sr.decision_reasons}")
            
            if sr.constraints:
                print(f"\n📋 约束信息:")
                print(f"  最大步骤数: {sr.constraints.max_steps}")
                print(f"  最大执行时间: {sr.constraints.max_execution_time_ms}ms")
                if sr.constraints.allowed_tools:
                    print(f"  允许的工具: {sr.constraints.allowed_tools}")
                if sr.constraints.forbidden_tools:
                    print(f"  禁止的工具: {sr.constraints.forbidden_tools}")
        
        agent.close()
        
    except Exception as e:
        print(f"❌ 错误: {e}")
        return False
    
    return True


def main():
    """Run all demos"""
    print("\n" + "=" * 70)
    print("  SharpAIKit Python SDK - 完整功能演示")
    print("=" * 70)
    print("\n本示例展示 Python SDK 支持的所有功能:")
    print("  1. 基本 Agent 执行")
    print("  2. Skill 系统集成")
    print("  3. 流式执行")
    print("  4. 上下文传递")
    print("  5. 错误处理")
    print("  6. Skill 解析详情")
    print("\n注意: 需要先构建 C# gRPC 主机")
    print("  cd ../src/SharpAIKit.Grpc.Host && dotnet build -c Release")
    print("\n" + "-" * 70)
    
    demos = [
        ("基本 Agent 执行", demo_basic_agent),
        ("Skill 系统集成", demo_skill_system),
        ("流式执行", demo_streaming),
        ("上下文传递", demo_context_passing),
        ("错误处理", demo_error_handling),
        ("Skill 解析详情", demo_skill_resolution),
    ]
    
    results = []
    for name, demo_func in demos:
        try:
            success = demo_func()
            results.append((name, success))
        except KeyboardInterrupt:
            print("\n\n⚠️  用户中断")
            break
        except Exception as e:
            print(f"\n❌ 演示 '{name}' 失败: {e}")
            results.append((name, False))
    
    # Summary
    print_section("演示总结")
    print("结果:")
    for name, success in results:
        status = "✅ 通过" if success else "❌ 失败"
        print(f"  {status}: {name}")
    
    total = len(results)
    passed = sum(1 for _, success in results if success)
    print(f"\n总计: {passed}/{total} 个演示通过")
    
    return passed == total


if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)

