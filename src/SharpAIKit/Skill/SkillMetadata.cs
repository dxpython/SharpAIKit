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

