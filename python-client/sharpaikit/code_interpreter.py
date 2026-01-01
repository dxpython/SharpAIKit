"""
CodeInterpreter service client for C# code execution.
"""
from typing import Optional, TypeVar, Type
from .unified_client import UnifiedGrpcClient
from .models import CodeExecutionResult
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError

T = TypeVar('T')


class CodeInterpreter:
    """Client for CodeInterpreter operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get CodeInterpreterService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("CodeInterpreterService")
        return self._stub
    
    def execute(self, code: str, timeout_ms: Optional[int] = None) -> CodeExecutionResult:
        """Execute C# code."""
        request = sharpaikit_pb2.ExecuteCodeRequest(
            code=code,
            timeout_ms=timeout_ms or 0
        )
        try:
            response = self._get_stub().ExecuteCode(request)
            return CodeExecutionResult(
                success=response.success,
                output=response.output,
                error=response.error if response.error else None,
                execution_time_ms=response.ExecutionTimeMs if response.ExecutionTimeMs > 0 else None
            )
        except Exception as e:
            raise SharpAIKitError(f"Failed to execute code: {str(e)}", "CODE_ERROR") from e
    
    def execute_typed(self, code: str, return_type: str, timeout_ms: Optional[int] = None) -> any:
        """Execute C# code and return typed result."""
        request = sharpaikit_pb2.ExecuteCodeTypedRequest(
            code=code,
            return_type=return_type,
            timeout_ms=timeout_ms or 0
        )
        try:
            response = self._get_stub().ExecuteCodeTyped(request)
            if response.success:
                # Parse the typed result based on return_type
                import json
                return json.loads(response.Result)
            raise SharpAIKitError(response.error or "Unknown error", "CODE_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to execute typed code: {str(e)}", "CODE_ERROR") from e

