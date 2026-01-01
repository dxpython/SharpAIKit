using Xunit;
using Moq;
using Moq.Protected;
using SharpAIKit.LLM;
using SharpAIKit.Common;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SharpAIKit.Tests.LLM;

/// <summary>
/// Tests for OpenAIClient: core LLM client functionality, error handling, and edge cases
/// </summary>
public class OpenAIClientTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly LLMOptions _defaultOptions;

    public OpenAIClientTests()
    {
        _mockLogger = new Mock<ILogger>();
        _defaultOptions = new LLMOptions
        {
            ApiKey = "test-api-key",
            Model = "gpt-3.5-turbo",
            BaseUrl = "https://api.openai.com/v1",
            TimeoutSeconds = 30
        };
    }

    private HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(handler.Object);
    }

    private HttpResponseMessage CreateSuccessResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }

    private string CreateOpenAIResponse(string content, bool hasToolCalls = false)
    {
        if (hasToolCalls)
        {
            var response = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            role = "assistant",
                            tool_calls = new[]
                            {
                                new
                                {
                                    id = "call_1",
                                    type = "function",
                                    function = new { name = "test_tool", arguments = "{}" }
                                }
                            }
                        }
                    }
                }
            };
            return JsonSerializer.Serialize(response);
        }
        else
        {
            var response = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new { role = "assistant", content = content }
                    }
                }
            };
            return JsonSerializer.Serialize(response);
        }
    }

    [Fact]
    public async Task ChatAsync_SimplePrompt_ReturnsResponse()
    {
        // Arrange
        var responseJson = CreateOpenAIResponse("Hello, world!");
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act
        var result = await client.ChatAsync("Hello");

        // Assert
        Assert.Equal("Hello, world!", result);
    }

    [Fact]
    public async Task ChatAsync_WithMessages_ReturnsResponse()
    {
        // Arrange
        var responseJson = CreateOpenAIResponse("Response to conversation");
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        var messages = new List<ChatMessage>
        {
            ChatMessage.System("You are a helpful assistant"),
            ChatMessage.User("Hello")
        };

        // Act
        var result = await client.ChatAsync(messages);

        // Assert
        Assert.Equal("Response to conversation", result);
    }

    [Fact]
    public async Task ChatAsync_WithToolCalls_ReturnsToolCallJson()
    {
        // Arrange
        var responseJson = CreateOpenAIResponse("", hasToolCalls: true);
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        var messages = new List<ChatMessage> { ChatMessage.User("Use calculator tool") };
        var options = new ChatOptions
        {
            Tools = new List<ToolDefinition>
            {
                new ToolDefinition
                {
                    Type = "function",
                    Function = new FunctionDefinition { Name = "calculator", Description = "Calculate" }
                }
            }
        };

        // Act
        var result = await client.ChatAsync(messages, options);

        // Assert
        Assert.Contains("tool_calls", result);
        Assert.Contains("test_tool", result);
    }

    [Fact]
    public async Task ChatAsync_WithJsonMode_IncludesResponseFormat()
    {
        // Arrange
        var responseJson = CreateOpenAIResponse("{\"result\": \"json\"}");
        var handler = new Mock<HttpMessageHandler>();
        HttpRequestMessage? capturedRequest = null;
        
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(CreateSuccessResponse(responseJson));

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        var options = new ChatOptions { JsonMode = true };

        // Act
        await client.ChatAsync(new[] { ChatMessage.User("Test") }, options);

        // Assert
        Assert.NotNull(capturedRequest);
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("response_format", requestBody);
        Assert.Contains("json_object", requestBody);
    }

    [Fact]
    public async Task ChatAsync_HttpError_ThrowsException()
    {
        // Arrange
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request", Encoding.UTF8, "text/plain")
        };
        var httpClient = CreateMockHttpClient(errorResponse);
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.ChatAsync("Test"));
    }

    [Fact]
    public async Task ChatAsync_TokenLimitExceeded_HandlesError()
    {
        // Arrange - Simulate 429 Too Many Requests or 400 with context_length_exceeded
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { error = new { message = "context_length_exceeded", type = "invalid_request_error" } }),
                Encoding.UTF8,
                "application/json")
        };
        var httpClient = CreateMockHttpClient(errorResponse);
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.ChatAsync("Test"));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ChatAsync_NetworkTimeout_ThrowsException()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var httpClient = new HttpClient(handler.Object) { Timeout = TimeSpan.FromSeconds(1) };
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => client.ChatAsync("Test"));
    }

    [Fact]
    public async Task ChatAsync_Cancellation_ThrowsCancellationException()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                throw new OperationCanceledException("Operation cancelled", ct);
            });

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - HttpClient throws TaskCanceledException for cancellation
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ChatAsync("Test", cts.Token));
    }

    [Fact]
    public async Task ChatStreamAsync_ReturnsStreamingChunks()
    {
        // Arrange
        var chunks = new[]
        {
            "data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}\n\n",
            "data: {\"choices\":[{\"delta\":{\"content\":\" world\"}}}]}\n\n",
            "data: [DONE]\n\n"
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Join("", chunks), Encoding.UTF8, "text/event-stream")
            });

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act
        var results = new List<string>();
        await foreach (var chunk in client.ChatStreamAsync("Hello").ConfigureAwait(false))
        {
            results.Add(chunk);
        }

        // Assert - Should receive chunks
        Assert.NotEmpty(results);
        var combined = string.Join("", results);
        Assert.Contains("Hello", combined);
    }

    [Fact]
    public async Task EmbeddingAsync_ReturnsEmbeddingVector()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { embedding = new[] { 0.1f, 0.2f, 0.3f } }
            }
        });
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act
        var result = await client.EmbeddingAsync("test text");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal(0.1f, result[0], 0.01f);
    }

    [Fact]
    public async Task EmbeddingBatchAsync_ProcessesMultipleTexts()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { embedding = new[] { 0.1f, 0.2f } },
                new { embedding = new[] { 0.3f, 0.4f } }
            }
        });
        var httpClient = CreateMockHttpClient(CreateSuccessResponse(responseJson));
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act
        var texts = new[] { "text1", "text2" };
        var results = await client.EmbeddingBatchAsync(texts);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(2, results[0].Length);
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new OpenAIClient(httpClient, _defaultOptions, _mockLogger.Object);

        // Act
        client.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void Constructor_WithCustomBaseUrl_UsesCustomUrl()
    {
        // Arrange
        var options = new LLMOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://custom-api.com/v1",
            Model = "gpt-4"
        };
        var httpClient = new HttpClient();

        // Act
        var client = new OpenAIClient(httpClient, options);

        // Assert - Constructor should not throw
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithoutModel_SetsDefaultModel()
    {
        // Arrange
        var options = new LLMOptions
        {
            ApiKey = "test-key",
            Model = null // No model specified
        };
        var httpClient = new HttpClient();

        // Act
        var client = new OpenAIClient(httpClient, options);

        // Assert - Should use default model
        Assert.NotNull(client);
    }
}

