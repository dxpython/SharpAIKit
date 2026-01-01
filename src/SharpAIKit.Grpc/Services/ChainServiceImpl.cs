using Grpc.Core;
using SharpAIKit.Chain;
using SharpAIKit.LLM;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Chain operations
/// </summary>
public class ChainServiceImpl : ChainService.ChainServiceBase
{
    private readonly ILogger<ChainServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, IChain> _chains = new();
    private readonly ConcurrentDictionary<string, ILLMClient> _llmClients = new();

    public ChainServiceImpl(ILogger<ChainServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<CreateChainResponse> CreateChain(CreateChainRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ChainId))
            {
                return Task.FromResult(new CreateChainResponse
                {
                    Success = false,
                    Error = "ChainId is required"
                });
            }

            IChain chain;
            switch (request.ChainType.ToLower())
            {
                case "llm":
                    if (string.IsNullOrEmpty(request.ApiKey))
                    {
                        return Task.FromResult(new CreateChainResponse
                        {
                            Success = false,
                            Error = "ApiKey is required for LLM chain"
                        });
                    }

                    var llmClient = LLMClientFactory.Create(
                        request.ApiKey,
                        request.BaseUrl ?? "https://api.openai.com/v1",
                        request.Model ?? "gpt-3.5-turbo",
                        _logger);
                    
                    _llmClients[request.ChainId] = llmClient;
                    chain = new LLMChain(llmClient, request.SystemPrompt);
                    break;

                case "lambda":
                    // Lambda chain - simple transformation
                    chain = new LambdaChain((SharpAIKit.Chain.ChainContext ctx) =>
                    {
                        // Simple pass-through for now
                        return Task.FromResult(ctx);
                    });
                    break;

                default:
                    return Task.FromResult(new CreateChainResponse
                    {
                        Success = false,
                        Error = $"Unknown chain type: {request.ChainType}"
                    });
            }

            _chains[request.ChainId] = chain;
            _logger.LogInformation("Chain created: {ChainId} ({Type})", request.ChainId, request.ChainType);

