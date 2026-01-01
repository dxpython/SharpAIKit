# SharpAIKit Skill 系统架构设计

## 📋 概述

Skill 系统是 SharpAIKit 框架中的**行为约束与治理机制**，旨在将 Agent 的行为规范从 Prompt 中解耦，提供可发现、可激活、可约束的行为模块。本系统面向企业级/平台级 Agent 治理场景，而非用户交互层。

### 核心定位

- **Skill 是行为约束，不是执行主体**：Skill 不直接执行任务，只影响 Agent 的"如何执行"和"允许执行什么"
- **Skill 是一等架构抽象**：不是 Helper、Config 或 Prompt，而是框架的核心组件
- **最小可行改动（MVP）**：新增接口数量 ≤ 3，不重写 Agent Core，不引入 DSL

---

## 1️⃣ 核心抽象设计

### 1.1 ISkill 接口

```csharp
namespace SharpAIKit.Skill;

/// <summary>
/// Skill 元数据
/// </summary>
public class SkillMetadata
{
    /// <summary>Skill 唯一标识符</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>Skill 名称</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Skill 描述</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>Skill 版本</summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>Skill 作用域（如 "code_review", "security", "compliance"）</summary>
    public string Scope { get; set; } = string.Empty;
    
    /// <summary>Skill 优先级（数字越大优先级越高）</summary>
    public int Priority { get; set; } = 0;
}

/// <summary>
/// Skill 约束定义
/// </summary>
public class SkillConstraints
{
    /// <summary>允许使用的工具白名单（null 表示不限制）</summary>
    public HashSet<string>? AllowedTools { get; set; }
    
    /// <summary>禁止使用的工具黑名单</summary>
    public HashSet<string> ForbiddenTools { get; set; } = new();
    
    /// <summary>最大执行步骤数（null 表示不限制）</summary>
    public int? MaxSteps { get; set; }
    
    /// <summary>最大执行时间（null 表示不限制）</summary>
    public TimeSpan? MaxExecutionTime { get; set; }
    
    /// <summary>上下文修改规则（键值对，将注入到 Agent 上下文）</summary>
    public Dictionary<string, object?> ContextModifications { get; set; } = new();
    
    /// <summary>自定义约束验证函数（在工具执行前调用）</summary>
    public Func<string, Dictionary<string, object?>, StrongContext, bool>? CustomValidator { get; set; }
}

/// <summary>
/// Skill 激活条件
/// </summary>
public class SkillActivation
{
    /// <summary>基于意图关键词的匹配（任一匹配即激活）</summary>
    public HashSet<string> IntentKeywords { get; set; } = new();
    
    /// <summary>基于上下文的匹配函数（返回 true 表示激活）</summary>
    public Func<StrongContext, bool>? ContextMatcher { get; set; }
    
    /// <summary>强制激活（忽略其他条件）</summary>
    public bool ForceActivate { get; set; } = false;
}

/// <summary>
/// Skill 接口：定义行为约束和激活条件
/// </summary>
public interface ISkill
{
    /// <summary>获取 Skill 元数据</summary>
    SkillMetadata Metadata { get; }
    
    /// <summary>检查 Skill 是否应该被激活</summary>
    /// <param name="task">用户任务描述</param>
    /// <param name="context">当前上下文</param>
    /// <returns>是否激活</returns>
    bool ShouldActivate(string task, StrongContext context);
    
    /// <summary>获取 Skill 的约束</summary>
    /// <param name="context">当前上下文</param>
    /// <returns>约束定义</returns>
    SkillConstraints GetConstraints(StrongContext context);
    
    /// <summary>在规划阶段应用约束（可选，用于修改规划提示）</summary>
    /// <param name="planningPrompt">原始规划提示</param>
    /// <param name="context">当前上下文</param>
    /// <returns>修改后的规划提示</returns>
    string? ApplyToPlanning(string planningPrompt, StrongContext context) => null;
}
```

### 1.2 ISkillResolver 接口

