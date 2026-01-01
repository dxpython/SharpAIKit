using Xunit;
using SharpAIKit.Skill;
using SharpAIKit.Common;
using SharpAIKit.Agent;
using SharpAIKit.LLM;
using Moq;
using ToolDefinition = SharpAIKit.Agent.ToolDefinition;

namespace SharpAIKit.Tests.Skill;

/// <summary>
/// Tests for AiAgent Skill integration: verifies that AiAgent can use Skill system
/// </summary>
public class AiAgentSkillIntegrationTests
{
    #region Test Skill Implementations
    
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
    
    #endregion
    
    [Fact]
    public async Task Test_AiAgent_WithSkillResolver_FiltersTools()
    {
        // Arrange
        var mockClient = new Mock<ILLMClient>();
        var skillResolver = new DefaultSkillResolver();
        skillResolver.RegisterSkill(new ForbidToolSkill("file_writer"));
        
        var agent = new AiAgent(mockClient.Object, skillResolver: skillResolver);
        
        // Add multiple tools
        var calculatorTool = new ToolDefinition
        {
            Name = "calculator",
            Description = "Calculator tool",
            Parameters = new List<ToolParameter>(),
            Execute = _ => Task.FromResult("42")
        };
        var fileWriterTool = new ToolDefinition
        {
            Name = "file_writer",
            Description = "File writer tool",
            Parameters = new List<ToolParameter>(),
            Execute = _ => Task.FromResult("written")
        };
        
        agent.AddTool(calculatorTool);
        agent.AddTool(fileWriterTool);
        
        // Mock LLM to return answer immediately
        mockClient.Setup(c => c.ChatAsync(
            It.IsAny<List<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"action\": \"answer\", \"content\": \"Test answer\"}");
        
        // Act: RunAsync will trigger Skill resolution
        await agent.RunAsync("Test task");
        
        // Assert: Skill resolution should be stored after RunAsync
        Assert.NotNull(agent.LastSkillResolution);
        Assert.True(agent.LastSkillResolution.HasActivatedSkills);
        Assert.Contains("forbid_file_writer", agent.LastSkillResolution.ActivatedSkillIds);
        
        // Verify that file_writer is forbidden
        Assert.False(agent.LastSkillResolution.IsToolAllowed("file_writer"));
        Assert.True(agent.LastSkillResolution.IsToolAllowed("calculator"));
    }
    
    [Fact]
    public void Test_AiAgent_WithoutSkillResolver_WorksNormally()
    {
        // Arrange
        var mockClient = new Mock<ILLMClient>();
        var agent = new AiAgent(mockClient.Object); // No skill resolver
        
        // Add tools
        var calculatorTool = new ToolDefinition
        {
            Name = "calculator",
            Description = "Calculator tool",
            Parameters = new List<ToolParameter>(),
            Execute = _ => Task.FromResult("42")
        };
        agent.AddTool(calculatorTool);
        
        // Assert: Should work normally without Skill system
        Assert.Null(agent.LastSkillResolution);
        // Agent should function normally (backward compatible)
    }
    
    [Fact]
    public async Task Test_AiAgent_SkillResolution_StoredInLastSkillResolution()
    {
        // Arrange
        var mockClient = new Mock<ILLMClient>();
        var skillResolver = new DefaultSkillResolver();
        skillResolver.RegisterSkill(new AllowToolSkill(new[] { "calculator" }));
        
        var agent = new AiAgent(mockClient.Object, skillResolver: skillResolver);
        
        // Mock LLM response to return a direct answer (to avoid tool execution complexity)
        mockClient.Setup(c => c.ChatAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("Direct answer");
        
        mockClient.Setup(c => c.ChatAsync(
            It.IsAny<List<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"action\": \"answer\", \"content\": \"Test answer\"}");
        
        // Act
        var result = await agent.RunAsync("Test task");
        
        // Assert: Skill resolution should be stored
        Assert.NotNull(agent.LastSkillResolution);
        Assert.True(agent.LastSkillResolution.HasActivatedSkills);
        Assert.Contains("allow_calculator", agent.LastSkillResolution.ActivatedSkillIds);
    }
    
    [Fact]
    public async Task Test_AiAgent_SkillConstraints_AppliedToMaxSteps()
    {
        // Arrange
        var mockClient = new Mock<ILLMClient>();
        var skillResolver = new DefaultSkillResolver();
        
        // Create a skill that limits MaxSteps
        var maxStepsSkill = new MaxStepsSkill(5, priority: 10);
        skillResolver.RegisterSkill(maxStepsSkill);
        
        var agent = new AiAgent(mockClient.Object, skillResolver: skillResolver)
        {
            MaxSteps = 10 // Original value
        };
        
        // Mock LLM to return answer immediately
        mockClient.Setup(c => c.ChatAsync(
            It.IsAny<List<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"action\": \"answer\", \"content\": \"Test\"}");
        
        // Act
        await agent.RunAsync("Test");
        
        // Assert: MaxSteps should be constrained by Skill
        Assert.Equal(5, agent.MaxSteps); // Should be reduced from 10 to 5
    }
}

/// <summary>
/// Helper Skill for testing MaxSteps constraint
/// </summary>
internal class MaxStepsSkill : ISkill
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

