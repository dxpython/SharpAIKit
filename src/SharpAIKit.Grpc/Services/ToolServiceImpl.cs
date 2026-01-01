using Grpc.Core;
using SharpAIKit.Agent;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Tool operations
/// </summary>
public class ToolServiceImpl : ToolService.ToolServiceBase
{
    private readonly ILogger<ToolServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, ToolDefinition> _tools = new();
    private readonly ConcurrentDictionary<string, string> _toolImplementations = new(); // tool_id -> C# code

    public ToolServiceImpl(ILogger<ToolServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<RegisterToolResponse> RegisterTool(RegisterToolRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ToolId))
            {
                return Task.FromResult(new RegisterToolResponse
                {
                    Success = false,
                    Error = "ToolId is required"
                });
            }

            // Store tool definition
            _tools[request.ToolId] = request.Definition;
            
            // Store implementation code (for future dynamic compilation)
            if (!string.IsNullOrEmpty(request.ImplementationCode))
            {
                _toolImplementations[request.ToolId] = request.ImplementationCode;
            }

            _logger.LogInformation("Tool registered: {ToolId} ({Name})", request.ToolId, request.Definition.Name);

            return Task.FromResult(new RegisterToolResponse { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering tool: {ToolId}", request.ToolId);
            return Task.FromResult(new RegisterToolResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<ListToolsResponse> ListTools(ListToolsRequest request, ServerCallContext context)
    {
        try
        {
            var response = new ListToolsResponse { Success = true };

            // Filter by agent_id if provided (simplified - would need agent-tool mapping)
            var tools = _tools.Values;
            if (!string.IsNullOrEmpty(request.AgentId))
            {
                // In a full implementation, would filter by agent
                // For now, return all tools
            }

            foreach (var tool in tools)
            {
                response.Tools.Add(tool);
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tools");
            return Task.FromResult(new ListToolsResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<UnregisterToolResponse> UnregisterTool(UnregisterToolRequest request, ServerCallContext context)
    {
        try
        {
            if (_tools.TryRemove(request.ToolId, out _))
            {
                _toolImplementations.TryRemove(request.ToolId, out _);
                _logger.LogInformation("Tool unregistered: {ToolId}", request.ToolId);
                return Task.FromResult(new UnregisterToolResponse { Success = true });
            }

            return Task.FromResult(new UnregisterToolResponse
            {
                Success = false,
                Error = $"Tool {request.ToolId} not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering tool: {ToolId}", request.ToolId);
            return Task.FromResult(new UnregisterToolResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
}

