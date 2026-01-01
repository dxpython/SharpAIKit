using Grpc.Core;
using SharpAIKit.CodeInterpreter;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for CodeInterpreter operations
/// </summary>
public class CodeInterpreterServiceImpl : CodeInterpreterService.CodeInterpreterServiceBase
{
    private readonly ILogger<CodeInterpreterServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, ICodeInterpreter> _interpreters = new();

    public CodeInterpreterServiceImpl(ILogger<CodeInterpreterServiceImpl> logger)
    {
        _logger = logger;
    }

    public override async Task<ExecuteCodeResponse> ExecuteCode(ExecuteCodeRequest request, ServerCallContext context)
    {
        try
        {
            var interpreter = GetOrCreateInterpreter("default");
            var timeout = request.TimeoutMs > 0 ? TimeSpan.FromMilliseconds(request.TimeoutMs) : TimeSpan.FromSeconds(30);

            using var cts = new CancellationTokenSource(timeout);
            var result = await interpreter.ExecuteAsync(request.Code, cts.Token);

            return new ExecuteCodeResponse
            {
                Success = result.Success,
                Output = result.Output,
                Error = result.Error,
                Stdout = result.Output, // Simplified
                Stderr = result.Error ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing code");
            return new ExecuteCodeResponse
            {
                Success = false,
                Error = ex.Message,
                Stderr = ex.Message
            };
        }
    }

    public override async Task<ExecuteCodeTypedResponse> ExecuteCodeTyped(ExecuteCodeTypedRequest request, ServerCallContext context)
    {
        try
        {
            var interpreter = GetOrCreateInterpreter("default");
            var timeout = request.TimeoutMs > 0 ? TimeSpan.FromMilliseconds(request.TimeoutMs) : TimeSpan.FromSeconds(30);

            using var cts = new CancellationTokenSource(timeout);

            // Execute with typed return
            var result = await interpreter.ExecuteAsync(request.Code, cts.Token);
            
            // Try to parse as the requested type (simplified)
            string valueJson = "null";
            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                // For now, just return the output as JSON string
                // Full implementation would parse based on type
                valueJson = System.Text.Json.JsonSerializer.Serialize(result.Output);
            }

            return new ExecuteCodeTypedResponse
            {
                Success = result.Success,
                ValueJson = valueJson,
                Error = result.Error,
                Stdout = result.Output,
                Stderr = result.Error ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing typed code");
            return new ExecuteCodeTypedResponse
            {
                Success = false,
                Error = ex.Message,
                Stderr = ex.Message
            };
        }
    }

    private ICodeInterpreter GetOrCreateInterpreter(string id)
    {
        return _interpreters.GetOrAdd(id, _ => new RoslynCodeInterpreter());
    }
}

