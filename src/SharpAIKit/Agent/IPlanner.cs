using SharpAIKit.Common;

namespace SharpAIKit.Agent;

/// <summary>
/// Represents a plan step.
/// </summary>
public class PlanStep
{
    /// <summary>
    /// Gets or sets the step number.
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Gets or sets the step description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool name to use (if applicable).
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the tool arguments (if applicable).
    /// </summary>
    public Dictionary<string, object?>? ToolArguments { get; set; }

    /// <summary>
    /// Gets or sets whether this step depends on previous steps.
    /// </summary>
    public List<int> Dependencies { get; set; } = new();
}

/// <summary>
/// Represents a complete plan.
/// </summary>
public class Plan
{
    /// <summary>
    /// Gets or sets the plan steps.
    /// </summary>
    public List<PlanStep> Steps { get; set; } = new();

    /// <summary>
    /// Gets or sets the plan description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated execution time.
    /// </summary>
    public TimeSpan? EstimatedDuration { get; set; }
}

/// <summary>
/// Interface for planning agents that generate execution plans.
/// </summary>
public interface IPlanner
{
    /// <summary>
    /// Generates a plan for the given task.
    /// </summary>
    Task<Plan> PlanAsync(string task, StrongContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines an existing plan based on execution results.
    /// </summary>
    Task<Plan> RefinePlanAsync(Plan currentPlan, List<PlanStep> executedSteps, StrongContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation of a planner using LLM.
/// </summary>
public abstract class LLMPlannerBase : IPlanner
{
    protected readonly LLM.ILLMClient LLMClient;

    /// <summary>
    /// Gets or sets the planning prompt template.
    /// </summary>
    public string PlanningPromptTemplate { get; set; } = """
        Given the following task, create a step-by-step plan to accomplish it.
        Each step should be clear and actionable.
        
        Available tools:
        {tools}
        
        Task: {task}
        
        Previous context:
        {context}
        
        Respond with a numbered list of steps:
        1. [First step]
        2. [Second step]
        ...
        """;

    /// <summary>
    /// Creates a new LLM-based planner.
    /// </summary>
    protected LLMPlannerBase(LLM.ILLMClient llmClient)
    {
        LLMClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    /// <inheritdoc/>
    public abstract Task<Plan> PlanAsync(string task, StrongContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<Plan> RefinePlanAsync(Plan currentPlan, List<PlanStep> executedSteps, StrongContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a plan from LLM response.
    /// </summary>
    protected virtual Plan ParsePlan(string response)
    {
        var plan = new Plan();
        var lines = response.Split('\n');
        var stepNumber = 1;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 2 && char.IsDigit(trimmed[0]) && (trimmed[1] == '.' || trimmed[1] == ')'))
            {
                var description = trimmed.Substring(2).Trim();
                plan.Steps.Add(new PlanStep
                {
                    StepNumber = stepNumber++,
                    Description = description
                });
            }
        }

        return plan;
    }
}

/// <summary>
/// Simple planner that generates linear plans.
/// </summary>
public class SimplePlanner : LLMPlannerBase
{
    /// <summary>
    /// Creates a new simple planner.
    /// </summary>
    public SimplePlanner(LLM.ILLMClient llmClient) : base(llmClient)
    {
    }

    /// <inheritdoc/>
    public override async Task<Plan> PlanAsync(string task, StrongContext context, CancellationToken cancellationToken = default)
    {
        var tools = context.Get<List<string>>("available_tools") ?? new List<string>();
        var toolsDesc = string.Join("\n", tools);

        var prompt = PlanningPromptTemplate
            .Replace("{tools}", toolsDesc)
            .Replace("{task}", task)
            .Replace("{context}", context.ToJson());

        var response = await LLMClient.ChatAsync(prompt, cancellationToken);
        return ParsePlan(response);
    }

    /// <inheritdoc/>
    public override async Task<Plan> RefinePlanAsync(Plan currentPlan, List<PlanStep> executedSteps, StrongContext context, CancellationToken cancellationToken = default)
    {
        var executedDesc = string.Join("\n", executedSteps.Select(s => $"{s.StepNumber}. {s.Description} - Completed"));
        var remainingSteps = currentPlan.Steps.Where(s => !executedSteps.Contains(s)).ToList();

        var prompt = $"""
            Refine the following plan based on execution results.
            
            Original plan:
            {string.Join("\n", currentPlan.Steps.Select(s => $"{s.StepNumber}. {s.Description}"))}
            
            Executed steps:
            {executedDesc}
            
            Remaining steps:
            {string.Join("\n", remainingSteps.Select(s => $"{s.StepNumber}. {s.Description}"))}
            
            Context:
            {context.ToJson()}
            
            Provide a refined plan:
            """;

        var response = await LLMClient.ChatAsync(prompt, cancellationToken);
        return ParsePlan(response);
    }
}

