using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SharpAIKit.Observability;

/// <summary>
/// OpenTelemetry support for distributed tracing.
/// </summary>
public static class OpenTelemetrySupport
{
    /// <summary>
    /// Creates an activity for LLM operations.
    /// </summary>
    public static Activity? StartLLMActivity(string operationName, string? model = null)
    {
        var activity = new ActivitySource("SharpAIKit.LLM").StartActivity(operationName);
        if (activity != null)
        {
            activity.SetTag("llm.operation", operationName);
            if (!string.IsNullOrEmpty(model))
            {
                activity.SetTag("llm.model", model);
            }
        }
        return activity;
    }

    /// <summary>
    /// Creates an activity for tool execution.
    /// </summary>
    public static Activity? StartToolActivity(string toolName)
    {
        var activity = new ActivitySource("SharpAIKit.Tool").StartActivity("Tool.Execute");
        if (activity != null)
        {
            activity.SetTag("tool.name", toolName);
        }
        return activity;
    }

    /// <summary>
    /// Creates an activity for graph node execution.
    /// </summary>
    public static Activity? StartGraphNodeActivity(string nodeName)
    {
        var activity = new ActivitySource("SharpAIKit.Graph").StartActivity("Graph.Node");
        if (activity != null)
        {
            activity.SetTag("graph.node", nodeName);
        }
        return activity;
    }

    /// <summary>
    /// Activity source for LLM operations.
    /// </summary>
    public static readonly ActivitySource LLMActivitySource = new("SharpAIKit.LLM");

    /// <summary>
    /// Activity source for tool operations.
    /// </summary>
    public static readonly ActivitySource ToolActivitySource = new("SharpAIKit.Tool");

    /// <summary>
    /// Activity source for graph operations.
    /// </summary>
    public static readonly ActivitySource GraphActivitySource = new("SharpAIKit.Graph");
}

/// <summary>
/// Structured logging support.
/// </summary>
public class StructuredLogger
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new structured logger.
    /// </summary>
    public StructuredLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs an LLM request with structured data.
    /// </summary>
    public void LogLLMRequest(string model, List<Common.ChatMessage> messages, string? response = null, TimeSpan? duration = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["llm.model"] = model,
            ["llm.message_count"] = messages.Count,
            ["llm.response_length"] = response?.Length ?? 0,
            ["llm.duration_ms"] = duration?.TotalMilliseconds ?? 0
        });

        _logger.LogInformation(
            "LLM Request: Model={Model}, Messages={MessageCount}, ResponseLength={ResponseLength}, Duration={Duration}ms",
            model, messages.Count, response?.Length ?? 0, duration?.TotalMilliseconds ?? 0);
    }

    /// <summary>
    /// Logs a tool execution with structured data.
    /// </summary>
    public void LogToolExecution(string toolName, Dictionary<string, object?> arguments, string? result = null, bool success = true)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["tool.name"] = toolName,
            ["tool.success"] = success,
            ["tool.arg_count"] = arguments.Count
        });

        _logger.LogInformation(
            "Tool Execution: Name={ToolName}, Success={Success}, ArgCount={ArgCount}",
            toolName, success, arguments.Count);
    }

    /// <summary>
    /// Logs a graph node execution with structured data.
    /// </summary>
    public void LogGraphNode(string nodeName, TimeSpan duration, bool success = true)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["graph.node"] = nodeName,
            ["graph.success"] = success,
            ["graph.duration_ms"] = duration.TotalMilliseconds
        });

        _logger.LogInformation(
            "Graph Node: Node={NodeName}, Success={Success}, Duration={Duration}ms",
            nodeName, success, duration.TotalMilliseconds);
    }
}

