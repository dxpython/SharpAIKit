using Grpc.Core;
using SharpAIKit.Optimizer;
using SharpAIKit.LLM;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Optimizer operations
/// </summary>
public class OptimizerServiceImpl : OptimizerService.OptimizerServiceBase
{
    private readonly ILogger<OptimizerServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, DSPyOptimizer> _optimizers = new();

    public OptimizerServiceImpl(ILogger<OptimizerServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<CreateOptimizerResponse> CreateOptimizer(CreateOptimizerRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.OptimizerId))
            {
                return Task.FromResult(new CreateOptimizerResponse
                {
                    Success = false,
                    Error = "OptimizerId is required"
                });
            }

            if (string.IsNullOrEmpty(request.ApiKey))
            {
                return Task.FromResult(new CreateOptimizerResponse
                {
                    Success = false,
                    Error = "ApiKey is required"
                });
            }

            var llmClient = LLMClientFactory.Create(
                request.ApiKey,
                request.BaseUrl ?? "https://api.openai.com/v1",
                request.Model ?? "gpt-3.5-turbo",
                _logger);

            var optimizer = new DSPyOptimizer(llmClient)
            {
                MaxIterations = request.MaxIterations > 0 ? request.MaxIterations : 10
            };

            // Set metric function from expression (simplified)
            MetricFunction metricFunc = async (input, output, expectedOutput) =>
            {
                // Simple metric: exact match = 1.0, otherwise 0.0
                // In a full implementation, would evaluate the JavaScript-like expression
                return output.Equals(expectedOutput, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            };
            optimizer.SetMetric(metricFunc);

            _optimizers[request.OptimizerId] = optimizer;
            _logger.LogInformation("Optimizer created: {OptimizerId}", request.OptimizerId);

            return Task.FromResult(new CreateOptimizerResponse
            {
                Success = true,
                OptimizerId = request.OptimizerId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating optimizer: {OptimizerId}", request.OptimizerId);
            return Task.FromResult(new CreateOptimizerResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override async Task<OptimizeResponse> Optimize(OptimizeRequest request, ServerCallContext context)
    {
        try
        {
            if (!_optimizers.TryGetValue(request.OptimizerId, out var optimizer))
            {
                return new OptimizeResponse
                {
                    Success = false,
                    Error = $"Optimizer {request.OptimizerId} not found"
                };
            }

            // Add training examples
            foreach (var ex in request.Examples)
            {
                // Simplified - would parse input/output from example string
                // For now, use example as both input and expected output
                optimizer.AddExample(ex, ex);
            }

            var result = await optimizer.OptimizeAsync(request.InitialPrompt, context.CancellationToken);

            return new OptimizeResponse
            {
                Success = true,
                OptimizedPrompt = result.OptimizedPrompt,
                MetricScore = (float)result.BestScore,
                Iterations = result.Iterations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing prompt: {OptimizerId}", request.OptimizerId);
            return new OptimizeResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task OptimizeStream(OptimizeRequest request, IServerStreamWriter<OptimizeStreamChunk> responseStream, ServerCallContext context)
    {
        try
        {
            if (!_optimizers.TryGetValue(request.OptimizerId, out var optimizer))
            {
                await responseStream.WriteAsync(new OptimizeStreamChunk
                {
                    Error = $"Optimizer {request.OptimizerId} not found"
                });
                return;
            }

            // Add training examples
            foreach (var ex in request.Examples)
            {
                optimizer.AddExample(ex, ex);
            }

            // Stream optimization progress (simplified - would need to add streaming support to DSPyOptimizer)
            var result = await optimizer.OptimizeAsync(request.InitialPrompt, context.CancellationToken);

            // Send final result
            await responseStream.WriteAsync(new OptimizeStreamChunk
            {
                PromptCandidate = result.OptimizedPrompt,
                MetricScore = (float)result.BestScore,
                Iteration = result.Iterations,
                Done = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OptimizeStream: {OptimizerId}", request.OptimizerId);
            await responseStream.WriteAsync(new OptimizeStreamChunk
            {
                Error = ex.Message
            });
        }
    }
}
