using SharpAIKit.Common;

namespace SharpAIKit.Agent;

/// <summary>
/// Result of tool execution.
/// </summary>
public class ToolExecutionResult
{
    /// <summary>
    /// Gets or sets whether execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the execution result.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

/// <summary>
/// Interface for executing tools.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes a tool with the given arguments.
    /// </summary>
    Task<ToolExecutionResult> ExecuteAsync(string toolName, Dictionary<string, object?> arguments, StrongContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available tool definitions.
    /// </summary>
    IEnumerable<ToolDefinition> GetAvailableTools();

    /// <summary>
    /// Registers a tool.
    /// </summary>
    void RegisterTool(ToolBase tool);

    /// <summary>
    /// Registers a tool definition.
    /// </summary>
    void RegisterTool(ToolDefinition definition);
}

/// <summary>
/// Default implementation of tool executor.
/// </summary>
public class DefaultToolExecutor : IToolExecutor
{
    private readonly Dictionary<string, ToolDefinition> _tools = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets or sets the maximum execution time per tool.
    /// </summary>
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, Dictionary<string, object?> arguments, StrongContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new ToolExecutionResult();

        try
        {
            ToolDefinition? tool;
            lock (_lock)
            {
                if (!_tools.TryGetValue(toolName, out tool))
                {
                    result.Success = false;
                    result.Error = $"Tool '{toolName}' not found";
                    return result;
                }
            }

            if (tool.Execute == null)
            {
                result.Success = false;
                result.Error = $"Tool '{toolName}' has no execution method";
                return result;
            }

            // Execute with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(MaxExecutionTime);

            var executionResult = await tool.Execute(arguments);
            
            stopwatch.Stop();
            result.Success = true;
            result.Result = executionResult;
            result.Duration = stopwatch.Elapsed;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Error = $"Tool execution timed out after {MaxExecutionTime.TotalSeconds} seconds";
            result.Duration = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Error = ex.Message;
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    /// <inheritdoc/>
    public IEnumerable<ToolDefinition> GetAvailableTools()
    {
        lock (_lock)
        {
            return _tools.Values.ToList();
        }
    }

    /// <inheritdoc/>
    public void RegisterTool(ToolBase tool)
    {
        var definitions = tool.GetToolDefinitions();
        lock (_lock)
        {
            foreach (var def in definitions)
            {
                _tools[def.Name] = def;
            }
        }
    }

    /// <inheritdoc/>
    public void RegisterTool(ToolDefinition definition)
    {
        lock (_lock)
        {
            _tools[definition.Name] = definition;
        }
    }
}

