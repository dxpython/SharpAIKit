using SharpAIKit.Common;

namespace SharpAIKit.Skill;

/// <summary>
/// Skill constraints definition: expresses restrictions on Agent behavior imposed by a Skill
/// 
/// Constraint merging rules:
/// - AllowedTools: Intersection of multiple Skills' whitelists (most restrictive)
/// - ForbiddenTools: Union of multiple Skills' blacklists (any deny means deny)
/// - MaxSteps/MaxExecutionTime: Minimum value (most restrictive)
/// - ContextModifications: Later registered Skills override earlier ones (applied after priority sorting)
/// - CustomValidator: All validators must pass (AND logic)
/// 
/// Conflict resolution:
/// - ForbiddenTools always override AllowedTools (Deny-overrides-Allow)
/// - Higher priority Skills' constraints take precedence over lower priority ones
/// </summary>
public sealed class SkillConstraints
{
    /// <summary>
    /// Whitelist of allowed tools
    /// - null means no restriction (all tools allowed)
    /// - Non-empty set means only tools in the set are allowed
    /// - When conflicting with ForbiddenTools, ForbiddenTools take precedence (Deny-overrides-Allow)
    /// </summary>
    public IReadOnlySet<string>? AllowedTools { get; init; }
    
    /// <summary>
    /// Blacklist of forbidden tools
    /// - Always effective, has higher priority than AllowedTools
    /// - Even if a tool is in the whitelist, it will be rejected if it's also in the blacklist
    /// </summary>
    public IReadOnlySet<string> ForbiddenTools { get; init; } = new HashSet<string>();
    
    /// <summary>
    /// Maximum execution steps
    /// - null means no restriction
    /// - When multiple Skills restrict this, the minimum value is taken
    /// </summary>
    public int? MaxSteps { get; init; }
    
    /// <summary>
    /// Maximum execution time
    /// - null means no restriction
    /// - When multiple Skills restrict this, the minimum value is taken
    /// </summary>
    public TimeSpan? MaxExecutionTime { get; init; }
    
    /// <summary>
    /// Context modification rules (key-value pairs that will be injected into Agent context)
    /// - Modifications from multiple Skills are merged, later registered ones override earlier ones
    /// - Applied after sorting by Skill priority (higher priority Skills applied later, overriding lower priority)
    /// </summary>
    public IReadOnlyDictionary<string, object?> ContextModifications { get; init; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Custom constraint validation function (called before tool execution)
    /// - Returns true to allow execution, false to reject
    /// - All validators from multiple Skills must return true for execution to be allowed (AND logic)
    /// - Parameters: tool name, tool arguments, current context
    /// </summary>
    public Func<string, Dictionary<string, object?>, StrongContext, bool>? CustomValidator { get; init; }
    
    /// <summary>
    /// Creates an empty constraint (no restrictions applied)
    /// </summary>
    public static SkillConstraints Empty => new();
    
    /// <summary>
    /// Checks if a tool is allowed to be used
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <returns>true if allowed, false if forbidden</returns>
    public bool IsToolAllowed(string toolName)
    {
        // Deny-overrides-Allow: blacklist takes precedence
        if (ForbiddenTools.Contains(toolName))
            return false;
        
        // If whitelist is null, no restriction
        if (AllowedTools == null)
            return true;
        
        // Check if in whitelist
        return AllowedTools.Contains(toolName);
    }
}

