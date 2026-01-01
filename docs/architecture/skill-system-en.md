# SharpAIKit Skill System Architecture

## 📋 Overview

The Skill system is a **behavior constraint and governance mechanism** in the SharpAIKit framework, designed to decouple Agent behavior specifications from Prompts, providing discoverable, activatable, and constrainable behavior modules. This system targets enterprise/platform-level Agent governance scenarios, not user interaction layers.

### Core Positioning

- **Skills are behavior constraints, not execution entities**: Skills do not directly execute tasks, but only influence "how" and "what" the Agent is allowed to execute
- **Skills are first-class architectural abstractions**: Not helpers, configs, or prompts, but core framework components
- **Minimal viable changes (MVP)**: ≤ 3 new interfaces, no rewriting of Agent Core, no DSL introduction

---

## 1️⃣ Core Abstraction Design

### 1.1 ISkill Interface

```csharp
namespace SharpAIKit.Skill;

/// <summary>
/// Skill metadata: describes the identity and basic properties of a Skill.
/// </summary>
public sealed class SkillMetadata
{
    /// <summary>Unique identifier for the Skill (immutable)</summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>Skill name (for display and logging)</summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>Skill description</summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>Skill version (semantic versioning, e.g., "1.0.0")</summary>
    public string Version { get; init; } = "1.0.0";
    
    /// <summary>Skill scope (e.g., "code_review", "security", "compliance")</summary>
    public string Scope { get; init; } = string.Empty;
    
    /// <summary>
    /// Skill priority (higher number = higher priority)
    /// Used for conflict resolution during constraint merging: higher priority Skills' constraints are applied first
    /// </summary>
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Skill constraints definition: expresses restrictions on Agent behavior imposed by a Skill
/// </summary>
public sealed class SkillConstraints
{
    /// <summary>Whitelist of allowed tools (null means no restriction)</summary>
    public IReadOnlySet<string>? AllowedTools { get; init; }
    
    /// <summary>Blacklist of forbidden tools</summary>
    public IReadOnlySet<string> ForbiddenTools { get; init; } = new HashSet<string>();
    
    /// <summary>Maximum execution steps (null means no restriction)</summary>
    public int? MaxSteps { get; init; }
    
    /// <summary>Maximum execution time (null means no restriction)</summary>
    public TimeSpan? MaxExecutionTime { get; init; }
    
    /// <summary>Context modification rules (key-value pairs injected into Agent context)</summary>
    public IReadOnlyDictionary<string, object?> ContextModifications { get; init; } = new Dictionary<string, object?>();
    
    /// <summary>Custom constraint validation function (called before tool execution)</summary>
    public Func<string, Dictionary<string, object?>, StrongContext, bool>? CustomValidator { get; init; }
}

/// <summary>
/// Skill interface: defines behavior constraints and activation conditions
/// </summary>
public interface ISkill
{
    /// <summary>Gets the Skill metadata</summary>
    SkillMetadata Metadata { get; }
    
    /// <summary>Checks if the Skill should be activated</summary>
    bool ShouldActivate(string task, StrongContext context);
    
    /// <summary>Gets the Skill's constraints</summary>
    SkillConstraints GetConstraints(StrongContext context);
    
    /// <summary>Applies constraints during planning phase (optional)</summary>
    string? ApplyToPlanning(string planningPrompt, StrongContext context) => null;
}
```

### 1.2 ISkillResolver Interface

```csharp
namespace SharpAIKit.Skill;

/// <summary>
/// Skill resolver: responsible for discovering, activating, and merging Skill constraints
/// </summary>
public interface ISkillResolver
{
    /// <summary>Registers a Skill</summary>
    void RegisterSkill(ISkill skill);
    
    /// <summary>
    /// Resolves Skills based on task and context, returns complete resolution result
    /// </summary>
    SkillResolutionResult Resolve(string task, StrongContext context);
    
    /// <summary>Gets all registered Skills (metadata only)</summary>
    IReadOnlyList<SkillMetadata> GetAllSkills();
}
```

