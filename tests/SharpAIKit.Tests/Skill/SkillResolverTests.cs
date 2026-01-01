using Xunit;
using SharpAIKit.Skill;
using SharpAIKit.Common;

namespace SharpAIKit.Tests.Skill;

/// <summary>
/// Skill Resolver 测试：验证约束合并策略、冲突解决和可预测性
/// </summary>
public class SkillResolverTests
{
    #region Test Skill Implementations
    
    /// <summary>
    /// 测试 Skill：禁止特定工具
    /// </summary>
    private class ForbidToolSkill : ISkill
    {
        private readonly string _toolName;
        
        public ForbidToolSkill(string toolName, int priority = 0)
        {
            _toolName = toolName;
            Metadata = new SkillMetadata
            {
                Id = $"forbid_{toolName}",
                Name = $"Forbid {toolName}",
                Description = $"Forbids tool {toolName}",
                Priority = priority
            };
        }
        
        public SkillMetadata Metadata { get; }
        
        public bool ShouldActivate(string task, StrongContext context) => true;
        
        public SkillConstraints GetConstraints(StrongContext context)
        {
            return new SkillConstraints
            {
                ForbiddenTools = new HashSet<string> { _toolName }
            };
        }
    }
    
    /// <summary>
    /// 测试 Skill：允许特定工具
    /// </summary>
    private class AllowToolSkill : ISkill
    {
        private readonly HashSet<string> _allowedTools;
        
        public AllowToolSkill(IEnumerable<string> allowedTools, int priority = 0)
        {
            _allowedTools = new HashSet<string>(allowedTools);
            Metadata = new SkillMetadata
            {
                Id = $"allow_{string.Join("_", _allowedTools)}",
                Name = $"Allow {string.Join(", ", _allowedTools)}",
                Description = $"Allows only specified tools",
                Priority = priority
            };
        }
        
        public SkillMetadata Metadata { get; }
        
        public bool ShouldActivate(string task, StrongContext context) => true;
        
        public SkillConstraints GetConstraints(StrongContext context)
        {
            return new SkillConstraints
            {
                AllowedTools = _allowedTools
            };
        }
    }
    
    /// <summary>
    /// 测试 Skill：设置最大步骤数
    /// </summary>
    private class MaxStepsSkill : ISkill
    {
        private readonly int _maxSteps;
        
        public MaxStepsSkill(int maxSteps, int priority = 0)
        {
            _maxSteps = maxSteps;
            Metadata = new SkillMetadata
            {
                Id = $"max_steps_{maxSteps}",
                Name = $"Max Steps {maxSteps}",
                Description = $"Sets max steps to {maxSteps}",
                Priority = priority
            };
        }
        
        public SkillMetadata Metadata { get; }
        
        public bool ShouldActivate(string task, StrongContext context) => true;
        
        public SkillConstraints GetConstraints(StrongContext context)
        {
            return new SkillConstraints
            {
                MaxSteps = _maxSteps
            };
        }
    }
    
    /// <summary>
    /// 测试 Skill：条件激活（基于任务关键词）
    /// </summary>
    private class ConditionalSkill : ISkill
    {
        private readonly string _keyword;
        
        public ConditionalSkill(string keyword, int priority = 0)
        {
            _keyword = keyword;
            Metadata = new SkillMetadata
            {
                Id = $"conditional_{keyword}",
                Name = $"Conditional {keyword}",
                Description = $"Activates when task contains '{keyword}'",
                Priority = priority
            };
        }
        
        public SkillMetadata Metadata { get; }
        
        public bool ShouldActivate(string task, StrongContext context)
        {
            return task.Contains(_keyword, StringComparison.OrdinalIgnoreCase);
        }
        
        public SkillConstraints GetConstraints(StrongContext context)
        {
            return new SkillConstraints
            {
                ForbiddenTools = new HashSet<string> { "forbidden_tool" }
            };
        }
    }
    
    #endregion
    
    [Fact]
    public void Test1_SingleSkillForbidsTool_ToolIsRejected()
    {
        // Arrange
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new ForbidToolSkill("file_writer"));
        