```csharp
namespace SharpAIKit.Skill;

/// <summary>
/// Skill 解析器：负责发现和激活 Skill
/// </summary>
public interface ISkillResolver
{
    /// <summary>注册一个 Skill</summary>
    void RegisterSkill(ISkill skill);
    
    /// <summary>根据任务和上下文解析应该激活的 Skill</summary>
    /// <param name="task">用户任务</param>
    /// <param name="context">当前上下文</param>
    /// <returns>激活的 Skill 列表（按优先级排序）</returns>
    IReadOnlyList<ISkill> ResolveSkills(string task, StrongContext context);
    
    /// <summary>获取所有已注册的 Skill（仅元数据）</summary>
    IReadOnlyList<SkillMetadata> GetAllSkills();
    
    /// <summary>合并多个 Skill 的约束</summary>
    /// <param name="skills">要合并的 Skill 列表</param>
    /// <param name="context">当前上下文</param>
    /// <returns>合并后的约束</returns>
    SkillConstraints MergeConstraints(IReadOnlyList<ISkill> skills, StrongContext context);
}
```

### 1.3 默认实现

```csharp
namespace SharpAIKit.Skill;

/// <summary>
/// 默认 Skill 解析器实现
/// </summary>
public class DefaultSkillResolver : ISkillResolver
{
    private readonly List<ISkill> _skills = new();
    private readonly object _lock = new();
    
    public void RegisterSkill(ISkill skill)
    {
        lock (_lock)
        {
            _skills.Add(skill);
        }
    }
    
    public IReadOnlyList<ISkill> ResolveSkills(string task, StrongContext context)
    {
        lock (_lock)
        {
            var activated = _skills
                .Where(s => s.ShouldActivate(task, context))
                .OrderByDescending(s => s.Metadata.Priority)
                .ToList();
            
            return activated;
        }
    }
    
    public IReadOnlyList<SkillMetadata> GetAllSkills()
    {
        lock (_lock)
        {
            return _skills.Select(s => s.Metadata).ToList();
        }
    }
    
    public SkillConstraints MergeConstraints(IReadOnlyList<ISkill> skills, StrongContext context)
    {
        var merged = new SkillConstraints();
        
        foreach (var skill in skills)
        {
            var constraints = skill.GetConstraints(context);
            
            // 合并工具白名单（交集）
            if (constraints.AllowedTools != null)
            {
                if (merged.AllowedTools == null)
                    merged.AllowedTools = new HashSet<string>(constraints.AllowedTools);
                else
                    merged.AllowedTools.IntersectWith(constraints.AllowedTools);
            }
            
            // 合并工具黑名单（并集）
            merged.ForbiddenTools.UnionWith(constraints.ForbiddenTools);
            
            // 取最小 MaxSteps
            if (constraints.MaxSteps.HasValue)
            {
                merged.MaxSteps = merged.MaxSteps.HasValue
                    ? Math.Min(merged.MaxSteps.Value, constraints.MaxSteps.Value)
                    : constraints.MaxSteps.Value;
            }
            
            // 取最小 MaxExecutionTime
            if (constraints.MaxExecutionTime.HasValue)
            {
                merged.MaxExecutionTime = merged.MaxExecutionTime.HasValue
                    ? TimeSpan.FromMilliseconds(Math.Min(
                        merged.MaxExecutionTime.Value.TotalMilliseconds,
                        constraints.MaxExecutionTime.Value.TotalMilliseconds))
                    : constraints.MaxExecutionTime.Value;
            }
            
            // 合并上下文修改（后注册的覆盖先注册的）
            foreach (var kvp in constraints.ContextModifications)
            {
                merged.ContextModifications[kvp.Key] = kvp.Value;
            }
        }
        
        return merged;
    }
}
```

---

## 2️⃣ 运行时参与方式

### 2.1 执行流程

```
User Intent (任务描述)
    ↓
[Skill Discovery]  ← 扫描所有已注册的 Skill（仅读取元数据）
    ↓
[Skill Resolution] ← 根据任务和上下文激活匹配的 Skill
    ↓
[Constraint Merging] ← 合并所有激活 Skill 的约束
    ↓
[Context Enhancement] ← 将约束中的上下文修改注入到 Agent 上下文
    ↓
[Planning Phase] ← IPlanner.PlanAsync（应用工具白名单/黑名单）
    ↓
[Execution Phase] ← IToolExecutor.ExecuteAsync（验证工具调用）
    ↓
[Agent Execute] ← 现有 Core 执行（受约束限制）
```

### 2.2 伪代码实现

