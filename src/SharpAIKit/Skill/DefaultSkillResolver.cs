using System.Text;
using SharpAIKit.Common;

namespace SharpAIKit.Skill;

/// <summary>
/// Default Skill resolver implementation
/// 
/// Constraint merging strategy (deterministic and predictable):
/// 1. Skill activation: Check ShouldActivate in registration order, activate matching ones
/// 2. Skill sorting: Sort by priority descending (higher priority first)
/// 3. Constraint merging:
///    - AllowedTools: Intersection (most restrictive)
///    - ForbiddenTools: Union (any deny means deny)
///    - MaxSteps/MaxExecutionTime: Minimum value (most restrictive)
///    - ContextModifications: Applied from low to high priority (high priority overrides low priority)
///    - CustomValidator: Combined with AND logic (all validators must pass)
/// 4. Conflict resolution: ForbiddenTools always override AllowedTools (Deny-overrides-Allow)
/// </summary>
public sealed class DefaultSkillResolver : ISkillResolver
{
    private readonly List<ISkill> _skills = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// Registers a Skill
    /// </summary>
    public void RegisterSkill(ISkill skill)
    {
        if (skill == null)
            throw new ArgumentNullException(nameof(skill));
        
        lock (_lock)
        {
            // Check if a Skill with the same ID already exists
            if (_skills.Any(s => s.Metadata.Id == skill.Metadata.Id))
            {
                throw new InvalidOperationException(
                    $"Skill with ID '{skill.Metadata.Id}' is already registered.");
            }
            
            _skills.Add(skill);
        }
    }
    
    /// <summary>
    /// Resolves Skills and returns the complete result
    /// </summary>
    public SkillResolutionResult Resolve(string task, StrongContext context)
    {
        if (string.IsNullOrEmpty(task))
            throw new ArgumentException("Task cannot be null or empty", nameof(task));
        
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        
        List<ISkill> activatedSkills;
        List<string> decisionReasons;
        
        lock (_lock)
        {
            // 1. Skill Discovery & Activation
            activatedSkills = new List<ISkill>();
            decisionReasons = new List<string>();
            
            foreach (var skill in _skills)
            {
                if (skill.ShouldActivate(task, context))
                {
                    activatedSkills.Add(skill);
                    decisionReasons.Add(
                        $"Skill '{skill.Metadata.Id}' ({skill.Metadata.Name}) activated: " +
                        $"task matches activation conditions");
                }
            }
            
            // 2. Sort by Priority (descending: higher priority first)
            activatedSkills = activatedSkills
                .OrderByDescending(s => s.Metadata.Priority)
                .ThenBy(s => s.Metadata.Id) // Stable sort by ID for deterministic results
                .ToList();
            
            // 3. Merge Constraints
            var finalConstraints = MergeConstraints(activatedSkills, context, decisionReasons);
            
            return new SkillResolutionResult
            {
                ActivatedSkills = activatedSkills,
                FinalConstraints = finalConstraints,
                DecisionReasons = decisionReasons
            };
        }
    }
    
    /// <summary>
    /// Gets all registered Skills (metadata only)
    /// </summary>
    public IReadOnlyList<SkillMetadata> GetAllSkills()
    {
        lock (_lock)
        {
            return _skills.Select(s => s.Metadata).ToList();
        }
    }
    
