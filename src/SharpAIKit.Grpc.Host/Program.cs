using SharpAIKit.Grpc.Services;
using SharpAIKit.Skill;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTP/2 (required for gRPC)
// For local development, we use HTTP/2 without TLS (insecure)
builder.WebHost.ConfigureKestrel(options =>
{
    var port = int.Parse(Environment.GetEnvironmentVariable("SHARPAIKIT_GRPC_PORT") ?? "50051");
    options.ListenLocalhost(port, listenOptions =>
    {
        // Use HTTP/2 only for gRPC (allows insecure HTTP/2 for local development)
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

// Add gRPC services
builder.Services.AddGrpc();
builder.Services.AddLogging(b => b.AddConsole());

// Register all gRPC service implementations
builder.Services.AddSingleton<AgentServiceImpl>();
builder.Services.AddSingleton<ChainServiceImpl>();
builder.Services.AddSingleton<MemoryServiceImpl>();
builder.Services.AddSingleton<RAGServiceImpl>();
builder.Services.AddSingleton<GraphServiceImpl>();
builder.Services.AddSingleton<PromptServiceImpl>();
builder.Services.AddSingleton<OutputParserServiceImpl>();
builder.Services.AddSingleton<DocumentLoaderServiceImpl>();
builder.Services.AddSingleton<CodeInterpreterServiceImpl>();
builder.Services.AddSingleton<OptimizerServiceImpl>();
builder.Services.AddSingleton<ToolServiceImpl>();
builder.Services.AddSingleton<ObservabilityServiceImpl>();

// Register global skill resolver (optional - can be configured with skills)
var skillResolver = new DefaultSkillResolver();
builder.Services.AddSingleton<ISkillResolver>(skillResolver);

var app = builder.Build();

// Configure gRPC
app.MapGrpcService<AgentServiceImpl>();
app.MapGrpcService<ChainServiceImpl>();
app.MapGrpcService<MemoryServiceImpl>();
app.MapGrpcService<RAGServiceImpl>();
app.MapGrpcService<GraphServiceImpl>();
app.MapGrpcService<PromptServiceImpl>();
app.MapGrpcService<OutputParserServiceImpl>();
app.MapGrpcService<DocumentLoaderServiceImpl>();
app.MapGrpcService<CodeInterpreterServiceImpl>();
app.MapGrpcService<OptimizerServiceImpl>();
app.MapGrpcService<ToolServiceImpl>();
app.MapGrpcService<ObservabilityServiceImpl>();

// Health check endpoint (gRPC only, no HTTP endpoint for health)
// Health check is available via gRPC HealthCheck service

// Get port from environment or use default
var port = Environment.GetEnvironmentVariable("SHARPAIKIT_GRPC_PORT") ?? "50051";

app.Run();

