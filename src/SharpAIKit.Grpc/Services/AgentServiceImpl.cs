using Grpc.Core;
using SharpAIKit.Agent;
using SharpAIKit.LLM;
using SharpAIKit.Skill;
using SharpAIKit.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Agent operations
/// </summary>
public class AgentServiceImpl : AgentService.AgentServiceBase
{
    private readonly ILogger<AgentServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, AgentInstance> _agents = new();
    private readonly ISkillResolver? _globalSkillResolver;

    public AgentServiceImpl(ILogger<AgentServiceImpl> logger, ISkillResolver? globalSkillResolver = null)
    {
        _logger = logger;
        _globalSkillResolver = globalSkillResolver;
    }

    public override Task<CreateAgentResponse> CreateAgent(CreateAgentRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.AgentId))
            {
                return Task.FromResult(new CreateAgentResponse
                {
                    Success = false,
                    Error = "AgentId is required"
                });
            }

            if (string.IsNullOrEmpty(request.ApiKey))
            {
                return Task.FromResult(new CreateAgentResponse
                {
                    Success = false,
                    Error = "ApiKey is required"
                });
            }

            // Create LLM client
            var llmClient = LLMClientFactory.Create(
                request.ApiKey,
                request.BaseUrl ?? "https://api.openai.com/v1",
                request.Model ?? "gpt-3.5-turbo",
                _logger);

            // Create skill resolver if skills are specified
            ISkillResolver? skillResolver = null;
            if (request.SkillIds != null && request.SkillIds.Count > 0)
            {
                skillResolver = _globalSkillResolver ?? new DefaultSkillResolver();
                // Skills should be pre-registered in the skill resolver
                // For now, we'll use the provided skill resolver or create a new one
            }

            // Create enhanced agent
            var agentLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger<EnhancedAgent>();
            var agent = new EnhancedAgent(
                llmClient,
                skillResolver: skillResolver,
                logger: agentLogger);

            // Store agent instance
            _agents[request.AgentId] = new AgentInstance
            {
                Agent = agent,
                LlmClient = llmClient,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Agent created: {AgentId}", request.AgentId);

            return Task.FromResult(new CreateAgentResponse
            {
                Success = true,
                AgentId = request.AgentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent: {AgentId}", request.AgentId);
            return Task.FromResult(new CreateAgentResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override async Task<ExecuteResponse> Execute(ExecuteRequest request, ServerCallContext context)
    {
        try
        {
            if (!_agents.TryGetValue(request.AgentId, out var agentInstance))
            {
                return new ExecuteResponse
                {
                    Success = false,
                    Error = $"Agent {request.AgentId} not found",
                    ErrorCode = "AGENT_NOT_FOUND"
                };
            }

            var agent = agentInstance.Agent;

            // Convert context
            var strongContext = new StrongContext();
            if (request.Context != null)
            {
                foreach (var kvp in request.Context)
                {
                    strongContext[kvp.Key] = kvp.Value;
                }
            }

            // Execute agent
            var result = await agent.RunAsync(request.Task, context.CancellationToken);

            // Build response
            var response = new ExecuteResponse
            {
                Success = result.Success,
                Output = result.Answer ?? string.Empty
            };

            // Add steps from StepResults
            if (result.StepResults != null)
            {
                int stepNumber = 1;
                foreach (var stepResult in result.StepResults)
                {
                    var grpcStep = new ExecutionStep
                    {
                        StepNumber = stepNumber++,
                        Type = "execution",
                        Thought = stepResult.Step?.Description ?? string.Empty,
                        Action = stepResult.Step?.ToolName ?? stepResult.Step?.Description ?? string.Empty,
                        Observation = stepResult.Result ?? stepResult.Error ?? string.Empty,
                        ToolName = stepResult.Step?.ToolName ?? string.Empty
                    };

                    if (stepResult.Step?.ToolArguments != null)
                    {
                        foreach (var arg in stepResult.Step.ToolArguments)
                        {
                            grpcStep.ToolArgs[arg.Key] = arg.Value?.ToString() ?? string.Empty;
                        }
                    }

                    response.Steps.Add(grpcStep);
                }
            }

            // Add skill resolution info
            if (agent.LastSkillResolution != null)
            {
                response.SkillResolution = ConvertSkillResolution(agent.LastSkillResolution);
                
                // Add denied tools from constraints
                if (agent.LastSkillResolution.FinalConstraints.ForbiddenTools != null)
                {
                    foreach (var tool in agent.LastSkillResolution.FinalConstraints.ForbiddenTools)
                    {
                        response.DeniedTools.Add(tool);
                        var reason = agent.LastSkillResolution.GetToolDenialReason(tool);
                        if (!string.IsNullOrEmpty(reason))
                        {
                            response.SkillResolution.ToolDenialReasons[tool] = reason;
                        }
                        else
                        {
                            response.SkillResolution.ToolDenialReasons[tool] = $"Tool '{tool}' is forbidden by Skill constraints";
                        }
                    }
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent: {AgentId}", request.AgentId);
            return new ExecuteResponse
            {
                Success = false,
                Error = ex.Message,
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }

    public override async Task ExecuteStream(ExecuteRequest request, IServerStreamWriter<ExecuteStreamChunk> responseStream, ServerCallContext context)
    {
        try
        {
            if (!_agents.TryGetValue(request.AgentId, out var agentInstance))
            {
                await responseStream.WriteAsync(new ExecuteStreamChunk
                {
                    Error = $"Agent {request.AgentId} not found"
                });
                return;
            }

            var agent = agentInstance.Agent;

            // Convert context
            var strongContext = new StrongContext();
            if (request.Context != null)
            {
                foreach (var kvp in request.Context)
                {
                    strongContext[kvp.Key] = kvp.Value;
                }
            }

            // Send skill resolution first if available
            if (agent.LastSkillResolution != null)
            {
                await responseStream.WriteAsync(new ExecuteStreamChunk
                {
                    SkillResolution = ConvertSkillResolution(agent.LastSkillResolution)
                });
            }

            // Execute agent (for streaming, we'll send steps as they complete)
            var result = await agent.RunAsync(request.Task, context.CancellationToken);

            // Send steps from StepResults
            if (result.StepResults != null)
            {
                int stepNumber = 1;
                foreach (var stepResult in result.StepResults)
                {
                    var grpcStep = new ExecutionStep
                    {
                        StepNumber = stepNumber++,
                        Type = "execution",
                        Thought = stepResult.Step?.Description ?? string.Empty,
                        Action = stepResult.Step?.ToolName ?? stepResult.Step?.Description ?? string.Empty,
                        Observation = stepResult.Result ?? stepResult.Error ?? string.Empty,
                        ToolName = stepResult.Step?.ToolName ?? string.Empty
                    };

                    if (stepResult.Step?.ToolArguments != null)
                    {
                        foreach (var arg in stepResult.Step.ToolArguments)
                        {
                            grpcStep.ToolArgs[arg.Key] = arg.Value?.ToString() ?? string.Empty;
                        }
                    }

                    await responseStream.WriteAsync(new ExecuteStreamChunk
                    {
                        Step = grpcStep
                    });
                }
            }

            // Send final output
            if (!string.IsNullOrEmpty(result.Answer))
            {
                await responseStream.WriteAsync(new ExecuteStreamChunk
                {
                    TextChunk = result.Answer
                });
            }

            // Send done
            await responseStream.WriteAsync(new ExecuteStreamChunk
            {
                Done = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteStream: {AgentId}", request.AgentId);
            await responseStream.WriteAsync(new ExecuteStreamChunk
            {
                Error = ex.Message
            });
        }
    }

    public override Task<ListSkillsResponse> ListAvailableSkills(ListSkillsRequest request, ServerCallContext context)
    {
        try
        {
            var response = new ListSkillsResponse();
            var skillResolver = _globalSkillResolver ?? new DefaultSkillResolver();

            var skills = skillResolver.GetAllSkills();
            foreach (var skill in skills)
            {
                response.Skills.Add(new SkillInfo
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description,
                    Version = skill.Version ?? string.Empty,
                    Priority = skill.Priority,
                    Scope = skill.Scope ?? string.Empty
                });
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing skills");
            return Task.FromResult(new ListSkillsResponse());
        }
    }

    public override Task<GetSkillResolutionResponse> GetLastSkillResolution(GetSkillResolutionRequest request, ServerCallContext context)
    {
        try
        {
            if (!_agents.TryGetValue(request.AgentId, out var agentInstance))
            {
                return Task.FromResult(new GetSkillResolutionResponse
                {
                    Success = false,
                    Error = $"Agent {request.AgentId} not found"
                });
            }

            var agent = agentInstance.Agent;
            if (agent.LastSkillResolution == null)
            {
                return Task.FromResult(new GetSkillResolutionResponse
                {
                    Success = false,
                    Error = "No skill resolution available"
                });
            }

            return Task.FromResult(new GetSkillResolutionResponse
            {
                Success = true,
                SkillResolution = ConvertSkillResolution(agent.LastSkillResolution)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill resolution: {AgentId}", request.AgentId);
            return Task.FromResult(new GetSkillResolutionResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HealthCheckResponse
        {
            Healthy = true,
            Version = "0.3.0"
        });
    }

    private SkillResolutionInfo ConvertSkillResolution(SkillResolutionResult result)
    {
        var info = new SkillResolutionInfo();
        
        if (result.ActivatedSkills != null)
        {
            foreach (var skill in result.ActivatedSkills)
            {
                info.ActivatedSkillIds.Add(skill.Metadata.Id);
            }
        }

        if (result.DecisionReasons != null)
        {
            info.DecisionReasons.AddRange(result.DecisionReasons);
        }

        if (result.FinalConstraints != null)
        {
            info.Constraints = new SkillConstraintsInfo
            {
                MaxSteps = result.FinalConstraints.MaxSteps ?? 0,
                MaxExecutionTimeMs = (long)(result.FinalConstraints.MaxExecutionTime?.TotalMilliseconds ?? 0)
            };

            if (result.FinalConstraints.AllowedTools != null)
            {
                info.Constraints.AllowedTools.AddRange(result.FinalConstraints.AllowedTools);
            }

            if (result.FinalConstraints.ForbiddenTools != null)
            {
                info.Constraints.ForbiddenTools.AddRange(result.FinalConstraints.ForbiddenTools);
            }
        }

        return info;
    }

    private class AgentInstance
    {
        public EnhancedAgent Agent { get; set; } = null!;
        public ILLMClient LlmClient { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}

