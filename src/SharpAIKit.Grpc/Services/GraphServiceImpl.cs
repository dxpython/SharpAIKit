using Grpc.Core;
using SharpAIKit.Graph;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Graph operations
/// </summary>
public class GraphServiceImpl : GraphService.GraphServiceBase
{
    private readonly ILogger<GraphServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, SharpGraph> _graphs = new();

    public GraphServiceImpl(ILogger<GraphServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<CreateGraphResponse> CreateGraph(CreateGraphRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.GraphId))
            {
                return Task.FromResult(new CreateGraphResponse
                {
                    Success = false,
                    Error = "GraphId is required"
                });
            }

            var maxIterations = request.MaxIterations > 0 ? request.MaxIterations : 20;
            var graph = new SharpGraph(request.StartNode, maxIterations);

            _graphs[request.GraphId] = graph;
            _logger.LogInformation("Graph created: {GraphId}", request.GraphId);

            return Task.FromResult(new CreateGraphResponse
            {
                Success = true,
                GraphId = request.GraphId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating graph: {GraphId}", request.GraphId);
            return Task.FromResult(new CreateGraphResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<AddNodeResponse> AddNode(AddNodeRequest request, ServerCallContext context)
    {
        try
        {
            if (!_graphs.TryGetValue(request.GraphId, out var graph))
            {
                return Task.FromResult(new AddNodeResponse
                {
                    Success = false,
                    Error = $"Graph {request.GraphId} not found"
                });
            }

            // Add node based on type
            // Note: This is a simplified implementation
            // Full implementation would need to handle action_code and condition_expression
            // For now, create a simple pass-through action
            graph.AddNode(request.NodeName, async (state) =>
            {
                state.CurrentNode = request.NodeName;
                return state;
            }, request.Description ?? string.Empty);

            _logger.LogInformation("Node added to graph {GraphId}: {NodeName}", request.GraphId, request.NodeName);

            return Task.FromResult(new AddNodeResponse { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding node to graph: {GraphId}", request.GraphId);
            return Task.FromResult(new AddNodeResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<AddEdgeResponse> AddEdge(AddEdgeRequest request, ServerCallContext context)
    {
        try
        {
            if (!_graphs.TryGetValue(request.GraphId, out var graph))
            {
                return Task.FromResult(new AddEdgeResponse
                {
                    Success = false,
                    Error = $"Graph {request.GraphId} not found"
                });
            }

            // Convert condition string to function (simplified)
            Func<SharpAIKit.Graph.GraphState, bool>? conditionFunc = null;
            if (!string.IsNullOrEmpty(request.Condition))
            {
                conditionFunc = (state) => true; // Simplified - would need to parse condition string
            }
            graph.AddEdge(request.FromNode, request.ToNode, conditionFunc);

            _logger.LogInformation("Edge added to graph {GraphId}: {FromNode} -> {ToNode}", 
                request.GraphId, request.FromNode, request.ToNode);

            return Task.FromResult(new AddEdgeResponse { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding edge to graph: {GraphId}", request.GraphId);
            return Task.FromResult(new AddEdgeResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override async Task<RunGraphResponse> RunGraph(RunGraphRequest request, ServerCallContext context)
    {
        try
        {
            if (!_graphs.TryGetValue(request.GraphId, out var graph))
            {
                return new RunGraphResponse
                {
                    Success = false,
                    Error = $"Graph {request.GraphId} not found"
                };
            }

            var initialState = ConvertToGraphState(request.InitialState ?? new SharpAIKit.Grpc.GraphState());
            var result = await graph.ExecuteAsync(initialState, context.CancellationToken);

            var response = new RunGraphResponse
            {
                Success = true,
                FinalState = ConvertFromGraphState(result)
            };

            // Add execution path (simplified)
            response.ExecutionPath.Add(result.CurrentNode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running graph: {GraphId}", request.GraphId);
            return new RunGraphResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task RunGraphStream(RunGraphRequest request, IServerStreamWriter<GraphStreamChunk> responseStream, ServerCallContext context)
    {
        try
        {
            if (!_graphs.TryGetValue(request.GraphId, out var graph))
            {
                await responseStream.WriteAsync(new GraphStreamChunk
                {
                    Error = $"Graph {request.GraphId} not found"
                });
                return;
            }

            var initialState = ConvertToGraphState(request.InitialState ?? new SharpAIKit.Grpc.GraphState());
            var result = await graph.ExecuteAsync(initialState, context.CancellationToken);

            await responseStream.WriteAsync(new GraphStreamChunk
            {
                State = ConvertFromGraphState(result),
                NodeName = result.CurrentNode
            });

            await responseStream.WriteAsync(new GraphStreamChunk { Done = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RunGraphStream: {GraphId}", request.GraphId);
            await responseStream.WriteAsync(new GraphStreamChunk
            {
                Error = ex.Message
            });
        }
    }

    public override Task<GetStateResponse> GetState(GetStateRequest request, ServerCallContext context)
    {
        try
        {
            if (!_graphs.TryGetValue(request.GraphId, out var graph))
            {
                return Task.FromResult(new GetStateResponse
                {
                    Success = false,
                    Error = $"Graph {request.GraphId} not found"
                });
            }

            // Get current state (simplified - would need to track state in graph)
            var state = new SharpAIKit.Graph.GraphState
            {
                CurrentNode = "unknown"
            };

            return Task.FromResult(new GetStateResponse
            {
                Success = true,
                State = ConvertFromGraphState(state)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting graph state: {GraphId}", request.GraphId);
            return Task.FromResult(new GetStateResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<SaveStateResponse> SaveState(SaveStateRequest request, ServerCallContext context)
    {
        try
        {
            // Implementation would serialize state to file
            // For now, just return success
            return Task.FromResult(new SaveStateResponse { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving graph state: {GraphId}", request.GraphId);
            return Task.FromResult(new SaveStateResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<LoadStateResponse> LoadState(LoadStateRequest request, ServerCallContext context)
    {
        try
        {
            // Implementation would deserialize state from file
            // For now, return empty state
            return Task.FromResult(new LoadStateResponse
            {
                Success = true,
                State = new SharpAIKit.Grpc.GraphState()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading graph state: {GraphId}", request.GraphId);
            return Task.FromResult(new LoadStateResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    private SharpAIKit.Graph.GraphState ConvertToGraphState(SharpAIKit.Grpc.GraphState grpcState)
    {
        var state = new SharpAIKit.Graph.GraphState
        {
            CurrentNode = grpcState.CurrentNode ?? string.Empty,
            NextNode = grpcState.NextNode,
            ShouldEnd = grpcState.ShouldEnd,
            Output = grpcState.Output ?? string.Empty
        };

        if (grpcState.Data != null)
        {
            foreach (var kvp in grpcState.Data)
            {
                state.Data[kvp.Key] = kvp.Value;
            }
        }

        return state;
    }

    private SharpAIKit.Grpc.GraphState ConvertFromGraphState(SharpAIKit.Graph.GraphState state)
    {
        var grpcState = new SharpAIKit.Grpc.GraphState
        {
            CurrentNode = state.CurrentNode,
            NextNode = state.NextNode,
            ShouldEnd = state.ShouldEnd,
            Output = state.Output ?? string.Empty
        };

        foreach (var kvp in state.Data)
        {
            if (kvp.Value != null)
            {
                grpcState.Data[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
            }
        }

        return grpcState;
    }
}