### 1.3 Default Implementation

```csharp
namespace SharpAIKit.Skill;

/// <summary>
/// Default Skill resolver implementation with deterministic constraint merging
/// </summary>
public sealed class DefaultSkillResolver : ISkillResolver
{
    // Implementation details...
}
```

---

## 2️⃣ Runtime Participation

### 2.1 Execution Flow

```
User Intent (task description)
    ↓
[Skill Discovery]  ← Scan all registered Skills (metadata only)
    ↓
[Skill Resolution] ← Activate matching Skills based on task and context
    ↓
[Constraint Merging] ← Merge constraints from all activated Skills
    ↓
[Context Enhancement] ← Inject context modifications into Agent context
    ↓
[Planning Phase] ← IPlanner.PlanAsync (apply tool whitelist/blacklist)
    ↓
[Execution Phase] ← IToolExecutor.ExecuteAsync (validate tool calls)
    ↓
[Agent Execute] ← Existing Core execution (constrained)
```

### 2.2 Where Skills Take Effect

| Phase | Skill Effect | Implementation Location |
|------|-------------|------------------------|
| **Skill Discovery** | Scan metadata, no logic execution | `ISkillResolver.GetAllSkills()` |
| **Skill Resolution** | Match activation based on intent/context | `ISkillResolver.Resolve()` |
| **Constraint Merging** | Merge constraints from multiple Skills | `ISkillResolver.Resolve()` |
| **Planning Phase** | Filter available tools, modify planning prompts | `EnhancedAgent.RunAsync()` → `IPlanner.PlanAsync()` |
| **Tool Execution** | Validate tool calls against constraints | `IToolExecutor.ExecuteAsync()` |
| **Context Enhancement** | Inject context modifications | `EnhancedAgent.RunAsync()` applies `ContextModifications` |

### 2.3 Where Skills Do NOT Take Effect

- ❌ **Inside LLM calls**: Skills do not directly modify LLM's raw responses
- ❌ **Inside Tool execution logic**: Skills do not change Tool implementations, only control whether calls are allowed
- ❌ **Memory storage**: Skills do not interfere with Memory read/write logic
- ❌ **RAG retrieval**: Skills do not change RAG retrieval behavior

---

## 3️⃣ Integration with Existing Core

### 3.1 Modules Requiring Modification

#### 3.1.1 EnhancedAgent (Minor Changes)

**Modification Points**:
- Insert Skill Discovery/Resolution logic at the start of `RunAsync` method
- Apply tool constraints before planning
- Validate constraints before tool execution

#### 3.1.2 DefaultToolExecutor (Minor Changes)

**Modification Points**:
- Add constraint validation in `ExecuteAsync`

### 3.2 Modules NOT Requiring Modification

The following modules **require NO modification**, maintaining backward compatibility:

- ✅ **AiAgent**: Base Agent remains unchanged
- ✅ **ILLMClient** and implementations: LLM call layer unchanged
- ✅ **IMemory** and implementations: Memory layer unchanged
- ✅ **IPlanner** interface: Planner interface unchanged (implementations can optionally read constraints)
- ✅ **ToolBase** and subclasses: Tool implementations unchanged
- ✅ **StrongContext**: Context object unchanged
- ✅ **RAG Engine**: RAG module unchanged
- ✅ **Graph/Chain modules**: Graph orchestration modules unchanged

---

## 4️⃣ Example Skill Implementations

### 4.1 CodeReviewSkill

```csharp
namespace SharpAIKit.Skill.Examples;

/// <summary>
/// Code review Skill: restricts tools to code analysis only
/// </summary>
public class CodeReviewSkill : ISkill
{
    public SkillMetadata Metadata => new()
    {
        Id = "code_review",
        Name = "Code Review Skill",
        Description = "Enforces code review best practices and restricts tools to code analysis only",
        Scope = "code_review",
        Priority = 10
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        var keywords = new[] { "review", "code review", "analyze code", "inspect", "audit code" };
        var lowerTask = task.ToLowerInvariant();
        return keywords.Any(k => lowerTask.Contains(k));
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            AllowedTools = new HashSet<string>
            {
                "code_analyzer", "syntax_checker", "linter", "security_scanner"
            },
            ForbiddenTools = new HashSet<string> { "file_writer", "code_modifier" },
            MaxSteps = 5,
            ContextModifications = new Dictionary<string, object?>
            {
                ["review_mode"] = true,
                ["focus_areas"] = new[] { "security", "performance", "maintainability" }
            }
        };
    }
}
```

