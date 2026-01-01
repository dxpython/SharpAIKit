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
/// Tests for error handling: Token limits, rate limits, network errors, and retry logic
/// </summary>
public class ErrorHandlingTests
{
    private readonly LLMOptions _defaultOptions;

    public ErrorHandlingTests()
    {
        _defaultOptions = new LLMOptions
        {
            ApiKey = "test-api-key",
            Model = "gpt-3.5-turbo",
            BaseUrl = "https://api.openai.com/v1",
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
    }

    private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string errorMessage, string errorType = "invalid_request_error")
    {
        var errorBody = JsonSerializer.Serialize(new
        {
            error = new
            {
                message = errorMessage,
                type = errorType,
                code = statusCode.ToString()
            }
        });

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorBody, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task ChatAsync_ContextLengthExceeded_ThrowsException()
    {
        // Arrange - Simulate context_length_exceeded error
        var errorResponse = CreateErrorResponse(
            HttpStatusCode.BadRequest,
            "This model's maximum context length is 4097 tokens. However, your messages resulted in 5000 tokens.",
            "invalid_request_error");

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => 
            client.ChatAsync("Very long prompt that exceeds context..."));

        Assert.NotNull(exception);
        // The error should be properly propagated
    }

    [Fact]
    public async Task ChatAsync_RateLimitExceeded_ThrowsException()
    {
        // Arrange - Simulate rate limit error (429)
        var errorResponse = CreateErrorResponse(
            HttpStatusCode.TooManyRequests,
            "Rate limit exceeded",
            "rate_limit_error");

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => 
            client.ChatAsync("Test"));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ChatAsync_InvalidApiKey_ThrowsException()
    {
        // Arrange - Simulate 401 Unauthorized
        var errorResponse = CreateErrorResponse(
            HttpStatusCode.Unauthorized,
            "Incorrect API key provided",
            "invalid_request_error");

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => 
            client.ChatAsync("Test"));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ChatAsync_ServerError_ThrowsException()
    {
        // Arrange - Simulate 500 Internal Server Error
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => 
            client.ChatAsync("Test"));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ChatAsync_NetworkError_ThrowsException()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.ChatAsync("Test"));
    }

    [Fact]
    public async Task ChatAsync_InvalidJsonResponse_ThrowsException()
    {
        // Arrange - Invalid JSON response
        var invalidResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Invalid JSON {", Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(invalidResponse);

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert - Should throw JsonException when parsing
        await Assert.ThrowsAnyAsync<JsonException>(() => client.ChatAsync("Test"));
    }

    [Fact]
    public async Task ChatAsync_EmptyResponse_HandlesGracefully()
    {
        // Arrange - Empty choices array (invalid response)
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = Array.Empty<object>()
        });

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert - Should throw when trying to access choices[0]
        await Assert.ThrowsAnyAsync<Exception>(() => client.ChatAsync("Test"));
    }

    [Fact]
    public async Task ChatAsync_MissingContent_ReturnsEmptyString()
    {
        // Arrange - Response with null content
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new { role = "assistant", content = (string?)null }
                }
            }
        });

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act
        var result = await client.ChatAsync("Test");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task EmbeddingAsync_InvalidResponse_ThrowsException()
    {
        // Arrange - Invalid embedding response
        var invalidResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Invalid JSON {", Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(invalidResponse);

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert
        await Assert.ThrowsAnyAsync<JsonException>(() => client.EmbeddingAsync("test"));
    }

    [Fact]
    public async Task ChatStreamAsync_StreamError_HandlesGracefully()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Stream error"));

        var httpClient = new HttpClient(handler.Object);
        var client = new OpenAIClient(httpClient, _defaultOptions);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var chunk in client.ChatStreamAsync("Test"))
            {
                // Should not reach here
            }
        });
    }
}