```csharp
// 在 EnhancedAgent.RunAsync 中的集成点
public async Task<AgentExecutionResult> RunAsync(string task, CancellationToken cancellationToken = default)
{
    var context = new StrongContext();
    context.Set("task", task);
    
    // ========== Skill 机制插入点 ==========
    // 1. Skill Discovery & Resolution
    var activatedSkills = _skillResolver?.ResolveSkills(task, context) ?? new List<ISkill>();
    
    // 2. Merge Constraints
    var mergedConstraints = _skillResolver?.MergeConstraints(activatedSkills, context) ?? new SkillConstraints();
    
    // 3. Apply Context Modifications
    foreach (var kvp in mergedConstraints.ContextModifications)
    {
        context.Set(kvp.Key, kvp.Value);
    }
    
    // 4. Apply Tool Constraints to Available Tools
    var allTools = _toolExecutor.GetAvailableTools().Select(t => t.Name).ToList();
    var allowedTools = mergedConstraints.AllowedTools != null
        ? allTools.Intersect(mergedConstraints.AllowedTools).ToList()
        : allTools;
    var finalTools = allowedTools.Except(mergedConstraints.ForbiddenTools).ToList();
    
    context.Set("available_tools", finalTools);
    context.Set("skill_constraints", mergedConstraints); // 供后续验证使用
    // ======================================
    
    // 继续原有流程
    var memoryContext = await _memory.GetContextStringAsync(task);
    context.Set("memory", memoryContext);
    
    // Planning with Skill-aware prompt
    var planningPrompt = BuildPlanningPrompt(task, context);
    foreach (var skill in activatedSkills)
    {
        var modified = skill.ApplyToPlanning(planningPrompt, context);
        if (modified != null) planningPrompt = modified;
    }
    
    var plan = await _planner.PlanAsync(task, context, cancellationToken);
    // ... 后续执行
}
```

### 2.3 Skill 生效位置

| 阶段 | Skill 作用 | 实现位置 |
|------|-----------|---------|
| **Skill Discovery** | 扫描元数据，不执行任何逻辑 | `ISkillResolver.GetAllSkills()` |
| **Skill Resolution** | 基于意图/上下文匹配激活 | `ISkillResolver.ResolveSkills()` |
| **Constraint Merging** | 合并多个 Skill 的约束 | `ISkillResolver.MergeConstraints()` |
| **Planning Phase** | 过滤可用工具、修改规划提示 | `EnhancedAgent.RunAsync()` → `IPlanner.PlanAsync()` |
| **Tool Execution** | 验证工具调用是否符合约束 | `IToolExecutor.ExecuteAsync()` 中调用验证器 |
| **Context Enhancement** | 注入上下文修改 | `EnhancedAgent.RunAsync()` 中应用 `ContextModifications` |

### 2.4 Skill 不生效的位置

- ❌ **LLM 调用内部**：Skill 不直接修改 LLM 的原始响应
- ❌ **Tool 执行逻辑内部**：Skill 不改变 Tool 的实现，只控制是否允许调用
- ❌ **Memory 存储**：Skill 不干预 Memory 的读写逻辑
- ❌ **RAG 检索**：Skill 不改变 RAG 的检索行为

---

## 3️⃣ 与现有 Core 的集成点

### 3.1 需要修改的模块

#### 3.1.1 EnhancedAgent（轻微调整）

**修改点**：
- 在 `RunAsync` 方法开始处插入 Skill Discovery/Resolution 逻辑
- 在规划前应用工具约束
- 在工具执行前验证约束

**修改示例**：
```csharp
public class EnhancedAgent
{
    private readonly ISkillResolver? _skillResolver; // 新增可选依赖
    
    public EnhancedAgent(
        LLM.ILLMClient llmClient,
        IPlanner? planner = null,
        IToolExecutor? toolExecutor = null,
        IMemory? memory = null,
        ISkillResolver? skillResolver = null) // 新增参数
    {
        // ... 现有代码
        _skillResolver = skillResolver;
    }
    
    // 在 RunAsync 中插入 Skill 逻辑（见 2.2 节）
}
```

#### 3.1.2 DefaultToolExecutor（轻微调整）

**修改点**：
- 在 `ExecuteAsync` 中增加约束验证