### 4.2 SecurityPolicySkill

```csharp
namespace SharpAIKit.Skill.Examples;

/// <summary>
/// Security policy Skill: forbids high-risk tools
/// </summary>
public class SecurityPolicySkill : ISkill
{
    public SkillMetadata Metadata => new()
    {
        Id = "security_policy",
        Name = "Security Policy Skill",
        Description = "Enforces security policies by blocking high-risk tool usage",
        Scope = "security",
        Priority = 100 // High priority
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        var userRole = context.Get<string>("user_role");
        return userRole != "admin"; // Force activation for non-admins
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            ForbiddenTools = new HashSet<string>
            {
                "file_deleter", "system_command", "database_writer", "network_request"
            },
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            CustomValidator = (toolName, args, ctx) =>
            {
                var sensitivePatterns = new[] { "password", "token", "secret", "key" };
                var argsStr = string.Join(" ", args.Values.Select(v => v?.ToString() ?? ""));
                return !sensitivePatterns.Any(p => argsStr.Contains(p, StringComparison.OrdinalIgnoreCase));
            }
        };
    }
}
```

---

## 5️⃣ Non-Goals (Clear Boundaries)

This Skill system design **explicitly does NOT do the following**:

### 5.1 No Skill Marketplace
- ❌ No online discovery, download, or rating mechanism for Skills
- ❌ No version management or dependency resolution for Skills
- ✅ Skills are registered via code, controlled by developers/platform administrators

### 5.2 No Markdown Skill Files
- ❌ No support for loading Skills from Markdown/YAML files
- ❌ No Claude Code-style Markdown Skill format
- ✅ Skills must be C# classes implementing `ISkill` interface

### 5.3 No Claude Code UX
- ❌ No visual Skill editor
- ❌ No drag-and-drop configuration interface
- ❌ No real-time preview and testing tools
- ✅ Skills are pure code implementations, targeting developers

### 5.4 No End-User Skill Writing
- ❌ No low-code/no-code Skill writing tools
- ❌ No Skill templates and wizards
- ✅ Skill writing requires C# development skills, targeting platform developers/enterprise IT teams

---

## 6️⃣ Constraint Resolution Semantics

### 6.1 Merging Rules (Deterministic Algorithm)

Skill constraint merging follows **deterministic, predictable** rules, ensuring the same input produces the same output:

#### 6.1.1 Tool Constraint Merging

| Constraint Type | Merging Strategy | Description | Example |
|----------------|------------------|------------|---------|
| **AllowedTools** | **Intersection** | Whitelists from multiple Skills are intersected, most restrictive limit applies | Skill A: {tool1, tool2, tool3}<br>Skill B: {tool2, tool3, tool4}<br>Result: {tool2, tool3} |
| **ForbiddenTools** | **Union** | Blacklists from multiple Skills are unioned, any deny means deny | Skill A: {tool1}<br>Skill B: {tool2}<br>Result: {tool1, tool2} |
| **Conflict Resolution** | **Deny-overrides-Allow** | Blacklist always overrides whitelist, even if tool is in whitelist it will be rejected | AllowedTools: {tool1}<br>ForbiddenTools: {tool1}<br>Result: tool1 is rejected |

#### 6.1.2 Execution Limit Merging

| Constraint Type | Merging Strategy | Description |
|----------------|------------------|-------------|
| **MaxSteps** | **Minimum** | Takes the minimum value from all Skill restrictions, most restrictive limit applies |
| **MaxExecutionTime** | **Minimum** | Takes the minimum value from all Skill restrictions, most restrictive limit applies |

