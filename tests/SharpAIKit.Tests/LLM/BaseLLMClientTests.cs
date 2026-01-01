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
/// Tests for BaseLLMClient: base functionality and error handling
/// </summary>
public class BaseLLMClientTests
{
    private class TestLLMClient : BaseLLMClient
    {
        public TestLLMClient(HttpClient httpClient, LLMOptions options, ILogger? logger = null)
            : base(httpClient, options, logger)
        {
        }

        public override Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Test response");
        }

        public override Task<string> ChatAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Test response");
        }

        public override Task<string> ChatAsync(IEnumerable<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Test response");
        }

        public override async IAsyncEnumerable<string> ChatStreamAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "Test";
            await Task.CompletedTask;
        }

        public override async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "Test";
            await Task.CompletedTask;
        }

        public override async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, ChatOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "Test";
            await Task.CompletedTask;
        }

        public override Task<float[]> EmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new float[] { 0.1f, 0.2f, 0.3f });
        }
    }

    [Fact]
    public async Task EmbeddingBatchAsync_ProcessesMultipleTexts()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = new LLMOptions { ApiKey = "test", Model = "test" };
        var client = new TestLLMClient(httpClient, options);

        // Act
        var texts = new[] { "text1", "text2", "text3" };
        var results = await client.EmbeddingBatchAsync(texts);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.Equal(3, r.Length));
    }

    [Fact]
    public void Constructor_SetsHttpClientTimeout()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = new LLMOptions
        {
            ApiKey = "test",
            Model = "test",
            TimeoutSeconds = 60
        };

        // Act
        var client = new TestLLMClient(httpClient, options);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(60), httpClient.Timeout);
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = new LLMOptions { ApiKey = "test", Model = "test" };
        var client = new TestLLMClient(httpClient, options);

        // Act
        client.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }
}

