"""
Observability service client for logging, metrics, and tracing.
"""
from typing import Optional, Dict, List
from .unified_client import UnifiedGrpcClient
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class Observability:
    """Client for Observability operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get ObservabilityService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("ObservabilityService")
        return self._stub
    
    def log(self, level: str, message: str, metadata: Optional[Dict[str, str]] = None) -> bool:
        """Log a message."""
        request = sharpaikit_pb2.LogRequest(
            level=level,
            message=message,
            metadata=metadata or {}
        )
        try:
            response = self._get_stub().Log(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to log: {str(e)}", "OBSERVABILITY_ERROR") from e
    
    def record_metric(self, name: str, value: float, tags: Optional[Dict[str, str]] = None) -> bool:
        """Record a metric."""
        request = sharpaikit_pb2.RecordMetricRequest(
            Name=name,
            value=value,
            tags=tags or {}
        )
        try:
            response = self._get_stub().RecordMetric(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to record metric: {str(e)}", "OBSERVABILITY_ERROR") from e
    
    def start_trace(self, name: str, metadata: Optional[Dict[str, str]] = None) -> str:
        """Start a trace span."""
        request = sharpaikit_pb2.StartTraceRequest(
            Name=name,
            metadata=metadata or {}
        )
        try:
            response = self._get_stub().StartTrace(request)
            if response.success:
                return response.span_id
            raise SharpAIKitError(response.error or "Unknown error", "OBSERVABILITY_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to start trace: {str(e)}", "OBSERVABILITY_ERROR") from e
    
    def end_trace(self, span_id: str, metadata: Optional[Dict[str, str]] = None) -> bool:
        """End a trace span."""
        request = sharpaikit_pb2.EndTraceRequest(
            span_id=span_id,
            metadata=metadata or {}
        )
        try:
            response = self._get_stub().EndTrace(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to end trace: {str(e)}", "OBSERVABILITY_ERROR") from e
    
    def get_metrics(self, metric_name: Optional[str] = None) -> List[Dict]:
        """Get metrics."""
        request = sharpaikit_pb2.GetMetricsRequest(
            metric_name=metric_name or ""
        )
        try:
            response = self._get_stub().GetMetrics(request)
            if response.success:
                return [
                    {
                        "name": metric.name,
                        "value": metric.Value,
                        "tags": dict(metric.Tags),
                        "timestamp": metric.Timestamp
                    }
                    for metric in response.metrics
                ]
            raise SharpAIKitError(response.error or "Unknown error", "OBSERVABILITY_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to get metrics: {str(e)}", "OBSERVABILITY_ERROR") from e