        // Act
        var result = resolver.Resolve("Write a file", new StrongContext());
        
        // Assert
        Assert.True(result.HasActivatedSkills);
        Assert.Single(result.ActivatedSkills);
        Assert.False(result.IsToolAllowed("file_writer"));
        Assert.True(result.IsToolAllowed("calculator")); // Other tools still allowed
        
        // Verify denial reason
        var reason = result.GetToolDenialReason("file_writer");
        Assert.NotNull(reason);
        Assert.Contains("forbidden", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("forbid_file_writer", reason);
    }
    
    [Fact]
    public void Test2_TwoSkillsConflict_AllowVsDeny_DenyWins()
    {
        // Arrange: Skill A allows "file_writer", Skill B forbids it
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new AllowToolSkill(new[] { "file_writer", "calculator" }, priority: 10));
        resolver.RegisterSkill(new ForbidToolSkill("file_writer", priority: 5));
        
        // Act
        var result = resolver.Resolve("Do something", new StrongContext());
        
        // Assert: Deny-overrides-Allow
        Assert.Equal(2, result.ActivatedSkills.Count);
        Assert.False(result.IsToolAllowed("file_writer")); // Deny wins
        Assert.True(result.IsToolAllowed("calculator")); // Still allowed
        
        // Verify decision reasons mention conflict resolution
        var reasons = string.Join("; ", result.DecisionReasons);
        Assert.Contains("Deny-overrides-Allow", reasons, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("file_writer", reasons);
    }
    
    [Fact]
    public void Test3_HighPrioritySkillOverridesLowPriority()
    {
        // Arrange: Low priority sets MaxSteps=10, High priority sets MaxSteps=5
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new MaxStepsSkill(10, priority: 5));
        resolver.RegisterSkill(new MaxStepsSkill(5, priority: 20)); // Higher priority
        
        // Act
        var result = resolver.Resolve("Do something", new StrongContext());
        
        // Assert: Minimum wins (most restrictive)
        Assert.Equal(2, result.ActivatedSkills.Count);
        Assert.Equal(5, result.FinalConstraints.MaxSteps); // Minimum value
        
        // Verify high priority skill is processed first (in decision reasons)
        var highPrioritySkill = result.ActivatedSkills.First(s => s.Metadata.Priority == 20);
        var lowPrioritySkill = result.ActivatedSkills.First(s => s.Metadata.Priority == 5);
        