**修改示例**：
```csharp
public async Task<ToolExecutionResult> ExecuteAsync(
    string toolName, 
    Dictionary<string, object?> arguments, 
    StrongContext context, 
    CancellationToken cancellationToken = default)
{
    // 新增：约束验证
    var constraints = context.Get<SkillConstraints>("skill_constraints");
    if (constraints != null)
    {
        // 检查工具黑名单
        if (constraints.ForbiddenTools.Contains(toolName))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Tool '{toolName}' is forbidden by active skill constraints"
            };
        }
        
        // 检查工具白名单
        if (constraints.AllowedTools != null && !constraints.AllowedTools.Contains(toolName))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Tool '{toolName}' is not in the allowed tools list"
            };
        }
        
        // 自定义验证器
        if (constraints.CustomValidator != null && 
            !constraints.CustomValidator(toolName, arguments, context))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Tool '{toolName}' failed custom validation"
            };
        }
    }
    
    // 继续原有执行逻辑
    // ...
}
```

### 3.2 不需要修改的模块

以下模块**完全不需要修改**，保持向后兼容：

- ✅ **AiAgent**：基础 Agent 保持不变
- ✅ **ILLMClient** 及其实现：LLM 调用层不变
- ✅ **IMemory** 及其实现：Memory 层不变
- ✅ **IPlanner** 接口：规划器接口不变（实现可选择性读取约束）
- ✅ **ToolBase** 及其子类：Tool 实现不变
- ✅ **StrongContext**：上下文对象不变
- ✅ **RAG Engine**：RAG 模块不变
- ✅ **Graph/Chain 模块**：图编排模块不变

### 3.3 可选增强点

以下模块可以**选择性增强**以更好地利用 Skill：

- 🔶 **SimplePlanner**：可以读取 `context.Get<SkillConstraints>()` 来调整规划提示
- 🔶 **LLMPlannerBase**：可以在 `PlanningPromptTemplate` 中注入 Skill 相关的指导

---

## 4️⃣ 示例 Skill 实现

### 4.1 CodeReviewSkill

```csharp
namespace SharpAIKit.Skill.Examples;

/// <summary>
/// 代码审查 Skill：限制只能使用代码分析相关工具
/// </summary>
public class CodeReviewSkill : ISkill
{
    public SkillMetadata Metadata => new()
    {
        Id = "code_review",
        Name = "Code Review Skill",
        Description = "Enforces code review best practices and restricts tools to code analysis only",
        Version = "1.0.0",
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
            // 只允许代码分析工具
            AllowedTools = new HashSet<string>
            {
                "code_analyzer",
                "syntax_checker",
                "linter",
                "security_scanner"
            },
            // 禁止文件写入工具
            ForbiddenTools = new HashSet<string> { "file_writer", "code_modifier" },
            // 限制最大步骤数
            MaxSteps = 5,
            // 注入代码审查上下文
            ContextModifications = new Dictionary<string, object?>
            {
                ["review_mode"] = true,
                ["focus_areas"] = new[] { "security", "performance", "maintainability" }
            }
        };
    }
    
    public string? ApplyToPlanning(string planningPrompt, StrongContext context)
    {
        return planningPrompt + "\n\nNote: This is a code review task. Focus on analysis, not modification.";
    }
}
```

### 4.2 SecurityPolicySkill

```csharp
namespace SharpAIKit.Skill.Examples;

/// <summary>
/// 安全策略 Skill：禁止使用高风险工具
/// </summary>
public class SecurityPolicySkill : ISkill
{
    private readonly HashSet<string> _highRiskTools;
    
    public SecurityPolicySkill()
    {
        _highRiskTools = new HashSet<string>
        {
            "file_deleter",
            "system_command",
            "database_writer",
            "network_request"
        };
    }
    
    public SkillMetadata Metadata => new()
    {
        Id = "security_policy",
        Name = "Security Policy Skill",
        Description = "Enforces security policies by blocking high-risk tool usage",
        Version = "1.0.0",
        Scope = "security",
        Priority = 100 // 高优先级
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        // 安全策略始终激活（或基于用户角色/环境变量）
        var userRole = context.Get<string>("user_role");
        return userRole != "admin"; // 非管理员强制激活
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            ForbiddenTools = _highRiskTools,
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            // 自定义验证器：检查工具参数是否包含敏感信息
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

### 4.3 OrgStyleGuideSkill

```csharp
namespace SharpAIKit.Skill.Examples;

