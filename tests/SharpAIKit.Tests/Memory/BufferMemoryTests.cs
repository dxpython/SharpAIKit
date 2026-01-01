using Xunit;
using SharpAIKit.Memory;
using SharpAIKit.Common;

namespace SharpAIKit.Tests.Memory;

/// <summary>
/// Tests for BufferMemory: basic memory operations, trimming, and edge cases
/// </summary>
public class BufferMemoryTests
{
    [Fact]
    public async Task AddMessageAsync_AddsMessage()
    {
        // Arrange
        var memory = new BufferMemory();
        var message = ChatMessage.User("Hello");

        // Act
        await memory.AddMessageAsync(message);

        // Assert
        var count = await memory.GetCountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddExchangeAsync_AddsUserAndAssistant()
    {
        // Arrange
        var memory = new BufferMemory();

        // Act
        await memory.AddExchangeAsync("Hello", "Hi there!");

        // Assert
        var count = await memory.GetCountAsync();
        Assert.Equal(2, count); // User + Assistant

        var messages = await memory.GetMessagesAsync();
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("assistant", messages[1].Role);
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsAllMessages()
    {
        // Arrange
        var memory = new BufferMemory();
        await memory.AddMessageAsync(ChatMessage.User("Message 1"));
        await memory.AddMessageAsync(ChatMessage.Assistant("Response 1"));
        await memory.AddMessageAsync(ChatMessage.User("Message 2"));

        // Act
        var messages = await memory.GetMessagesAsync();

        // Assert
        Assert.Equal(3, messages.Count);
        Assert.Equal("Message 1", messages[0].Content);
        Assert.Equal("Response 1", messages[1].Content);
    }

    [Fact]
    public async Task GetContextStringAsync_FormatsMessages()
    {
        // Arrange
        var memory = new BufferMemory
        {
            HumanPrefix = "User",
            AiPrefix = "Assistant"
        };
        await memory.AddExchangeAsync("Hello", "Hi!");

        // Act
        var context = await memory.GetContextStringAsync();

        // Assert
        Assert.Contains("User: Hello", context);
        Assert.Contains("Assistant: Hi!", context);
    }

    [Fact]
    public async Task MaxMessages_TrimsOldMessages()
    {
        // Arrange
        var memory = new BufferMemory { MaxMessages = 3 };

        // Act - Add more than MaxMessages
        await memory.AddMessageAsync(ChatMessage.User("Message 1"));
        await memory.AddMessageAsync(ChatMessage.User("Message 2"));
        await memory.AddMessageAsync(ChatMessage.User("Message 3"));
        await memory.AddMessageAsync(ChatMessage.User("Message 4")); // Should trim Message 1

        // Assert
        var count = await memory.GetCountAsync();
        Assert.Equal(3, count);

        var messages = await memory.GetMessagesAsync();
        Assert.Equal("Message 2", messages[0].Content); // Oldest remaining
        Assert.Equal("Message 4", messages[2].Content); // Newest
    }

    [Fact]
    public async Task ClearAsync_RemovesAllMessages()
    {
        // Arrange
        var memory = new BufferMemory();
        await memory.AddMessageAsync(ChatMessage.User("Message 1"));
        await memory.AddMessageAsync(ChatMessage.User("Message 2"));

        // Act
        await memory.ClearAsync();

        // Assert
        var count = await memory.GetCountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var memory = new BufferMemory();

        // Act & Assert
        Assert.Equal(0, await memory.GetCountAsync());

        await memory.AddMessageAsync(ChatMessage.User("Message 1"));
        Assert.Equal(1, await memory.GetCountAsync());

        await memory.AddMessageAsync(ChatMessage.User("Message 2"));
        Assert.Equal(2, await memory.GetCountAsync());
    }

    [Fact]
    public async Task ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var memory = new BufferMemory();
        var tasks = new List<Task>();

        // Act - Add messages concurrently
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                await memory.AddMessageAsync(ChatMessage.User($"Message {index}"));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var count = await memory.GetCountAsync();
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task MaxMessages_LargeValue_NoTrimming()
    {
        // Arrange
        var memory = new BufferMemory { MaxMessages = 1000 }; // Large limit

        // Act - Add many messages
        for (int i = 0; i < 100; i++)
        {
            await memory.AddMessageAsync(ChatMessage.User($"Message {i}"));
        }

        // Assert
        var count = await memory.GetCountAsync();
        Assert.Equal(100, count);
    }
}

