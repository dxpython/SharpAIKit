"""
Unified gRPC client for all SharpAIKit services
"""

import grpc
import logging
from typing import Optional, Dict, Any, Iterator
from ._grpc import sharpaikit_pb2, sharpaikit_pb2_grpc
from .errors import ConnectionError, SharpAIKitError

logger = logging.getLogger(__name__)


class UnifiedGrpcClient:
    """Unified gRPC client for all SharpAIKit services"""

    def __init__(self, host: str = "localhost", port: int = 50051):
        """
        Initialize unified gRPC client

        Args:
            host: Host address
            port: gRPC port
        """
        self.host = host
        self.port = port
        self._channel: Optional[grpc.Channel] = None
        self._stubs: Dict[str, Any] = {}

    def _ensure_connected(self) -> None:
        """Ensure gRPC channel is connected"""
        if self._channel is None or self._channel.get_state() == grpc.ChannelConnectivity.SHUTDOWN:
            try:
                target = f"{self.host}:{self.port}"
                self._channel = grpc.insecure_channel(target)

                # Wait for channel to be ready (with timeout)
                try:
                    grpc.channel_ready_future(self._channel).result(timeout=5)
                except grpc.FutureTimeoutError:
                    raise ConnectionError(f"Failed to connect to {target}")

            except Exception as e:
                if isinstance(e, ConnectionError):
                    raise
                raise ConnectionError(f"Failed to connect to {self.host}:{self.port}: {str(e)}") from e

    def _get_stub(self, service_name: str):
        """Get or create a service stub"""
        self._ensure_connected()
        
        if service_name not in self._stubs:
            stub_class = getattr(sharpaikit_pb2_grpc, f"{service_name}Stub", None)
            if stub_class is None:
                raise SharpAIKitError(f"Service {service_name} not found", "SERVICE_NOT_FOUND")
            self._stubs[service_name] = stub_class(self._channel)
        
        return self._stubs[service_name]

    def close(self) -> None:
        """Close the gRPC channel"""
        if self._channel:
            self._channel.close()
            self._channel = None
            self._stubs.clear()

    def __enter__(self):
        """Context manager entry"""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit"""
        self.close()

