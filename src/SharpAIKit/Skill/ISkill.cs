using SharpAIKit.Common;

namespace SharpAIKit.Skill;

/// <summary>
/// Skill interface: defines behavior constraints and activation conditions
/// 
/// Skills are behavior constraint layers that do not directly execute tasks,
/// but only influence "how" and "what" the Agent is allowed to execute.
/// </summary>
public interface ISkill
{
    /// <summary>Gets the Skill metadata</summary>
    SkillMetadata Metadata { get; }
    
    /// <summary>
    /// Checks if the Skill should be activated
    /// </summary>
    /// <param name="task">User task description</param>
    /// <param name="context">Current context</param>
    /// <returns>true if should activate, false otherwise</returns>
    bool ShouldActivate(string task, StrongContext context);
    
    /// <summary>
    /// Gets the Skill's constraints
    /// </summary>
    /// <param name="context">Current context</param>
    /// <returns>Constraint definition</returns>
    SkillConstraints GetConstraints(StrongContext context);
    
    /// <summary>
    /// Applies constraints during planning phase (optional, for modifying planning prompts)
    /// Default returns null, meaning no modification to the planning prompt
    /// </summary>
    /// <param name="planningPrompt">Original planning prompt</param>
    /// <param name="context">Current context</param>
    /// <returns>Modified planning prompt, null means no modification</returns>
    string? ApplyToPlanning(string planningPrompt, StrongContext context) => null;
}

