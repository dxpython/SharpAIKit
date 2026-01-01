# Using Skills with AiAgent

## Overview

`AiAgent` now supports the Skill system for behavior constraints and governance. Skills can be optionally integrated through dependency injection, maintaining full backward compatibility.

## Basic Usage

### Without Skills (Backward Compatible)

```csharp
using SharpAIKit.Agent;
using SharpAIKit.LLM;

var client = LLMClientFactory.CreateDeepSeek("YOUR-API-KEY");
var agent = new AiAgent(client);

// Add tools as before
agent.AddTool(new CalculatorTool());
agent.AddTool(new FileWriterTool());

// Works exactly as before - no Skills needed
var result = await agent.RunAsync("Calculate 2 + 2");
```

### With Skills (New Feature)

```csharp
using SharpAIKit.Agent;
using SharpAIKit.LLM;
using SharpAIKit.Skill;
using SharpAIKit.Skill.Examples;

// Create Skill resolver
var skillResolver = new DefaultSkillResolver();
skillResolver.RegisterSkill(new SecurityPolicySkill());
skillResolver.RegisterSkill(new CodeReviewSkill());

// Create agent with Skill resolver
var client = LLMClientFactory.CreateDeepSeek("YOUR-API-KEY");
var agent = new AiAgent(client, skillResolver: skillResolver);

// Add tools
agent.AddTool(new CalculatorTool());
agent.AddTool(new FileWriterTool());

// Skills will automatically:
// 1. Activate based on task and context
// 2. Filter available tools
// 3. Apply constraints
// 4. Log decisions for audit

var result = await agent.RunAsync("Review this code for security issues");

// Access Skill resolution result
if (agent.LastSkillResolution != null)
{
    Console.WriteLine($"Activated Skills: {string.Join(", ", agent.LastSkillResolution.ActivatedSkillIds)}");
    Console.WriteLine($"Decision Reasons:\n{string.Join("\n", agent.LastSkillResolution.DecisionReasons)}");
}
```

## Example: Security Policy Skill

```csharp
// Define a security policy Skill
public class SecurityPolicySkill : ISkill
{
    public SkillMetadata Metadata => new()
    {
        Id = "security_policy",
        Name = "Security Policy",
        Description = "Enforces security policies",
        Priority = 100
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        // Activate for all tasks (or add custom logic)
        return true;
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            // Forbid dangerous tools
            ForbiddenTools = new HashSet<string>
            {
                "file_deleter",
                "system_command",
                "database_writer"
            },
            // Limit execution time
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            // Custom validation
            CustomValidator = (toolName, args, ctx) =>
            {
                // Check for sensitive data in arguments
                var argsStr = string.Join(" ", args.Values);
                return !argsStr.Contains("password", StringComparison.OrdinalIgnoreCase);
            }
        };
    }
}

// Use it
var skillResolver = new DefaultSkillResolver();
skillResolver.RegisterSkill(new SecurityPolicySkill());

var agent = new AiAgent(client, skillResolver: skillResolver);
agent.AddTool(new FileWriterTool());
agent.AddTool(new CalculatorTool());

// File operations will be constrained by SecurityPolicySkill
var result = await agent.RunAsync("Write a file");
```

## Key Features

1. **Optional Integration**: Skills are optional - `AiAgent` works without them
2. **Automatic Activation**: Skills activate based on `ShouldActivate()` logic
3. **Tool Filtering**: Tools are automatically filtered based on Skill constraints
4. **Constraint Validation**: Tool execution is validated before running
5. **Audit Trail**: `LastSkillResolution` provides complete decision history
6. **Backward Compatible**: Existing code continues to work without changes

## Migration Guide

### Before (No Skills)
```csharp
var agent = new AiAgent(client);
agent.AddTool(new CalculatorTool());
```

### After (With Skills - Optional)
```csharp
var skillResolver = new DefaultSkillResolver();
skillResolver.RegisterSkill(new MySkill());

var agent = new AiAgent(client, skillResolver: skillResolver);
agent.AddTool(new CalculatorTool());
```

No breaking changes - Skills are completely optional!

