using Grpc.Core;
using SharpAIKit.Prompt;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Prompt operations
/// </summary>
public class PromptServiceImpl : PromptService.PromptServiceBase
{
    private readonly ILogger<PromptServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, PromptTemplate> _templates = new();
    private readonly ConcurrentDictionary<string, ChatTemplateWrapper> _chatTemplates = new();

    public PromptServiceImpl(ILogger<PromptServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<CreateTemplateResponse> CreateTemplate(CreateTemplateRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TemplateId))
            {
                return Task.FromResult(new CreateTemplateResponse
                {
                    Success = false,
                    Error = "TemplateId is required"
                });
            }

            var template = PromptTemplate.FromTemplate(request.Template);
            _templates[request.TemplateId] = template;

            _logger.LogInformation("Template created: {TemplateId}", request.TemplateId);

            return Task.FromResult(new CreateTemplateResponse
            {
                Success = true,
                TemplateId = request.TemplateId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template: {TemplateId}", request.TemplateId);
            return Task.FromResult(new CreateTemplateResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<FormatTemplateResponse> FormatTemplate(FormatTemplateRequest request, ServerCallContext context)
    {
        try
        {
            if (!_templates.TryGetValue(request.TemplateId, out var template))
            {
                return Task.FromResult(new FormatTemplateResponse
                {
                    Success = false,
                    Error = $"Template {request.TemplateId} not found"
                });
            }

            var variables = request.Variables?.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value) ?? new Dictionary<string, object?>();
            var formatted = template.Format(variables);

            return Task.FromResult(new FormatTemplateResponse
            {
                Success = true,
                Formatted = formatted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting template: {TemplateId}", request.TemplateId);
            return Task.FromResult(new FormatTemplateResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<CreateChatTemplateResponse> CreateChatTemplate(CreateChatTemplateRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TemplateId))
            {
                return Task.FromResult(new CreateChatTemplateResponse
                {
                    Success = false,
                    Error = "TemplateId is required"
                });
            }

            // ChatTemplate may not exist as a separate class
            // For now, store messages and variables for later formatting
            var messages = request.Messages.Select(m => new SharpAIKit.Common.ChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList();

            // Store as a simple wrapper (simplified implementation)
            // In a full implementation, ChatTemplate would be a proper class
            _chatTemplates[request.TemplateId] = new ChatTemplateWrapper { Messages = messages, Variables = request.Variables?.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value) ?? new Dictionary<string, object?>() };

            _logger.LogInformation("Chat template created: {TemplateId}", request.TemplateId);

            return Task.FromResult(new CreateChatTemplateResponse
            {
                Success = true,
                TemplateId = request.TemplateId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat template: {TemplateId}", request.TemplateId);
            return Task.FromResult(new CreateChatTemplateResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<FormatChatTemplateResponse> FormatChatTemplate(FormatChatTemplateRequest request, ServerCallContext context)
    {
        try
        {
            if (!_chatTemplates.TryGetValue(request.TemplateId, out var chatTemplate))
            {
                return Task.FromResult(new FormatChatTemplateResponse
                {
                    Error = $"Chat template {request.TemplateId} not found"
                });
            }

            var variables = request.Variables?.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value) ?? new Dictionary<string, object?>();
            // Merge template variables with request variables
            var allVariables = new Dictionary<string, object?>(chatTemplate.Variables);
            foreach (var kvp in variables)
            {
                allVariables[kvp.Key] = kvp.Value;
            }
            
            // Format messages with variables
            var messages = chatTemplate.Messages.Select(m =>
            {
                var content = m.Content;
                foreach (var kvp in allVariables)
                {
                    content = content.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
                }
                return new SharpAIKit.Common.ChatMessage
                {
                    Role = m.Role,
                    Content = content
                };
            }).ToList();

            var response = new FormatChatTemplateResponse();
            foreach (var msg in messages)
            {
                response.Messages.Add(new ChatMessage
                {
                    Role = msg.Role,
                    Content = msg.Content
                });
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting chat template: {TemplateId}", request.TemplateId);
            return Task.FromResult(new FormatChatTemplateResponse
            {
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Simple wrapper for chat template (simplified implementation)
    /// </summary>
    private class ChatTemplateWrapper
    {
        public List<SharpAIKit.Common.ChatMessage> Messages { get; set; } = new();
        public Dictionary<string, object?> Variables { get; set; } = new();
    }
}

