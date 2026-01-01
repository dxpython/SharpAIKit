using Xunit;
using SharpAIKit.Chain;

namespace SharpAIKit.Tests.Chain;

/// <summary>
/// Tests for ChainBase: composition, piping, and context flow
/// </summary>
public class ChainBaseTests
{
    private class TestChain : ChainBase
    {
        private readonly Func<ChainContext, ChainContext> _transform;

        public TestChain(Func<ChainContext, ChainContext> transform)
        {
            _transform = transform;
        }

        protected override Task<ChainContext> ExecuteAsync(ChainContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(_transform(context));
        }
    }

    [Fact]
    public async Task InvokeAsync_StringInput_ReturnsStringOutput()
    {
        // Arrange
        var chain = new TestChain(ctx =>
        {
            ctx.Output = ctx.Input.ToUpper();
            return ctx;
        });

        // Act
        var result = await chain.InvokeAsync("hello");

        // Assert
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task InvokeAsync_ContextInput_ReturnsContext()
    {
        // Arrange
        var chain = new TestChain(ctx =>
        {
            ctx.Output = ctx.Input + " processed";
            return ctx;
        });

        var context = ChainContext.FromInput("test");

        // Act
        var result = await chain.InvokeAsync(context);

        // Assert
        Assert.Equal("test processed", result.Output);
        Assert.Same(context, result); // Should return same context instance
    }

    [Fact]
    public async Task Pipe_ComposesChains()
    {
        // Arrange
        var chain1 = new TestChain(ctx =>
        {
            ctx.Output = ctx.Input.ToUpper();
            return ctx;
        });

        var chain2 = new TestChain(ctx =>
        {
            ctx.Output = ctx.Output + "!";
            return ctx;
        });

        // Act
        var composed = chain1.Pipe(chain2);
        var result = await composed.InvokeAsync("hello");

        // Assert
        Assert.Equal("HELLO!", result);
    }

    [Fact]
    public async Task OperatorPipe_ComposesChains()
    {
        // Arrange
        var chain1 = new TestChain(ctx =>
        {
            ctx.Output = ctx.Input.ToUpper();
            return ctx;
        });

        var chain2 = new TestChain(ctx =>
        {
            ctx.Output = ctx.Output + "!";
            return ctx;
        });

        // Act - Using | operator
        var composed = chain1 | chain2;
        var result = await composed.InvokeAsync("hello");

        // Assert
        Assert.Equal("HELLO!", result);
    }

    [Fact]
    public async Task StreamAsync_Default_YieldsFinalResult()
    {
        // Arrange
        var chain = new TestChain(ctx =>
        {
            ctx.Output = ctx.Input.ToUpper();
            return ctx;
        });

        var context = ChainContext.FromInput("test");

        // Act
        var results = new List<ChainContext>();
        await foreach (var chunk in chain.StreamAsync(context).ConfigureAwait(false))
        {
            results.Add(chunk);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("TEST", results[0].Output);
    }

    [Fact]
    public async Task Context_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var context = ChainContext.FromInput("original");
        context.Set("key1", "value1");

        // Act
        var clone = context.Clone();
        clone.Set("key1", "value2");
        clone.Input = "modified";

        // Assert
        Assert.Equal("original", context.Input);
        Assert.Equal("value1", context.Get<string>("key1"));
        Assert.Equal("modified", clone.Input);
        Assert.Equal("value2", clone.Get<string>("key1"));
    }

    [Fact]
    public void Context_FromDictionary_CreatesContext()
    {
        // Arrange
        var data = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var context = ChainContext.FromDictionary(data);

        // Assert
        Assert.Equal("value1", context.Get<string>("key1"));
        Assert.Equal(42, context.Get<int>("key2"));
    }

    [Fact]
    public void Context_GetSet_WorksCorrectly()
    {
        // Arrange
        var context = new ChainContext();

        // Act
        context.Set("string_key", "value");
        context.Set("int_key", 123);
        context.Set("null_key", null);

        // Assert
        Assert.Equal("value", context.Get<string>("string_key"));
        Assert.Equal(123, context.Get<int>("int_key"));
        Assert.Null(context.Get<string>("null_key"));
        Assert.Equal(default(int), context.Get<int>("nonexistent"));
    }
}