#### 6.1.3 Context Modification Merging

- **Merging Strategy**: Applied from low to high priority (high priority overrides low priority)
- **Implementation**: Skills are sorted by `Priority` descending, then `ContextModifications` are applied sequentially
- **Result**: High priority Skills' context modifications override low priority ones

#### 6.1.4 Custom Validator Merging

- **Merging Strategy**: **AND logic** (all validators must pass)
- **Implementation**: All Skills' `CustomValidator` functions are combined into one validator, all must return `true` for execution to be allowed
- **Failure Behavior**: If any validator returns `false`, tool execution is rejected

### 6.2 Conflict Resolution Strategy

#### 6.2.1 Deny-overrides-Allow

**Rule**: `ForbiddenTools` always override `AllowedTools`, even if a tool appears in both lists.

**Reason**: Security and compliance requirements take precedence over functionality availability. In enterprise scenarios, prohibiting certain operations is more important than allowing them.

**Example**:
```csharp
// Skill A: allows file_writer
AllowedTools: {file_writer, calculator}

// Skill B: forbids file_writer (security policy)
ForbiddenTools: {file_writer}

// Final result: file_writer is rejected (Deny-overrides-Allow)
```

#### 6.2.2 Priority Sorting

**Rule**: Skills are sorted by `Priority` descending (high priority first), constraints are applied in this order.

**Purpose**:
- High priority Skills' `ContextModifications` override low priority ones
- In decision reasons, high priority Skills' decisions are recorded first

### 6.3 Determinism Guarantees

#### 6.3.1 Sort Stability

- Skills are sorted by `Priority` descending
- When priorities are equal, sorted by `Metadata.Id` alphabetically (stable sort)
- Ensures the same Skill set produces the same processing order

#### 6.3.2 Merging Result Determinism

- Same input (task, context, registered Skills) always produces the same `SkillResolutionResult`
- Merging algorithm is a pure function with no side effects
- Decision reason list order and content are reproducible

#### 6.3.3 Test Friendliness

- All merging rules have clear mathematical definitions (intersection, union, minimum)
- Can be verified for correctness through unit tests
- Decision reasons provide complete audit trail

---

## 7️⃣ Observability & Auditability

### 7.1 Skill Decision Observability

#### 7.1.1 SkillResolutionResult

The `SkillResolutionResult` object records the complete Skill decision process:

```csharp
public sealed class SkillResolutionResult
{
    // Activated Skills list (sorted by priority)
    public IReadOnlyList<ISkill> ActivatedSkills { get; }
    
    // Final merged constraints
    public SkillConstraints FinalConstraints { get; }
    
    // Decision reasons list (for explainability and auditing)
    public IReadOnlyList<string> DecisionReasons { get; }
}
```

**Uses**:
- **Logging**: Write `DecisionReasons` to logs for troubleshooting
- **Test assertions**: Verify Skill activation and constraint application meet expectations
- **Audit trail**: Record which Skills affected Agent behavior for compliance auditing

#### 7.1.2 Decision Reason Format

Each decision reason describes a decision point, format examples:

```
"Skill 'code_review' (Code Review Skill) activated: task matches activation conditions"
"Skill 'code_review' sets allowed tools: code_analyzer, syntax_checker, linter"
"Skill 'security_policy' adds forbidden tools: file_deleter, system_command"
"Conflict resolution: Deny-overrides-Allow. Tools file_writer are in both allowed and forbidden lists, they will be forbidden."
"Final constraints: AllowedTools=3 (null means no restriction), ForbiddenTools=2, MaxSteps=5, MaxExecutionTime=00:05:00, ContextModifications=2, CustomValidators=1"
```

### 7.2 EnhancedAgent Integration

#### 7.2.1 Logging

`EnhancedAgent` automatically logs after Skill resolution (if `ILogger<EnhancedAgent>` is provided):

