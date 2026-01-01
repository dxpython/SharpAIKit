using Xunit;
using SharpAIKit.Memory;
using SharpAIKit.Common;

namespace SharpAIKit.Tests.Memory;

/// <summary>
/// Tests for WindowBufferMemory: window-based memory trimming
/// </summary>
public class WindowBufferMemoryTests
{
    [Fact]
    public async Task WindowSize_TrimsToWindow()
    {
        // Arrange
        var memory = new WindowBufferMemory { WindowSize = 2 }; // Keep 2 exchanges

        // Act - Add 3 exchanges (6 messages)
        await memory.AddExchangeAsync("Exchange 1 User", "Exchange 1 Assistant");
        await memory.AddExchangeAsync("Exchange 2 User", "Exchange 2 Assistant");
        await memory.AddExchangeAsync("Exchange 3 User", "Exchange 3 Assistant");

        // Assert - Should keep only last 2 exchanges (4 messages)
        var count = await memory.GetCountAsync();
        Assert.Equal(4, count); // 2 exchanges * 2 messages each

        var messages = await memory.GetMessagesAsync();
        Assert.Equal("Exchange 2 User", messages[0].Content); // Oldest remaining
        Assert.Equal("Exchange 3 Assistant", messages[3].Content); // Newest
    }

    [Fact]
    public async Task WindowSize_One_KeepsOneExchange()
    {
        // Arrange
        var memory = new WindowBufferMemory { WindowSize = 1 };

        // Act
        await memory.AddExchangeAsync("User 1", "Assistant 1");
        await memory.AddExchangeAsync("User 2", "Assistant 2");

        // Assert
        var count = await memory.GetCountAsync();
        Assert.Equal(2, count); // Only last exchange

        var messages = await memory.GetMessagesAsync();
        Assert.Equal("User 2", messages[0].Content);
        Assert.Equal("Assistant 2", messages[1].Content);
    }

    [Fact]
    public async Task AddMessageAsync_TrimsToWindow()
    {
        // Arrange
        var memory = new WindowBufferMemory { WindowSize = 2 };

        // Act - Add individual messages
        await memory.AddMessageAsync(ChatMessage.User("User 1"));
        await memory.AddMessageAsync(ChatMessage.Assistant("Assistant 1"));
        await memory.AddMessageAsync(ChatMessage.User("User 2"));
        await memory.AddMessageAsync(ChatMessage.Assistant("Assistant 2"));
        await memory.AddMessageAsync(ChatMessage.User("User 3"));

        // Assert - Should trim to window (2 exchanges = 4 messages max)
        var count = await memory.GetCountAsync();
        Assert.True(count <= 4);
    }
}