        // High priority skill should be listed first
        Assert.Equal(highPrioritySkill.Metadata.Id, result.ActivatedSkills[0].Metadata.Id);
    }
    
    [Fact]
    public void Test4_NoSkillsActivated_BehaviorUnchanged()
    {
        // Arrange: Skill that doesn't activate
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new ConditionalSkill("review")); // Only activates for "review" tasks
        
        // Act: Task that doesn't match
        var result = resolver.Resolve("Write code", new StrongContext());
        
        // Assert: No skills activated, empty constraints
        Assert.False(result.HasActivatedSkills);
        Assert.Empty(result.ActivatedSkills);
        
        // Compare constraint properties instead of object reference
        var constraints = result.FinalConstraints;
        Assert.Null(constraints.AllowedTools); // No whitelist
        Assert.Empty(constraints.ForbiddenTools); // No blacklist
        Assert.Null(constraints.MaxSteps);
        Assert.Null(constraints.MaxExecutionTime);
        Assert.Empty(constraints.ContextModifications);
        Assert.Null(constraints.CustomValidator);
        
        // All tools should be allowed (no restrictions)
        Assert.True(result.IsToolAllowed("any_tool"));
    }
    
    [Fact]
    public void Test5_RemoveSkill_BehaviorRecovers()
    {
        // Arrange: Register and resolve with skill
        var resolver = new DefaultSkillResolver();
        var skill = new ForbidToolSkill("file_writer");
        resolver.RegisterSkill(skill);
        
        var result1 = resolver.Resolve("Do something", new StrongContext());
        Assert.False(result1.IsToolAllowed("file_writer"));
        
        // Act: Create new resolver without the skill (simulating removal)
        var resolver2 = new DefaultSkillResolver();
        // Don't register the skill
        
        var result2 = resolver2.Resolve("Do something", new StrongContext());
        
        // Assert: Behavior recovers (tool is allowed again)
        Assert.False(result2.HasActivatedSkills);
        Assert.True(result2.IsToolAllowed("file_writer")); // No longer forbidden
    }
    
    [Fact]
    public void Test_ConstraintMerging_AllowedToolsIntersection()
    {
        // Arrange: Two skills with overlapping allowed tools
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new AllowToolSkill(new[] { "tool1", "tool2", "tool3" }));
        resolver.RegisterSkill(new AllowToolSkill(new[] { "tool2", "tool3", "tool4" }));
        
        // Act
        var result = resolver.Resolve("Do something", new StrongContext());
        
        // Assert: Intersection = {tool2, tool3}
        Assert.NotNull(result.FinalConstraints.AllowedTools);
        Assert.Equal(2, result.FinalConstraints.AllowedTools.Count);
        Assert.Contains("tool2", result.FinalConstraints.AllowedTools);
        Assert.Contains("tool3", result.FinalConstraints.AllowedTools);
        Assert.DoesNotContain("tool1", result.FinalConstraints.AllowedTools);
        Assert.DoesNotContain("tool4", result.FinalConstraints.AllowedTools);
    }
    
    [Fact]
    public void Test_ConstraintMerging_ForbiddenToolsUnion()
    {
        // Arrange: Two skills with different forbidden tools
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new ForbidToolSkill("tool1"));
        resolver.RegisterSkill(new ForbidToolSkill("tool2"));
        
        // Act
        var result = resolver.Resolve("Do something", new StrongContext());
        
        // Assert: Union = {tool1, tool2}
        Assert.Equal(2, result.FinalConstraints.ForbiddenTools.Count);
        Assert.Contains("tool1", result.FinalConstraints.ForbiddenTools);
        Assert.Contains("tool2", result.FinalConstraints.ForbiddenTools);
    }
    
    [Fact]
    public void Test_ConstraintMerging_MaxStepsMinimum()
    {
        // Arrange: Multiple skills with different MaxSteps
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new MaxStepsSkill(20));
        resolver.RegisterSkill(new MaxStepsSkill(10));
        resolver.RegisterSkill(new MaxStepsSkill(15));
        
        // Act
        var result = resolver.Resolve("Do something", new StrongContext());
        
        // Assert: Minimum = 10
        Assert.Equal(10, result.FinalConstraints.MaxSteps);
    }
    
    [Fact]
    public void Test_SkillResolutionResult_ProvidesAuditTrail()
    {
        // Arrange
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new ForbidToolSkill("tool1"));
        resolver.RegisterSkill(new AllowToolSkill(new[] { "tool2", "tool3" }));
        
        // Act
        var result = resolver.Resolve("Do something", new StrongContext());
        
        // Assert: Decision reasons provide audit trail
        Assert.NotEmpty(result.DecisionReasons);
        Assert.All(result.DecisionReasons, reason => Assert.False(string.IsNullOrWhiteSpace(reason)));
        
        // Should contain information about activated skills
        var reasonsText = string.Join("; ", result.DecisionReasons);
        Assert.Contains("activated", reasonsText, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void Test_SkillResolutionResult_GetToolDenialReason()
    {
        // Arrange
        var resolver = new DefaultSkillResolver();
        resolver.RegisterSkill(new ForbidToolSkill("forbidden_tool"));
        
        // Act
        var result = resolver.Resolve("Do something", new StrongContext());
        
        // Assert
        var reason = result.GetToolDenialReason("forbidden_tool");
        Assert.NotNull(reason);
        Assert.Contains("forbidden_tool", reason);
        Assert.Contains("forbid_forbidden_tool", reason); // Skill ID
        
        // Non-forbidden tool should return null
        Assert.Null(result.GetToolDenialReason("allowed_tool"));
    }
}

