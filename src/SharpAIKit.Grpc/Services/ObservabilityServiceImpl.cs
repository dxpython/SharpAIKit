using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for Observability operations
/// </summary>
public class ObservabilityServiceImpl : ObservabilityService.ObservabilityServiceBase
{
    private readonly ILogger<ObservabilityServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, Activity> _activeTraces = new();
    private readonly ConcurrentBag<MetricDataRecord> _metrics = new();

    public ObservabilityServiceImpl(ILogger<ObservabilityServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<LogResponse> Log(LogRequest request, ServerCallContext context)
    {
        try
        {
            var logLevel = request.Level.ToLower() switch
            {
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Information,
                "warn" => LogLevel.Warning,
                "error" => LogLevel.Error,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "Log: {Message}", request.Message);

            // Log properties if provided
            if (request.Properties != null && request.Properties.Count > 0)
            {
                var props = string.Join(", ", request.Properties.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                _logger.Log(logLevel, "Properties: {Properties}", props);
            }

            return Task.FromResult(new LogResponse { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging");
            return Task.FromResult(new LogResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<RecordMetricResponse> RecordMetric(RecordMetricRequest request, ServerCallContext context)
    {
        try
        {
            var metric = new MetricDataRecord
            {
                Name = request.Name,
                Value = request.Value,
                Tags = request.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            _metrics.Add(metric);

            _logger.LogDebug("Metric recorded: {Name} = {Value}", request.Name, request.Value);

            return Task.FromResult(new RecordMetricResponse { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric");
            return Task.FromResult(new RecordMetricResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<StartTraceResponse> StartTrace(StartTraceRequest request, ServerCallContext context)
    {
        try
        {
            var activity = new Activity(request.OperationName);
            
            if (request.Attributes != null)
            {
                foreach (var attr in request.Attributes)
                {
                    activity.SetTag(attr.Key, attr.Value);
                }
            }

            activity.Start();
            var traceId = activity.TraceId.ToString();
            var spanId = activity.SpanId.ToString();

            _activeTraces[traceId] = activity;

            return Task.FromResult(new StartTraceResponse
            {
                Success = true,
                TraceId = traceId,
                SpanId = spanId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting trace");
            return Task.FromResult(new StartTraceResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<EndTraceResponse> EndTrace(EndTraceRequest request, ServerCallContext context)
    {
        try
        {
            if (_activeTraces.TryRemove(request.TraceId, out var activity))
            {
                if (request.Attributes != null)
                {
                    foreach (var attr in request.Attributes)
                    {
                        activity.SetTag(attr.Key, attr.Value);
                    }
                }

                // Set status
                if (request.Status == "error")
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }
                else
                {
                    activity.SetStatus(ActivityStatusCode.Ok);
                }

                activity.Stop();

                return Task.FromResult(new EndTraceResponse { Success = true });
            }

            return Task.FromResult(new EndTraceResponse
            {
                Success = false,
                Error = $"Trace {request.TraceId} not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending trace");
            return Task.FromResult(new EndTraceResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
    {
        try
        {
            var response = new GetMetricsResponse { Success = true };

            var metrics = _metrics.ToList();

            // Filter by name if provided
            if (!string.IsNullOrEmpty(request.MetricName))
            {
                metrics = metrics.Where(m => m.Name == request.MetricName).ToList();
            }

            // Filter by time range if provided
            if (!string.IsNullOrEmpty(request.StartTime) && DateTime.TryParse(request.StartTime, out var startTime))
            {
                metrics = metrics.Where(m => DateTime.TryParse(m.Timestamp, out var ts) && ts >= startTime).ToList();
            }

            if (!string.IsNullOrEmpty(request.EndTime) && DateTime.TryParse(request.EndTime, out var endTime))
            {
                metrics = metrics.Where(m => DateTime.TryParse(m.Timestamp, out var ts) && ts <= endTime).ToList();
            }

            foreach (var metric in metrics)
            {
                var grpcMetric = new SharpAIKit.Grpc.MetricData
                {
                    Name = metric.Name,
                    Value = metric.Value,
                    Timestamp = metric.Timestamp
                };
                foreach (var tag in metric.Tags)
                {
                    grpcMetric.Tags[tag.Key] = tag.Value;
                }
                response.Metrics.Add(grpcMetric);
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            return Task.FromResult(new GetMetricsResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Internal metric data record
    /// </summary>
    private class MetricDataRecord
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public string Timestamp { get; set; } = string.Empty;
    }
}

