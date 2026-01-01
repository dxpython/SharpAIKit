using Xunit;
using SharpAIKit.LLM;
using SharpAIKit.Common;

namespace SharpAIKit.Tests.LLM;

/// <summary>
/// Tests for LLMClientFactory: client creation, configuration, and error handling
/// </summary>
public class LLMClientFactoryTests
{
    [Fact]
    public void Create_OpenAI_CreatesClient()
    {
        // Arrange
        var options = new LLMOptions
        {
            ApiKey = "test-key",
            Model = "gpt-3.5-turbo",
            BaseUrl = "https://api.openai.com/v1"
        };

        // Act
        var client = LLMClientFactory.Create(LLMProviderType.OpenAI, options);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenAIClient>(client);
    }

    [Fact]
    public void Create_Ollama_CreatesClient()
    {
        // Arrange
        var options = new LLMOptions
        {
            ApiKey = "",
            Model = "llama2",
            BaseUrl = "http://localhost:11434"
        };

        // Act
        var client = LLMClientFactory.Create(LLMProviderType.Ollama, options);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OllamaClient>(client);
    }

    [Fact]
    public void Create_InvalidProvider_ThrowsException()
    {
        // Arrange
        var options = new LLMOptions
        {
            ApiKey = "test-key",
            Model = "test-model"
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LLMClientFactory.Create((LLMProviderType)999, options));
    }

    [Fact]
    public void Create_WithApiKeyAndUrl_CreatesOpenAIClient()
    {
        // Act
        var client = LLMClientFactory.Create(
            "test-api-key",
            "https://api.openai.com/v1",
            "gpt-4");

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenAIClient>(client);
    }

    [Fact]
    public void Create_WithCustomBaseUrl_UsesCustomUrl()
    {
        // Act
        var client = LLMClientFactory.Create(
            "test-key",
            "https://custom-api.com/v1",
            "custom-model");

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenAIClient>(client);
    }

    [Fact]
    public void CreateDeepSeek_CreatesClient()
    {
        // Act
        var client = LLMClientFactory.CreateDeepSeek("test-key");

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenAIClient>(client);
    }

    [Fact]
    public void CreateQwen_CreatesClient()
    {
        // Act
        var client = LLMClientFactory.CreateQwen("test-key");

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenAIClient>(client);
    }
}

