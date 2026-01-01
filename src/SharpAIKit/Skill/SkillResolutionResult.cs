namespace SharpAIKit.Skill;

/// <summary>
/// Skill resolution result: used for auditing, logging, and testing
/// 
/// This result object records the complete Skill decision process, including:
/// - Which Skills were activated
/// - The final constraints applied
/// - Decision reasons (for explainability)
/// </summary>
public sealed class SkillResolutionResult
{
    /// <summary>
    /// List of activated Skills (sorted by priority from high to low)
    /// </summary>
    public IReadOnlyList<ISkill> ActivatedSkills { get; init; } = new List<ISkill>();
    
    /// <summary>
    /// Final merged constraints
    /// </summary>
    public SkillConstraints FinalConstraints { get; init; } = SkillConstraints.Empty;
    
    /// <summary>
    /// List of decision reasons (for explainability and auditing)
    /// Each reason describes a decision point, for example:
    /// - "Skill 'code_review' activated: task contains 'review'"
    /// - "Tool 'file_writer' forbidden by Skill 'security_policy'"
    /// - "MaxSteps set to 5 by Skill 'code_review'"
    /// </summary>
    public IReadOnlyList<string> DecisionReasons { get; init; } = new List<string>();
    
    /// <summary>
    /// Whether any Skills were activated
    /// </summary>
    public bool HasActivatedSkills => ActivatedSkills.Count > 0;
    
    /// <summary>
    /// Gets the list of IDs of all activated Skills
    /// </summary>
    public IReadOnlyList<string> ActivatedSkillIds => 
        ActivatedSkills.Select(s => s.Metadata.Id).ToList();
    
    /// <summary>
    /// Checks if the specified tool is allowed
    /// </summary>
    public bool IsToolAllowed(string toolName) => FinalConstraints.IsToolAllowed(toolName);
    
    /// <summary>
    /// Gets the reason why a tool was denied (if denied)
    /// </summary>
    public string? GetToolDenialReason(string toolName)
    {
        if (FinalConstraints.ForbiddenTools.Contains(toolName))
        {
            var skillIds = ActivatedSkills
                .Where(s => s.GetConstraints(new Common.StrongContext()).ForbiddenTools.Contains(toolName))
                .Select(s => s.Metadata.Id)
                .ToList();
            
            return skillIds.Count > 0
                ? $"Tool '{toolName}' is forbidden by Skill(s): {string.Join(", ", skillIds)}"
                : $"Tool '{toolName}' is in the forbidden tools list";
        }
        
        if (FinalConstraints.AllowedTools != null && !FinalConstraints.AllowedTools.Contains(toolName))
        {
            return $"Tool '{toolName}' is not in the allowed tools list";
        }
        
        return null;
    }
}