/// <summary>
/// 组织风格指南 Skill：注入组织特定的上下文和约束
/// </summary>
public class OrgStyleGuideSkill : ISkill
{
    private readonly string _orgName;
    private readonly Dictionary<string, object?> _styleGuide;
    
    public OrgStyleGuideSkill(string orgName, Dictionary<string, object?> styleGuide)
    {
        _orgName = orgName;
        _styleGuide = styleGuide;
    }
    
    public SkillMetadata Metadata => new()
    {
        Id = $"org_style_{_orgName.ToLowerInvariant()}",
        Name = $"{_orgName} Style Guide",
        Description = $"Applies {_orgName} organizational style guide and conventions",
        Version = "1.0.0",
        Scope = "organization",
        Priority = 5
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        // 基于上下文中的组织标识激活
        var orgId = context.Get<string>("organization_id");
        return orgId == _orgName || context.Get<bool>("apply_style_guide") == true;
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            // 不限制工具，只注入风格指南上下文
            ContextModifications = new Dictionary<string, object?>(_styleGuide)
            {
                ["organization"] = _orgName,
                ["style_guide_applied"] = true
            }
        };
    }
    
    public string? ApplyToPlanning(string planningPrompt, StrongContext context)
    {
        var guide = string.Join("\n", _styleGuide.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));
        return $"{planningPrompt}\n\nOrganizational Style Guide:\n{guide}";
    }
}
```

---

## 5️⃣ 非目标（明确边界）

本次 Skill 系统设计**明确不做以下事情**：

### 5.1 不做 Skill Marketplace
- ❌ 不提供 Skill 的在线发现、下载、评分机制
- ❌ 不提供 Skill 的版本管理和依赖解析
- ✅ Skill 通过代码注册，由开发者/平台管理员控制

### 5.2 不做 Markdown Skill 文件
- ❌ 不支持从 Markdown/YAML 文件加载 Skill
- ❌ 不提供类似 Claude Code 的 Markdown Skill 格式
- ✅ Skill 必须是 C# 类，实现 `ISkill` 接口

### 5.3 不做 Claude Code UX
- ❌ 不提供可视化的 Skill 编辑器
- ❌ 不提供 Skill 的拖拽式配置界面
- ❌ 不提供 Skill 的实时预览和测试工具
- ✅ Skill 是纯代码实现，面向开发者

### 5.4 不让普通用户编写 Skill
- ❌ 不提供低代码/无代码 Skill 编写工具
- ❌ 不提供 Skill 模板和向导
- ✅ Skill 编写需要 C# 开发能力，面向平台开发者/企业 IT 团队

### 5.5 不做 Skill 的运行时动态加载
- ❌ 不支持从文件系统/网络动态加载 Skill 程序集
- ❌ 不提供 Skill 的热更新机制
- ✅ Skill 在编译时注册，运行时只进行匹配和约束应用

### 5.6 不做 Skill 的自动优化
- ❌ 不提供基于历史数据的 Skill 自动调优
- ❌ 不提供 Skill 的 A/B 测试框架
- ✅ Skill 的优化由开发者手动完成

---

## 6️⃣ 使用示例

### 6.1 基本使用

```csharp
using SharpAIKit.Skill;
using SharpAIKit.Skill.Examples;

// 1. 创建 Skill Resolver
var skillResolver = new DefaultSkillResolver();

// 2. 注册 Skill
skillResolver.RegisterSkill(new CodeReviewSkill());
skillResolver.RegisterSkill(new SecurityPolicySkill());
skillResolver.RegisterSkill(new OrgStyleGuideSkill("AcmeCorp", new Dictionary<string, object?>
{
    ["coding_standard"] = "C# 10",
    ["naming_convention"] = "PascalCase"
}));

// 3. 创建 EnhancedAgent 并注入 Skill Resolver
var llmClient = new OpenAIClient(/* ... */);
var agent = new EnhancedAgent(
    llmClient,
    skillResolver: skillResolver // 注入 Skill Resolver
);

