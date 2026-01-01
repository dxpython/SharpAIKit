"""
Chain service client for LCEL-style chain composition.
"""
from typing import Dict, Any, Optional, Iterator
from .unified_client import UnifiedGrpcClient
from .models import ChainContext
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class Chain:
    """Client for Chain operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get ChainService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("ChainService")
        return self._stub
    
    def create(self, chain_id: str, chain_type: str, api_key: str, 
               base_url: str = "https://api.openai.com/v1", 
               model: str = "gpt-3.5-turbo",
               system_prompt: Optional[str] = None,
               options: Optional[Dict[str, str]] = None) -> bool:
        """Create a new chain."""
        request = sharpaikit_pb2.CreateChainRequest(
            chain_id=chain_id,
            chain_type=chain_type,
            api_key=api_key,
            base_url=base_url,
            model=model,
            system_prompt=system_prompt or "",
            options=options or {}
        )
        try:
            response = self._get_stub().CreateChain(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create chain: {str(e)}", "CHAIN_ERROR") from e
    
    def invoke(self, chain_id: str, context: Optional[ChainContext] = None) -> ChainContext:
        """Invoke a chain."""
        ctx_dict = context.to_dict() if context else {}
        request = sharpaikit_pb2.InvokeChainRequest(
            chain_id=chain_id,
            context=sharpaikit_pb2.ChainContext(
                input=ctx_dict.get("input", ""),
                output=ctx_dict.get("output", ""),
                data=ctx_dict.get("data", {})
            )
        )
        try:
            response = self._get_stub().InvokeChain(request)
            if response.success:
                return ChainContext(
                    input=response.context.input,
                    output=response.context.output,
                    data=dict(response.context.data)
                )
            raise SharpAIKitError(response.error or "Unknown error", "CHAIN_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to invoke chain: {str(e)}", "CHAIN_ERROR") from e
    
    def invoke_stream(self, chain_id: str, context: Optional[ChainContext] = None) -> Iterator[ChainContext]:
        """Invoke a chain with streaming."""
        ctx_dict = context.to_dict() if context else {}
        request = sharpaikit_pb2.InvokeChainRequest(
            chain_id=chain_id,
            context=sharpaikit_pb2.ChainContext(
                input=ctx_dict.get("input", ""),
                output=ctx_dict.get("output", ""),
                data=ctx_dict.get("data", {})
            )
        )
        try:
            for chunk in self._get_stub().InvokeChainStream(request):
                if chunk.HasField("context"):
                    yield ChainContext(
                        input=chunk.context.input,
                        output=chunk.context.output,
                        data=dict(chunk.context.data)
                    )
                elif chunk.HasField("error"):
                    raise SharpAIKitError(chunk.error, "CHAIN_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to invoke chain stream: {str(e)}", "CHAIN_ERROR") from e
    
    def pipe(self, chain_id_1: str, chain_id_2: str, result_chain_id: str) -> bool:
        """Pipe two chains together."""
        request = sharpaikit_pb2.PipeChainsRequest(
            chain_id_1=chain_id_1,
            chain_id_2=chain_id_2,
            result_chain_id=result_chain_id
        )
        try:
            response = self._get_stub().PipeChains(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to pipe chains: {str(e)}", "CHAIN_ERROR") from e
    
    def parallel(self, chain_ids: list[str], result_chain_id: str) -> bool:
        """Create a parallel chain."""
        request = sharpaikit_pb2.ParallelChainsRequest(
            chain_ids=chain_ids,
            result_chain_id=result_chain_id
        )
        try:
            response = self._get_stub().ParallelChains(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create parallel chain: {str(e)}", "CHAIN_ERROR") from e
    
    def branch(self, chain_id: str, condition_expression: str, 
               true_chain_id: str, false_chain_id: Optional[str] = None,
               result_chain_id: str = "") -> bool:
        """Create a branch chain."""
        request = sharpaikit_pb2.BranchChainRequest(
            chain_id=chain_id,
            condition_expression=condition_expression,
            true_chain_id=true_chain_id,
            false_chain_id=false_chain_id or "",
            result_chain_id=result_chain_id
        )
        try:
            response = self._get_stub().BranchChain(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create branch chain: {str(e)}", "CHAIN_ERROR") from e