            return Task.FromResult(new CreateChainResponse
            {
                Success = true,
                ChainId = request.ChainId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chain: {ChainId}", request.ChainId);
            return Task.FromResult(new CreateChainResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override async Task<InvokeChainResponse> InvokeChain(InvokeChainRequest request, ServerCallContext context)
    {
        try
        {
            if (!_chains.TryGetValue(request.ChainId, out var chain))
            {
                return new InvokeChainResponse
                {
                    Success = false,
                    Error = $"Chain {request.ChainId} not found"
                };
            }

            var chainContext = ConvertToChainContext(request.Context ?? new SharpAIKit.Grpc.ChainContext());
            var result = await chain.InvokeAsync(chainContext, context.CancellationToken);

            return new InvokeChainResponse
            {
                Success = true,
                Context = ConvertFromChainContext(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking chain: {ChainId}", request.ChainId);
            return new InvokeChainResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task InvokeChainStream(InvokeChainRequest request, IServerStreamWriter<ChainStreamChunk> responseStream, ServerCallContext context)
    {
        try
        {
            if (!_chains.TryGetValue(request.ChainId, out var chain))
            {
                await responseStream.WriteAsync(new ChainStreamChunk
                {
                    Error = $"Chain {request.ChainId} not found"
                });
                return;
            }

            var chainContext = ConvertToChainContext(request.Context ?? new SharpAIKit.Grpc.ChainContext());
            await foreach (var chunk in chain.StreamAsync(chainContext, context.CancellationToken))
            {
                await responseStream.WriteAsync(new ChainStreamChunk
                {
                    Context = ConvertFromChainContext(chunk)
                });
            }

            await responseStream.WriteAsync(new ChainStreamChunk
            {
                Done = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InvokeChainStream: {ChainId}", request.ChainId);
            await responseStream.WriteAsync(new ChainStreamChunk
            {
                Error = ex.Message
            });
        }
    }

    public override Task<CreateChainResponse> PipeChains(PipeChainsRequest request, ServerCallContext context)
    {
        try
        {
            if (!_chains.TryGetValue(request.ChainId1, out var chain1))
            {
                return Task.FromResult(new CreateChainResponse
                {
                    Success = false,
                    Error = $"Chain {request.ChainId1} not found"
                });
            }

            if (!_chains.TryGetValue(request.ChainId2, out var chain2))
            {
                return Task.FromResult(new CreateChainResponse
                {
                    Success = false,
                    Error = $"Chain {request.ChainId2} not found"
                });
            }

            var pipedChain = chain1.Pipe(chain2);
            _chains[request.ResultChainId] = pipedChain;

            _logger.LogInformation("Chains piped: {Chain1} | {Chain2} -> {Result}", 
                request.ChainId1, request.ChainId2, request.ResultChainId);

            return Task.FromResult(new CreateChainResponse
            {
                Success = true,
                ChainId = request.ResultChainId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error piping chains");
            return Task.FromResult(new CreateChainResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<CreateChainResponse> ParallelChains(ParallelChainsRequest request, ServerCallContext context)
    {
        try
        {
            var chains = new List<IChain>();
            foreach (var chainId in request.ChainIds)
            {
                if (!_chains.TryGetValue(chainId, out var chain))
                {
                    return Task.FromResult(new CreateChainResponse
                    {
                        Success = false,
                        Error = $"Chain {chainId} not found"
                    });
                }
                chains.Add(chain);
            }

            var parallelChain = ChainExtensions.Parallel(chains.ToArray());
            _chains[request.ResultChainId] = parallelChain;

            _logger.LogInformation("Parallel chain created: {ResultChainId} from {Count} chains", 
                request.ResultChainId, chains.Count);

            return Task.FromResult(new CreateChainResponse
            {
                Success = true,
                ChainId = request.ResultChainId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parallel chain");
            return Task.FromResult(new CreateChainResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<CreateChainResponse> BranchChain(BranchChainRequest request, ServerCallContext context)
    {
        try
        {
            if (!_chains.TryGetValue(request.ChainId, out var chain))
            {
                return Task.FromResult(new CreateChainResponse
                {
                    Success = false,
                    Error = $"Chain {request.ChainId} not found"
                });
            }

            if (!_chains.TryGetValue(request.TrueChainId, out var trueChain))
            {
                return Task.FromResult(new CreateChainResponse
                {
                    Success = false,
                    Error = $"Chain {request.TrueChainId} not found"
                });
            }

            IChain? falseChain = null;
            if (!string.IsNullOrEmpty(request.FalseChainId))
            {
                if (!_chains.TryGetValue(request.FalseChainId, out falseChain))
                {
                    return Task.FromResult(new CreateChainResponse
                    {
                        Success = false,
                        Error = $"Chain {request.FalseChainId} not found"
                    });
                }
            }

            // Simple condition evaluation (can be enhanced)
            Func<SharpAIKit.Chain.ChainContext, bool> condition = ctx =>
            {
                // Evaluate JavaScript-like expression (simplified)
                // For now, just check if output contains certain keywords
                var output = ctx.Output.ToLower();
                return request.ConditionExpression.ToLower().Contains("error") 
                    ? output.Contains("error") 
                    : !output.Contains("error");
            };

            var branchChain = chain.Branch(condition, trueChain, falseChain);
            _chains[request.ResultChainId] = branchChain;

            _logger.LogInformation("Branch chain created: {ResultChainId}", request.ResultChainId);

            return Task.FromResult(new CreateChainResponse
            {
                Success = true,
                ChainId = request.ResultChainId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch chain");
            return Task.FromResult(new CreateChainResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    private SharpAIKit.Chain.ChainContext ConvertToChainContext(SharpAIKit.Grpc.ChainContext grpcContext)
    {
        if (grpcContext == null)
        {
            return new SharpAIKit.Chain.ChainContext();
        }

        var context = SharpAIKit.Chain.ChainContext.FromInput(grpcContext.Input ?? string.Empty);
        context.Output = grpcContext.Output ?? string.Empty;

        if (grpcContext.Data != null)
        {
            foreach (var kvp in grpcContext.Data)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        return context;
    }

    private SharpAIKit.Grpc.ChainContext ConvertFromChainContext(SharpAIKit.Chain.ChainContext context)
    {
        var grpcContext = new SharpAIKit.Grpc.ChainContext
        {
            Input = context.Input,
            Output = context.Output
        };

        // ChainContext uses internal dictionary, we'll just copy input/output
        // Additional data can be accessed via Get<T> but we don't have direct Keys access
        // For now, just pass input/output which are the main fields
        if (!string.IsNullOrEmpty(context.Input))
        {
            grpcContext.Data["input"] = context.Input;
        }
        if (!string.IsNullOrEmpty(context.Output))
        {
            grpcContext.Data["output"] = context.Output;
        }

        return grpcContext;
    }
}

