using Grpc.Core;
using SharpAIKit.Memory;
using SharpAIKit.LLM;
using SharpAIKit.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Memory operations
/// </summary>
public class MemoryServiceImpl : MemoryService.MemoryServiceBase
{
    private readonly ILogger<MemoryServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, IMemory> _memories = new();
    private readonly ConcurrentDictionary<string, ILLMClient> _llmClients = new();

    public MemoryServiceImpl(ILogger<MemoryServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<CreateMemoryResponse> CreateMemory(CreateMemoryRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.MemoryId))
            {
                return Task.FromResult(new CreateMemoryResponse
                {
                    Success = false,
                    Error = "MemoryId is required"
                });
            }

            IMemory memory;
            switch (request.MemoryType.ToLower())
            {
                case "buffer":
                    var maxMessages = request.Options.TryGetValue("MaxMessages", out var maxMsgStr) 
                        && int.TryParse(maxMsgStr, out var maxMsg) ? maxMsg : 20;
                    memory = new BufferMemory { MaxMessages = maxMessages };
                    break;

                case "window":
                    var windowSize = request.Options.TryGetValue("WindowSize", out var winSizeStr) 
                        && int.TryParse(winSizeStr, out var winSize) ? winSize : 5;
                    memory = new WindowBufferMemory { WindowSize = windowSize };
                    break;

                case "vector":
                    if (string.IsNullOrEmpty(request.ApiKey))
                    {
                        return Task.FromResult(new CreateMemoryResponse
                        {
                            Success = false,
                            Error = "ApiKey is required for vector memory"
                        });
                    }

                    var llmClient = LLMClientFactory.Create(
                        request.ApiKey,
                        request.BaseUrl ?? "https://api.openai.com/v1",
                        request.Model ?? "gpt-3.5-turbo",
                        _logger);
                    
                    _llmClients[request.MemoryId] = llmClient;

                    var topK = 5;
                    if (request.Options.TryGetValue("TopK", out var topKStr) && int.TryParse(topKStr, out var topKVal))
                    {
                        topK = topKVal;
                    }
                    memory = new VectorMemory(llmClient) { TopK = topK };
                    break;

                case "summary":
                    if (string.IsNullOrEmpty(request.ApiKey))
                    {
                        return Task.FromResult(new CreateMemoryResponse
                        {
                            Success = false,
                            Error = "ApiKey is required for summary memory"
                        });
                    }

                    var summaryLlmClient = LLMClientFactory.Create(
                        request.ApiKey,
                        request.BaseUrl ?? "https://api.openai.com/v1",
                        request.Model ?? "gpt-3.5-turbo",
                        _logger);
                    
                    _llmClients[request.MemoryId] = summaryLlmClient;

                    var recentCount = request.Options.TryGetValue("RecentMessagesCount", out var recentStr) 
                        && int.TryParse(recentStr, out var recentVal) ? recentVal : 6;
                    memory = new SummaryMemory(summaryLlmClient) { RecentMessagesCount = recentCount };
                    break;

                case "entity":
                    if (string.IsNullOrEmpty(request.ApiKey))
                    {
                        return Task.FromResult(new CreateMemoryResponse
                        {
                            Success = false,
                            Error = "ApiKey is required for entity memory"
                        });
                    }

                    var entityLlmClient = LLMClientFactory.Create(
                        request.ApiKey,
                        request.BaseUrl ?? "https://api.openai.com/v1",
                        request.Model ?? "gpt-3.5-turbo",
                        _logger);
                    
                    _llmClients[request.MemoryId] = entityLlmClient;
                    memory = new EntityMemory(entityLlmClient);
                    break;

                default:
                    return Task.FromResult(new CreateMemoryResponse
                    {
                        Success = false,
                        Error = $"Unknown memory type: {request.MemoryType}"
                    });
            }

            _memories[request.MemoryId] = memory;
            _logger.LogInformation("Memory created: {MemoryId} ({Type})", request.MemoryId, request.MemoryType);

            return Task.FromResult(new CreateMemoryResponse
            {
                Success = true,
                MemoryId = request.MemoryId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating memory: {MemoryId}", request.MemoryId);
            return Task.FromResult(new CreateMemoryResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override async Task<AddMessageResponse> AddMessage(AddMessageRequest request, ServerCallContext context)
    {
        try
        {
            if (!_memories.TryGetValue(request.MemoryId, out var memory))
            {
                return new AddMessageResponse
                {
                    Success = false,
                    Error = $"Memory {request.MemoryId} not found"
                };
            }

            var message = new SharpAIKit.Common.ChatMessage
            {
                Role = request.Message.Role,
                Content = request.Message.Content
            };

            await memory.AddMessageAsync(message);

            return new AddMessageResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to memory: {MemoryId}", request.MemoryId);
            return new AddMessageResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<AddExchangeResponse> AddExchange(AddExchangeRequest request, ServerCallContext context)
    {
        try
        {
            if (!_memories.TryGetValue(request.MemoryId, out var memory))
            {
                return new AddExchangeResponse
                {
                    Success = false,
                    Error = $"Memory {request.MemoryId} not found"
                };
            }

            await memory.AddExchangeAsync(request.UserMessage, request.AssistantMessage);

            return new AddExchangeResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding exchange to memory: {MemoryId}", request.MemoryId);
            return new AddExchangeResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<GetMessagesResponse> GetMessages(GetMessagesRequest request, ServerCallContext context)
    {
        try
        {
            if (!_memories.TryGetValue(request.MemoryId, out var memory))
            {
                return new GetMessagesResponse
                {
                    Success = false,
                    Error = $"Memory {request.MemoryId} not found"
                };
            }

            var messages = await memory.GetMessagesAsync(request.Query);

            var response = new GetMessagesResponse { Success = true };
            foreach (var msg in messages)
            {
                response.Messages.Add(new SharpAIKit.Grpc.ChatMessage
                {
                    Role = msg.Role,
                    Content = msg.Content
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages from memory: {MemoryId}", request.MemoryId);
            return new GetMessagesResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<GetContextStringResponse> GetContextString(GetContextStringRequest request, ServerCallContext context)
    {
        try
        {
            if (!_memories.TryGetValue(request.MemoryId, out var memory))
            {
                return new GetContextStringResponse
                {
                    Success = false,
                    Error = $"Memory {request.MemoryId} not found"
                };
            }

            var contextString = await memory.GetContextStringAsync(request.Query);

            return new GetContextStringResponse
            {
                Success = true,
                ContextString = contextString
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting context string from memory: {MemoryId}", request.MemoryId);
            return new GetContextStringResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<ClearMemoryResponse> ClearMemory(ClearMemoryRequest request, ServerCallContext context)
    {
        try
        {
            if (!_memories.TryGetValue(request.MemoryId, out var memory))
            {
                return new ClearMemoryResponse
                {
                    Success = false,
                    Error = $"Memory {request.MemoryId} not found"
                };
            }

            await memory.ClearAsync();

            return new ClearMemoryResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing memory: {MemoryId}", request.MemoryId);
            return new ClearMemoryResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<GetCountResponse> GetCount(GetCountRequest request, ServerCallContext context)
    {
        try
        {
            if (!_memories.TryGetValue(request.MemoryId, out var memory))
            {
                return new GetCountResponse
                {
                    Success = false,
                    Error = $"Memory {request.MemoryId} not found"
                };
            }

            var count = await memory.GetCountAsync();

            return new GetCountResponse
            {
                Success = true,
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting count from memory: {MemoryId}", request.MemoryId);
            return new GetCountResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

