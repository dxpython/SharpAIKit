"""
Optimizer service client for DSPy-style prompt optimization.
"""
from typing import List, Iterator, Optional
from .unified_client import UnifiedGrpcClient
from .models import OptimizationResult
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class Optimizer:
    """Client for Optimizer operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get OptimizerService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("OptimizerService")
        return self._stub
    
    def create(self, optimizer_id: str, api_key: str,
               base_url: str = "https://api.openai.com/v1",
               model: str = "gpt-3.5-turbo") -> bool:
        """Create a new optimizer."""
        request = sharpaikit_pb2.CreateOptimizerRequest(
            optimizer_id=optimizer_id,
            api_key=api_key,
            base_url=base_url,
            model=model
        )
        try:
            response = self._get_stub().CreateOptimizer(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create optimizer: {str(e)}", "OPTIMIZER_ERROR") from e
    
    def optimize(self, optimizer_id: str, initial_prompt: str,
                 examples: List[dict], metric_name: str = "accuracy",
                 max_iterations: int = 10) -> OptimizationResult:
        """Optimize a prompt."""
        pb_examples = [
            sharpaikit_pb2.TrainingExample(
                Input=ex.get("input", ""),
                Output=ex.get("output", ""),
                expected_output=ex.get("expected_output", "")
            )
            for ex in examples
        ]
        request = sharpaikit_pb2.OptimizeRequest(
            optimizer_id=optimizer_id,
            initial_prompt=initial_prompt,
            examples=pb_examples,
            metric_name=metric_name,
            max_iterations=max_iterations
        )
        try:
            response = self._get_stub().Optimize(request)
            if response.success:
                return OptimizationResult(
                    success=True,
                    optimized_prompt=response.optimized_prompt,
                    metric_score=response.metric_score,
                    steps=[
                        {
                            "iteration": step.iteration,
                            "prompt": step.prompt,
                            "score": step.score
                        }
                        for step in response.steps
                    ]
                )
            raise SharpAIKitError(response.error or "Unknown error", "OPTIMIZER_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to optimize: {str(e)}", "OPTIMIZER_ERROR") from e
    
    def optimize_stream(self, optimizer_id: str, initial_prompt: str,
                       examples: List[dict], metric_name: str = "accuracy",
                       max_iterations: int = 10) -> Iterator[dict]:
        """Optimize a prompt with streaming."""
        pb_examples = [
            sharpaikit_pb2.TrainingExample(
                Input=ex.get("input", ""),
                Output=ex.get("output", ""),
                expected_output=ex.get("expected_output", "")
            )
            for ex in examples
        ]
        request = sharpaikit_pb2.OptimizeRequest(
            optimizer_id=optimizer_id,
            initial_prompt=initial_prompt,
            examples=pb_examples,
            metric_name=metric_name,
            max_iterations=max_iterations
        )
        try:
            for chunk in self._get_stub().OptimizeStream(request):
                if chunk.HasField("step"):
                    yield {
                        "iteration": chunk.step.iteration,
                        "prompt": chunk.step.prompt,
                        "score": chunk.step.score
                    }
                elif chunk.HasField("result"):
                    yield {
                        "optimized_prompt": chunk.result.optimized_prompt,
                        "metric_score": chunk.result.metric_score
                    }
                elif chunk.HasField("error"):
                    raise SharpAIKitError(chunk.error, "OPTIMIZER_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to optimize stream: {str(e)}", "OPTIMIZER_ERROR") from e