// 4. 运行任务（Skill 自动激活和应用）
var result = await agent.RunAsync("Review the security of this code snippet: ...");
```

### 6.2 自定义 Skill

```csharp
public class CustomComplianceSkill : ISkill
{
    public SkillMetadata Metadata => new()
    {
        Id = "gdpr_compliance",
        Name = "GDPR Compliance Skill",
        Description = "Enforces GDPR compliance rules",
        Scope = "compliance",
        Priority = 50
    };
    
    public bool ShouldActivate(string task, StrongContext context)
    {
        // 检查上下文中的区域标识
        var region = context.Get<string>("user_region");
        return region == "EU" || task.ToLowerInvariant().Contains("gdpr");
    }
    
    public SkillConstraints GetConstraints(StrongContext context)
    {
        return new SkillConstraints
        {
            // 禁止访问个人数据的工具
            ForbiddenTools = new HashSet<string> { "user_data_accessor", "pii_extractor" },
            // 注入 GDPR 上下文
            ContextModifications = new Dictionary<string, object?>
            {
                ["gdpr_enforced"] = true,
                ["data_retention_days"] = 30
            },
            // 自定义验证：检查是否包含个人数据
            CustomValidator = (toolName, args, ctx) =>
            {
                var hasPii = args.Values.Any(v => 
                    v?.ToString()?.Contains("@") == true || // Email
                    v?.ToString()?.Length == 11 && v.ToString()?.All(char.IsDigit) == true); // Phone
                return !hasPii;
            }
        };
    }
}
```

---

## 7️⃣ 架构优势

### 7.1 解耦性
- Skill 与 Agent Core 完全解耦，Agent 可以无 Skill 运行
- Skill 之间相互独立，可以任意组合

### 7.2 可扩展性
- 新增 Skill 只需实现 `ISkill` 接口，无需修改 Core
- Skill Resolver 可以替换为自定义实现（如基于配置文件的解析器）

### 7.3 类型安全
- 所有约束和元数据都是强类型，编译时检查
- 利用 C# 的类型系统避免运行时错误

### 7.4 性能
- Skill Discovery 只读取元数据，不执行逻辑
- Skill Resolution 是轻量级匹配，开销极小
- 约束验证在工具执行前进行，失败时快速返回

### 7.5 向后兼容
- 现有代码无需修改即可运行
- Skill 系统是可选的，通过依赖注入启用

---

## 8️⃣ 约束解析语义（Constraint Resolution Semantics）

### 8.1 合并规则（确定性算法）

Skill 约束合并遵循**确定性、可预测**的规则，确保相同输入产生相同输出：

#### 8.1.1 工具约束合并

| 约束类型 | 合并策略 | 说明 | 示例 |
|---------|---------|------|------|
| **AllowedTools** | **交集**（Intersection） | 多个 Skill 的白名单取交集，最严格的限制生效 | Skill A: {tool1, tool2, tool3}<br>Skill B: {tool2, tool3, tool4}<br>结果: {tool2, tool3} |
| **ForbiddenTools** | **并集**（Union） | 多个 Skill 的黑名单取并集，任一禁止即禁止 | Skill A: {tool1}<br>Skill B: {tool2}<br>结果: {tool1, tool2} |
| **冲突解决** | **Deny-overrides-Allow** | 黑名单始终覆盖白名单，即使工具在白名单中也会被拒绝 | AllowedTools: {tool1}<br>ForbiddenTools: {tool1}<br>结果: tool1 被拒绝 |

#### 8.1.2 执行限制合并

| 约束类型 | 合并策略 | 说明 |
|---------|---------|------|
| **MaxSteps** | **最小值**（Minimum） | 取所有 Skill 限制的最小值，最严格的限制生效 |
| **MaxExecutionTime** | **最小值**（Minimum） | 取所有 Skill 限制的最小值，最严格的限制生效 |

#### 8.1.3 上下文修改合并

- **合并策略**：按 Skill 优先级从低到高应用（高优先级覆盖低优先级）
- **实现**：Skill 按 `Priority` 降序排序后，依次应用 `ContextModifications`
- **结果**：高优先级 Skill 的上下文修改会覆盖低优先级的修改

#### 8.1.4 自定义验证器合并

- **合并策略**：**AND 逻辑**（所有验证器必须通过）
- **实现**：将所有 Skill 的 `CustomValidator` 组合为一个验证器，所有验证器返回 `true` 才允许执行
- **失败行为**：任一验证器返回 `false`，工具执行被拒绝

### 8.2 冲突解决策略

#### 8.2.1 Deny-overrides-Allow（拒绝优先）

**规则**：`ForbiddenTools` 始终覆盖 `AllowedTools`，即使工具同时出现在两个列表中。

**原因**：安全性和合规性要求优先于功能可用性。企业场景中，禁止某些操作比允许更重要。

**示例**：
```csharp
// Skill A: 允许 file_writer
AllowedTools: {file_writer, calculator}

