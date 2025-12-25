using SharpAIKit.Common;
using SharpAIKit.Memory;

namespace SharpAIKit.Agent;

/// <summary>
/// Enhanced agent with modular architecture using IPlanner, IToolExecutor, and IMemory.
/// </summary>
public class EnhancedAgent
{
    private readonly IPlanner _planner;
    private readonly IToolExecutor _toolExecutor;
    private readonly IMemory _memory;
    private readonly LLM.ILLMClient _llmClient;

    /// <summary>
    /// Gets or sets the maximum plan refinement attempts.
    /// </summary>
    public int MaxRefinementAttempts { get; set; } = 3;

    /// <summary>
    /// Creates a new enhanced agent.
    /// </summary>
    public EnhancedAgent(
        LLM.ILLMClient llmClient,
        IPlanner? planner = null,
        IToolExecutor? toolExecutor = null,
        IMemory? memory = null)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _planner = planner ?? new SimplePlanner(llmClient);
        _toolExecutor = toolExecutor ?? new DefaultToolExecutor();
        _memory = memory ?? new BufferMemory();
    }

    /// <summary>
    /// Runs the agent on a task.
    /// </summary>
    public async Task<AgentExecutionResult> RunAsync(string task, CancellationToken cancellationToken = default)
    {
        var context = new StrongContext();
        context.Set("task", task);
        context.Set("available_tools", _toolExecutor.GetAvailableTools().Select(t => t.Name).ToList());

        // Get relevant memory
        var memoryContext = await _memory.GetContextStringAsync(task);
        context.Set("memory", memoryContext);

        // Generate plan
        var plan = await _planner.PlanAsync(task, context, cancellationToken);
        context.Set("plan", plan);

        var result = new AgentExecutionResult
        {
            Task = task,
            Plan = plan
        };

        // Execute plan steps
        var executedSteps = new List<PlanStep>();
        
        foreach (var step in plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepResult = await ExecuteStepAsync(step, context, cancellationToken);
            result.StepResults.Add(stepResult);
            executedSteps.Add(step);

            // Update memory
            await _memory.AddExchangeAsync(
                $"Step {step.StepNumber}: {step.Description}",
                stepResult.Result ?? "Completed"
            );

            // Refine plan if needed
            if (stepResult.NeedsRefinement && executedSteps.Count < MaxRefinementAttempts)
            {
                plan = await _planner.RefinePlanAsync(plan, executedSteps, context, cancellationToken);
                context.Set("plan", plan);
            }
        }

        // Synthesize final answer
        result.Answer = await SynthesizeAnswerAsync(task, result.StepResults, context, cancellationToken);
        result.Success = true;

        return result;
    }

    private async Task<StepExecutionResult> ExecuteStepAsync(PlanStep step, StrongContext context, CancellationToken cancellationToken)
    {
        var stepResult = new StepExecutionResult
        {
            Step = step,
            StartTime = DateTime.UtcNow
        };

        try
        {
            if (!string.IsNullOrEmpty(step.ToolName))
            {
                // Execute tool
                var toolResult = await _toolExecutor.ExecuteAsync(
                    step.ToolName,
                    step.ToolArguments ?? new Dictionary<string, object?>(),
                    context,
                    cancellationToken
                );

                stepResult.Success = toolResult.Success;
                stepResult.Result = toolResult.Result;
                stepResult.Error = toolResult.Error;
            }
            else
            {
                // Use LLM to execute step
                var prompt = $"""
                    Task: {context.Get<string>("task")}
                    Current step: {step.Description}
                    
                    Previous steps:
                    {string.Join("\n", context.Get<List<StepExecutionResult>>("executed_steps")?.Select(s => s.Step.Description) ?? new List<string>())}
                    
                    Execute this step:
                    """;

                var response = await _llmClient.ChatAsync(prompt, cancellationToken);
                stepResult.Result = response;
                stepResult.Success = true;
            }
        }
        catch (Exception ex)
        {
            stepResult.Success = false;
            stepResult.Error = ex.Message;
            stepResult.NeedsRefinement = true;
        }
        finally
        {
            stepResult.EndTime = DateTime.UtcNow;
            // Duration is computed property, no need to set
        }

        return stepResult;
    }

    private async Task<string> SynthesizeAnswerAsync(
        string task,
        List<StepExecutionResult> stepResults,
        StrongContext context,
        CancellationToken cancellationToken)
    {
        var resultsSummary = string.Join("\n", stepResults.Select(s => 
            $"- {s.Step.Description}: {s.Result}"));

        var prompt = $"""
            Synthesize a comprehensive answer based on the following execution results.
            
            Original task: {task}
            
            Execution results:
            {resultsSummary}
            
            Provide a final, complete answer:
            """;

        return await _llmClient.ChatAsync(prompt, cancellationToken);
    }
}

/// <summary>
/// Result of agent execution.
/// </summary>
public class AgentExecutionResult
{
    /// <summary>Gets or sets the original task.</summary>
    public string Task { get; set; } = string.Empty;
    /// <summary>Gets or sets the execution plan.</summary>
    public Plan Plan { get; set; } = new();
    /// <summary>Gets or sets the step execution results.</summary>
    public List<StepExecutionResult> StepResults { get; set; } = new();
    /// <summary>Gets or sets the final answer.</summary>
    public string Answer { get; set; } = string.Empty;
    /// <summary>Gets or sets whether execution was successful.</summary>
    public bool Success { get; set; }
}

/// <summary>
/// Result of a single step execution.
/// </summary>
public class StepExecutionResult
{
    /// <summary>Gets or sets the plan step.</summary>
    public PlanStep Step { get; set; } = new();
    /// <summary>Gets or sets whether execution was successful.</summary>
    public bool Success { get; set; }
    /// <summary>Gets or sets the execution result.</summary>
    public string? Result { get; set; }
    /// <summary>Gets or sets any error.</summary>
    public string? Error { get; set; }
    /// <summary>Gets or sets the start time.</summary>
    public DateTime StartTime { get; set; }
    /// <summary>Gets or sets the end time.</summary>
    public DateTime EndTime { get; set; }
    /// <summary>Gets the execution duration.</summary>
    public TimeSpan Duration => EndTime - StartTime;
    /// <summary>Gets or sets whether plan refinement is needed.</summary>
    public bool NeedsRefinement { get; set; }
}

