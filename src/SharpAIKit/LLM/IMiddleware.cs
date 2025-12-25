using SharpAIKit.Common;
using Microsoft.Extensions.Logging;

namespace SharpAIKit.LLM;

/// <summary>
/// Represents the context for LLM middleware.
/// </summary>
public class LLMMiddlewareContext
{
    /// <summary>
    /// Gets or sets the messages.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the chat options.
    /// </summary>
    public ChatOptions? Options { get; set; }

    /// <summary>
    /// Gets or sets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets the response (set by middleware or LLM).
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the execution time.
    /// </summary>
    public TimeSpan? ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets any error that occurred.
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// Gets or sets metadata for middleware communication.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

/// <summary>
/// Interface for LLM middleware.
/// Middleware can intercept and modify requests/responses.
/// </summary>
public interface ILLMMiddleware
{
    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The middleware context.</param>
    /// <param name="next">The next middleware or LLM client in the chain.</param>
    Task InvokeAsync(LLMMiddlewareContext context, Func<LLMMiddlewareContext, Task> next);
}

/// <summary>
/// Retry middleware for LLM calls.
/// </summary>
public class RetryMiddleware : ILLMMiddleware
{
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;
    private readonly Func<Exception, bool>? _shouldRetry;

    /// <summary>
    /// Creates a new retry middleware.
    /// </summary>
    public RetryMiddleware(int maxRetries = 3, TimeSpan? delay = null, Func<Exception, bool>? shouldRetry = null)
    {
        _maxRetries = maxRetries;
        _delay = delay ?? TimeSpan.FromSeconds(1);
        _shouldRetry = shouldRetry;
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(LLMMiddlewareContext context, Func<LLMMiddlewareContext, Task> next)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _maxRetries)
        {
            try
            {
                await next(context);
                if (context.Error == null)
                {
                    return; // Success
                }
                lastException = context.Error;
            }
            catch (Exception ex)
            {
                lastException = ex;
                context.Error = ex;
            }

            // Check if we should retry
            if (lastException != null && _shouldRetry != null && !_shouldRetry(lastException))
            {
                break;
            }

            attempt++;
            if (attempt < _maxRetries)
            {
                await Task.Delay(_delay * attempt, context.CancellationToken);
                context.Error = null; // Reset error for retry
            }
        }

        if (lastException != null)
        {
            context.Error = lastException;
            throw lastException;
        }
    }
}

/// <summary>
/// Rate limiting middleware.
/// </summary>
public class RateLimitMiddleware : ILLMMiddleware
{
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan _timeWindow;
    private readonly int _maxRequests;
    private readonly Queue<DateTime> _requestTimes = new();

    /// <summary>
    /// Creates a new rate limit middleware.
    /// </summary>
    public RateLimitMiddleware(int maxRequests, TimeSpan timeWindow)
    {
        _maxRequests = maxRequests;
        _timeWindow = timeWindow;
        _semaphore = new SemaphoreSlim(maxRequests, maxRequests);
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(LLMMiddlewareContext context, Func<LLMMiddlewareContext, Task> next)
    {
        var now = DateTime.UtcNow;

        // Clean old requests
        while (_requestTimes.Count > 0 && now - _requestTimes.Peek() > _timeWindow)
        {
            _requestTimes.Dequeue();
        }

        // Check if we're at the limit
        if (_requestTimes.Count >= _maxRequests)
        {
            var oldestRequest = _requestTimes.Peek();
            var waitTime = _timeWindow - (now - oldestRequest);
            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime, context.CancellationToken);
            }
            _requestTimes.Dequeue();
        }

        _requestTimes.Enqueue(now);
        await _semaphore.WaitAsync(context.CancellationToken);

        try
        {
            await next(context);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// Logging middleware.
/// </summary>
public class LoggingMiddleware : ILLMMiddleware
{
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;

    /// <summary>
    /// Creates a new logging middleware.
    /// </summary>
    public LoggingMiddleware(Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(LLMMiddlewareContext context, Func<LLMMiddlewareContext, Task> next)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger?.LogInformation("LLM Request started. Messages: {Count}", context.Messages.Count);

        try
        {
            await next(context);
            stopwatch.Stop();
            context.ExecutionTime = stopwatch.Elapsed;

            _logger?.LogInformation(
                "LLM Request completed. Duration: {Duration}ms, Response length: {Length}",
                stopwatch.ElapsedMilliseconds,
                context.Response?.Length ?? 0);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.Error = ex;
            _logger?.LogError(ex, "LLM Request failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Circuit breaker middleware for resilience.
/// </summary>
public class CircuitBreakerMiddleware : ILLMMiddleware
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private int _failureCount;
    private DateTime? _lastFailureTime;
    private bool _isOpen;

    /// <summary>
    /// Creates a new circuit breaker middleware.
    /// </summary>
    public CircuitBreakerMiddleware(int failureThreshold = 5, TimeSpan? timeout = null)
    {
        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(LLMMiddlewareContext context, Func<LLMMiddlewareContext, Task> next)
    {
        // Check if circuit is open
        if (_isOpen)
        {
            if (_lastFailureTime.HasValue && DateTime.UtcNow - _lastFailureTime.Value > _timeout)
            {
                // Try to close the circuit
                _isOpen = false;
                _failureCount = 0;
            }
            else
            {
                throw new InvalidOperationException("Circuit breaker is open. Too many failures.");
            }
        }

        try
        {
            await next(context);
            if (context.Error == null)
            {
                // Success - reset failure count
                _failureCount = 0;
                _isOpen = false;
            }
            else
            {
                RecordFailure();
            }
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }

    private void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _isOpen = true;
        }
    }
}