// Skill B: 禁止 file_writer（安全策略）
ForbiddenTools: {file_writer}

// 最终结果: file_writer 被拒绝（Deny-overrides-Allow）
```

#### 8.2.2 优先级排序

**规则**：Skill 按 `Priority` 降序排序（高优先级在前），约束按此顺序应用。

**用途**：
- 高优先级 Skill 的 `ContextModifications` 覆盖低优先级
- 在决策原因中，高优先级 Skill 的决策会优先记录

**示例**：
```csharp
// Skill A (Priority=10): MaxSteps=20
// Skill B (Priority=50): MaxSteps=5

// 处理顺序: B → A
// 最终 MaxSteps: 5 (最小值，但 B 的决策会优先记录)
```

### 8.3 确定性保证

#### 8.3.1 排序稳定性

- Skill 按 `Priority` 降序排序
- 相同优先级时，按 `Metadata.Id` 字母序排序（稳定排序）
- 确保相同 Skill 集合产生相同的处理顺序

#### 8.3.2 合并结果确定性

- 相同输入（任务、上下文、已注册 Skill）始终产生相同的 `SkillResolutionResult`
- 合并算法是纯函数，无副作用
- 决策原因列表的顺序和内容可重现

#### 8.3.3 测试友好性

- 所有合并规则都有明确的数学定义（交集、并集、最小值）
- 可以通过单元测试验证合并结果的正确性
- 决策原因提供完整的审计轨迹

---

## 9️⃣ 可观测性与审计（Observability & Auditability）

### 9.1 Skill 决策可观测性

#### 9.1.1 SkillResolutionResult

`SkillResolutionResult` 对象记录了 Skill 决策的完整过程：

```csharp
public sealed class SkillResolutionResult
{
    // 激活的 Skill 列表（按优先级排序）
    public IReadOnlyList<ISkill> ActivatedSkills { get; }
    
    // 合并后的最终约束
    public SkillConstraints FinalConstraints { get; }
    
    // 决策原因列表（用于可解释性和审计）
    public IReadOnlyList<string> DecisionReasons { get; }
}
```

**用途**：
- **日志记录**：将 `DecisionReasons` 写入日志，用于问题排查
- **测试断言**：验证 Skill 激活和约束应用是否符合预期
- **审计追踪**：记录哪些 Skill 影响了 Agent 行为，用于合规审计

#### 9.1.2 决策原因格式

每条决策原因描述一个决策点，格式示例：

```
"Skill 'code_review' (Code Review Skill) activated: task matches activation conditions"
"Skill 'code_review' sets allowed tools: code_analyzer, syntax_checker, linter"
"Skill 'security_policy' adds forbidden tools: file_deleter, system_command"
"Conflict resolution: Deny-overrides-Allow. Tools file_writer are in both allowed and forbidden lists, they will be forbidden."
"Final constraints: AllowedTools=3 (null means no restriction), ForbiddenTools=2, MaxSteps=5, MaxExecutionTime=00:05:00, ContextModifications=2, CustomValidators=1"
```

### 9.2 EnhancedAgent 集成

#### 9.2.1 日志记录

`EnhancedAgent` 在 Skill 解析后自动记录日志（如果提供了 `ILogger<EnhancedAgent>`）：

```csharp
_logger?.LogInformation(
    "Skill resolution completed. Activated {Count} skill(s): {SkillIds}. " +
    "Decision reasons: {Reasons}",
    skillResolution.ActivatedSkills.Count,
    string.Join(", ", skillResolution.ActivatedSkillIds),
    string.Join("; ", skillResolution.DecisionReasons));