```csharp
_logger?.LogInformation(
    "Skill resolution completed. Activated {Count} skill(s): {SkillIds}. " +
    "Decision reasons: {Reasons}",
    skillResolution.ActivatedSkills.Count,
    string.Join(", ", skillResolution.ActivatedSkillIds),
    string.Join("; ", skillResolution.DecisionReasons));
```

#### 7.2.2 Tool Filtering Logging

When Skills filter available tools, detailed information is logged:

```csharp
_logger?.LogInformation(
    "Tool filtering applied by Skills. Allowed: {AllowedCount}/{TotalCount}. " +
    "Removed tools: {RemovedTools}",
    finalTools.Count, allTools.Count, string.Join(", ", removed));
```

#### 7.2.3 Accessing Resolution Results

Access the last Skill resolution result via `EnhancedAgent.LastSkillResolution` property:

```csharp
var result = await agent.RunAsync("Review code");
var skillResolution = agent.LastSkillResolution;

if (skillResolution != null)
{
    Console.WriteLine($"Activated skills: {string.Join(", ", skillResolution.ActivatedSkillIds)}");
    Console.WriteLine($"Decision reasons:\n{string.Join("\n", skillResolution.DecisionReasons)}");
}
```

### 7.3 Tool Execution Denial Reasons

When tool execution is rejected by Skill constraints, `ToolExecutionResult` contains detailed denial reasons:

```csharp
// In DefaultToolExecutor.ExecuteAsync
result.Error = $"Tool '{toolName}' is forbidden by active skill constraints";
result.Metadata["skill_constraint_violation"] = true;
result.Metadata["denial_reason"] = reason; // From SkillResolutionResult.GetToolDenialReason()
```

**Denial reason formats**:
```
"Tool 'file_writer' is forbidden by Skill(s): security_policy, code_review"
"Tool 'file_writer' is not in the allowed tools list"
"Tool 'file_writer' failed custom validation by active skill constraints"
```

### 7.4 Enterprise Audit Value

#### 7.4.1 Compliance Auditing

- **Recording**: Which Skills activated when, affecting which tool calls
- **Tracing**: Reasons for tool execution denials, which Skill caused them
- **Proof**: Enterprise policies (security, compliance) were correctly applied

#### 7.4.2 Troubleshooting

- **Diagnosis**: When Agent behavior doesn't meet expectations, quickly locate causes via `DecisionReasons`
- **Debugging**: Verify Skill activation conditions are correct, constraint merging meets expectations
- **Optimization**: Analyze Skill conflicts, optimize Skill priorities and constraint definitions

#### 7.4.3 Explainability

- **Transparency**: Every Agent decision can be traced back to specific Skills and constraints
- **Trust**: Enterprise users can understand why certain operations were prohibited or restricted
- **Accountability**: Clear which Skills are responsible for Agent behavior, facilitating responsibility assignment

### 7.5 Best Practices

#### 7.5.1 Log Levels

- **Information**: Skill resolution completed, tool filtering applied
- **Warning**: Skill conflicts, constraint conflicts
- **Error**: Skill registration failures, constraint validation failures

#### 7.5.2 Log Content

- **Must include**: Activated Skill IDs, final constraint summary, tool filtering results
- **Recommended**: Complete decision reason list (for auditing)
- **Optional**: Skill metadata (name, version, scope)

#### 7.5.3 Performance Considerations

- Skill resolution is lightweight, logging should not affect performance
- Decision reason lists are in memory, only serialized when needed (e.g., writing to logs)
- Production environments can selectively log detailed decision reasons (via configuration)

---

## 8️⃣ Summary

The SharpAIKit Skill system is a **lightweight, type-safe, extensible** behavior constraint mechanism that achieves enterprise-level Agent governance capabilities through minimal changes. The system design follows the principle of "constraints, not execution", ensuring Skills do not compromise the stability and maintainability of the existing architecture.

**Core Value**:
- ✅ Decouples behavior specifications from Prompts
- ✅ Provides discoverable, activatable, and constrainable behavior modules
- ✅ Targets enterprise/platform-level governance, not user interaction
- ✅ Minimal viable changes, maintains backward compatibility