    /// <summary>
    /// Merges constraints from multiple Skills (deterministic algorithm)
    /// </summary>
    private SkillConstraints MergeConstraints(
        IReadOnlyList<ISkill> skills,
        StrongContext context,
        List<string> decisionReasons)
    {
        if (skills.Count == 0)
        {
            decisionReasons.Add("No skills activated, using empty constraints");
            return SkillConstraints.Empty;
        }
        
        // Initialize merged constraints
        HashSet<string>? allowedTools = null;
        var forbiddenTools = new HashSet<string>();
        int? maxSteps = null;
        TimeSpan? maxExecutionTime = null;
        var contextModifications = new Dictionary<string, object?>();
        var customValidators = new List<Func<string, Dictionary<string, object?>, StrongContext, bool>>();
        
        // Process skills in priority order (already sorted)
        foreach (var skill in skills)
        {
            var constraints = skill.GetConstraints(context);
            var skillId = skill.Metadata.Id;
            
            // Merge AllowedTools (intersection - most restrictive)
            if (constraints.AllowedTools != null)
            {
                if (allowedTools == null)
                {
                    allowedTools = new HashSet<string>(constraints.AllowedTools);
                    decisionReasons.Add(
                        $"Skill '{skillId}' sets allowed tools: {string.Join(", ", constraints.AllowedTools)}");
                }
                else
                {
                    var beforeCount = allowedTools.Count;
                    allowedTools.IntersectWith(constraints.AllowedTools);
                    var afterCount = allowedTools.Count;
                    if (beforeCount != afterCount)
                    {
                        decisionReasons.Add(
                            $"Skill '{skillId}' restricts allowed tools from {beforeCount} to {afterCount} " +
                            $"(intersection with existing: {string.Join(", ", allowedTools)})");
                    }
                }
            }
            
            // Merge ForbiddenTools (union - any deny means deny)
            var beforeForbiddenCount = forbiddenTools.Count;
            forbiddenTools.UnionWith(constraints.ForbiddenTools);
            if (forbiddenTools.Count > beforeForbiddenCount)
            {
                var newlyForbidden = constraints.ForbiddenTools.Except(
                    forbiddenTools.Except(constraints.ForbiddenTools)).ToList();
                decisionReasons.Add(
                    $"Skill '{skillId}' adds forbidden tools: {string.Join(", ", newlyForbidden)}");
            }
            
            // Merge MaxSteps (minimum - most restrictive)
            if (constraints.MaxSteps.HasValue)
            {
                if (!maxSteps.HasValue || constraints.MaxSteps.Value < maxSteps.Value)
                {
                    var oldValue = maxSteps;
                    maxSteps = constraints.MaxSteps.Value;
                    decisionReasons.Add(
                        $"Skill '{skillId}' sets MaxSteps to {maxSteps}" +
                        (oldValue.HasValue ? $" (was {oldValue})" : ""));
                }
            }
            
            // Merge MaxExecutionTime (minimum - most restrictive)
            if (constraints.MaxExecutionTime.HasValue)
            {
                if (!maxExecutionTime.HasValue || 
                    constraints.MaxExecutionTime.Value < maxExecutionTime.Value)
                {
                    var oldValue = maxExecutionTime;
                    maxExecutionTime = constraints.MaxExecutionTime.Value;
                    decisionReasons.Add(
                        $"Skill '{skillId}' sets MaxExecutionTime to {maxExecutionTime}" +
                        (oldValue.HasValue ? $" (was {oldValue})" : ""));
                }
            }
            
            // Merge ContextModifications (later skills override earlier ones)
            // Since we process in priority order (high to low), high priority skills override low priority
            foreach (var kvp in constraints.ContextModifications)
            {
                var hadValue = contextModifications.ContainsKey(kvp.Key);
                contextModifications[kvp.Key] = kvp.Value;
                if (hadValue)
                {
                    decisionReasons.Add(
                        $"Skill '{skillId}' overrides context key '{kvp.Key}' " +
                        $"(high priority skill takes precedence)");
                }
                else
                {
                    decisionReasons.Add(
                        $"Skill '{skillId}' adds context key '{kvp.Key}'");
                }
            }
            
            // Collect CustomValidators (all must pass - AND logic)
            if (constraints.CustomValidator != null)
            {
                customValidators.Add(constraints.CustomValidator);
                decisionReasons.Add(
                    $"Skill '{skillId}' adds custom validator");
            }
        }
        
        // Apply Deny-overrides-Allow rule
        if (allowedTools != null && forbiddenTools.Count > 0)
        {
            var conflicted = allowedTools.Intersect(forbiddenTools).ToList();
            if (conflicted.Count > 0)
            {
                decisionReasons.Add(
                    $"Conflict resolution: Deny-overrides-Allow. " +
                    $"Tools {string.Join(", ", conflicted)} are in both allowed and forbidden lists, " +
                    $"they will be forbidden.");
                allowedTools.ExceptWith(forbiddenTools);
            }
        }
        
        // Combine custom validators into a single validator (AND logic)
        Func<string, Dictionary<string, object?>, StrongContext, bool>? combinedValidator = null;
        if (customValidators.Count > 0)
        {
            combinedValidator = (toolName, args, ctx) =>
            {
                // All validators must return true
                foreach (var validator in customValidators)
                {
                    if (!validator(toolName, args, ctx))
                        return false;
                }
                return true;
            };
            
            if (customValidators.Count > 1)
            {
                decisionReasons.Add(
                    $"Combined {customValidators.Count} custom validators (AND logic)");
            }
        }
        
        // Build final constraints
        var finalConstraints = new SkillConstraints
        {
            AllowedTools = allowedTools,
            ForbiddenTools = forbiddenTools,
            MaxSteps = maxSteps,
            MaxExecutionTime = maxExecutionTime,
            ContextModifications = contextModifications,
            CustomValidator = combinedValidator
        };
        
        decisionReasons.Add(
            $"Final constraints: " +
            $"AllowedTools={allowedTools?.Count ?? 0} (null means no restriction), " +
            $"ForbiddenTools={forbiddenTools.Count}, " +
            $"MaxSteps={maxSteps?.ToString() ?? "unlimited"}, " +
            $"MaxExecutionTime={maxExecutionTime?.ToString() ?? "unlimited"}, " +
            $"ContextModifications={contextModifications.Count}, " +
            $"CustomValidators={customValidators.Count}");
        
        return finalConstraints;
    }
}