```

#### 9.2.2 工具过滤日志

当 Skill 过滤了可用工具时，记录详细信息：

```csharp
_logger?.LogInformation(
    "Tool filtering applied by Skills. Allowed: {AllowedCount}/{TotalCount}. " +
    "Removed tools: {RemovedTools}",
    finalTools.Count, allTools.Count, string.Join(", ", removed));
```

#### 9.2.3 访问解析结果

通过 `EnhancedAgent.LastSkillResolution` 属性访问最后一次 Skill 解析结果：

```csharp
var result = await agent.RunAsync("Review code");
var skillResolution = agent.LastSkillResolution;

if (skillResolution != null)
{
    Console.WriteLine($"Activated skills: {string.Join(", ", skillResolution.ActivatedSkillIds)}");
    Console.WriteLine($"Decision reasons:\n{string.Join("\n", skillResolution.DecisionReasons)}");
}
```

### 9.3 工具执行拒绝原因

当工具执行被 Skill 约束拒绝时，`ToolExecutionResult` 包含详细的拒绝原因：

```csharp
// 在 DefaultToolExecutor.ExecuteAsync 中
result.Error = $"Tool '{toolName}' is forbidden by active skill constraints";
result.Metadata["skill_constraint_violation"] = true;
result.Metadata["denial_reason"] = reason; // 来自 SkillResolutionResult.GetToolDenialReason()
```

**拒绝原因格式**：
```
"Tool 'file_writer' is forbidden by Skill(s): security_policy, code_review"
"Tool 'file_writer' is not in the allowed tools list"
"Tool 'file_writer' failed custom validation by active skill constraints"
```

### 9.4 企业级审计价值

#### 9.4.1 合规审计

- **记录**：哪些 Skill 在何时激活，影响了哪些工具调用
- **追溯**：工具执行被拒绝的原因，来自哪个 Skill
- **证明**：企业策略（如安全策略、合规策略）已正确应用

#### 9.4.2 问题排查

- **诊断**：Agent 行为不符合预期时，通过 `DecisionReasons` 快速定位原因
- **调试**：验证 Skill 激活条件是否正确，约束合并是否符合预期
- **优化**：分析 Skill 冲突，优化 Skill 优先级和约束定义

#### 9.4.3 可解释性

- **透明度**：Agent 的每个决策都可以追溯到具体的 Skill 和约束
- **信任**：企业用户可以理解为什么某些操作被禁止或限制
- **责任**：明确哪些 Skill 对 Agent 行为负责，便于责任划分

### 9.5 最佳实践

#### 9.5.1 日志级别

- **Information**：Skill 解析完成、工具过滤应用
- **Warning**：Skill 冲突、约束冲突
- **Error**：Skill 注册失败、约束验证失败

#### 9.5.2 日志内容

- **必须包含**：激活的 Skill ID、最终约束摘要、工具过滤结果
- **建议包含**：决策原因完整列表（用于审计）
- **可选包含**：Skill 元数据（名称、版本、作用域）

#### 9.5.3 性能考虑

- Skill 解析是轻量级操作，日志记录不应影响性能
- 决策原因列表在内存中，仅在需要时序列化（如写入日志）
- 生产环境可选择性记录详细决策原因（通过配置控制）

---

## 🔟 未来扩展方向（非 MVP）

以下功能不在本次 MVP 范围内，但为未来扩展预留了接口：

- **Skill 组合策略**：支持 Skill 的 AND/OR 组合逻辑
- **Skill 冲突解决**：当多个 Skill 的约束冲突时的解决策略
- **Skill 生命周期钩子**：`OnActivated`、`OnDeactivated` 等事件
- **Skill 依赖管理**：Skill A 依赖 Skill B 的激活
- **Skill 条件激活**：基于时间、用户角色、环境变量等复杂条件

---

## 1️⃣1️⃣ 总结

SharpAIKit Skill 系统是一个**轻量级、类型安全、可扩展**的行为约束机制，通过最小化改动实现了企业级 Agent 治理能力。系统设计遵循"约束而非执行"的原则，确保 Skill 不会破坏现有架构的稳定性和可维护性。

**核心价值**：
- ✅ 将行为规范从 Prompt 中解耦
- ✅ 提供可发现、可激活、可约束的行为模块
- ✅ 面向企业/平台级治理，而非用户交互
- ✅ 最小可行改动，保持向后兼容

