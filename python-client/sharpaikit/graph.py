"""
Graph service client for SharpGraph orchestration.
"""
from typing import Optional, Iterator
from .unified_client import UnifiedGrpcClient
from .models import GraphState
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class Graph:
    """Client for Graph operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get GraphService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("GraphService")
        return self._stub
    
    def create(self, graph_id: str) -> bool:
        """Create a new graph."""
        request = sharpaikit_pb2.CreateGraphRequest(graph_id=graph_id)
        try:
            response = self._get_stub().CreateGraph(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create graph: {str(e)}", "GRAPH_ERROR") from e
    
    def add_node(self, graph_id: str, node_id: str, node_type: str, 
                 handler: Optional[str] = None) -> bool:
        """Add a node to the graph."""
        request = sharpaikit_pb2.AddNodeRequest(
            graph_id=graph_id,
            node_id=node_id,
            node_type=node_type,
            handler=handler or ""
        )
        try:
            response = self._get_stub().AddNode(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to add node: {str(e)}", "GRAPH_ERROR") from e
    
    def add_edge(self, graph_id: str, from_node: str, to_node: str, 
                 condition: Optional[str] = None) -> bool:
        """Add an edge to the graph."""
        request = sharpaikit_pb2.AddEdgeRequest(
            graph_id=graph_id,
            from_node=from_node,
            to_node=to_node,
            condition=condition or ""
        )
        try:
            response = self._get_stub().AddEdge(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to add edge: {str(e)}", "GRAPH_ERROR") from e
    
    def run(self, graph_id: str, initial_state: Optional[GraphState] = None) -> GraphState:
        """Run the graph."""
        state_dict = initial_state.to_dict() if initial_state else {}
        request = sharpaikit_pb2.RunGraphRequest(
            graph_id=graph_id,
            initial_state=sharpaikit_pb2.GraphState(
                CurrentNode=state_dict.get("current_node", ""),
                NextNode=state_dict.get("next_node", ""),
                ShouldEnd=state_dict.get("should_end", False),
                Output=state_dict.get("output", ""),
                Data=state_dict.get("data", {})
            )
        )
        try:
            response = self._get_stub().RunGraph(request)
            if response.success:
                return GraphState(
                    current_node=response.state.current_node,
                    next_node=response.state.next_node,
                    should_end=response.state.should_end,
                    output=response.state.output,
                    data=dict(response.state.data)
                )
            raise SharpAIKitError(response.error or "Unknown error", "GRAPH_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to run graph: {str(e)}", "GRAPH_ERROR") from e
    
    def run_stream(self, graph_id: str, initial_state: Optional[GraphState] = None) -> Iterator[GraphState]:
        """Run the graph with streaming."""
        state_dict = initial_state.to_dict() if initial_state else {}
        request = sharpaikit_pb2.RunGraphRequest(
            graph_id=graph_id,
            initial_state=sharpaikit_pb2.GraphState(
                CurrentNode=state_dict.get("current_node", ""),
                NextNode=state_dict.get("next_node", ""),
                ShouldEnd=state_dict.get("should_end", False),
                Output=state_dict.get("output", ""),
                Data=state_dict.get("data", {})
            )
        )
        try:
            for chunk in self._get_stub().RunGraphStream(request):
                if chunk.HasField("state"):
                    yield GraphState(
                        current_node=chunk.state.current_node,
                        next_node=chunk.state.next_node,
                        should_end=chunk.state.should_end,
                        output=chunk.state.output,
                        data=dict(chunk.state.data)
                    )
                elif chunk.HasField("error"):
                    raise SharpAIKitError(chunk.error, "GRAPH_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to run graph stream: {str(e)}", "GRAPH_ERROR") from e
    
    def get_state(self, graph_id: str) -> GraphState:
        """Get current graph state."""
        request = sharpaikit_pb2.GetStateRequest(graph_id=graph_id)
        try:
            response = self._get_stub().GetState(request)
            if response.success:
                return GraphState(
                    current_node=response.state.current_node,
                    next_node=response.state.next_node,
                    should_end=response.state.should_end,
                    output=response.state.output,
                    data=dict(response.state.data)
                )
            raise SharpAIKitError(response.error or "Unknown error", "GRAPH_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to get state: {str(e)}", "GRAPH_ERROR") from e
    
    def save_state(self, graph_id: str, state: GraphState) -> bool:
        """Save graph state."""
        state_dict = state.to_dict()
        request = sharpaikit_pb2.SaveStateRequest(
            graph_id=graph_id,
            State=sharpaikit_pb2.GraphState(
                CurrentNode=state_dict.get("current_node", ""),
                NextNode=state_dict.get("next_node", ""),
                ShouldEnd=state_dict.get("should_end", False),
                Output=state_dict.get("output", ""),
                Data=state_dict.get("data", {})
            )
        )
        try:
            response = self._get_stub().SaveState(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to save state: {str(e)}", "GRAPH_ERROR") from e
    
    def load_state(self, graph_id: str) -> GraphState:
        """Load graph state."""
        request = sharpaikit_pb2.LoadStateRequest(graph_id=graph_id)
        try:
            response = self._get_stub().LoadState(request)
            if response.success:
                return GraphState(
                    current_node=response.state.current_node,
                    next_node=response.state.next_node,
                    should_end=response.state.should_end,
                    output=response.state.output,
                    data=dict(response.state.data)
                )
            raise SharpAIKitError(response.error or "Unknown error", "GRAPH_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to load state: {str(e)}", "GRAPH_ERROR") from e

