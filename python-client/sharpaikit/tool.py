"""
Tool service client for tool registration and management.
"""
from typing import List, Dict
from .unified_client import UnifiedGrpcClient
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class Tool:
    """Client for Tool operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get ToolService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("ToolService")
        return self._stub
    
    def register(self, tool_name: str, description: str,
                 parameters: List[Dict[str, str]]) -> bool:
        """Register a tool."""
        pb_params = [
            sharpaikit_pb2.ParameterDefinition(
                Name=param.get("name", ""),
                type=param.get("type", "string"),
                description=param.get("description", ""),
                required=param.get("required", True)
            )
            for param in parameters
        ]
        request = sharpaikit_pb2.RegisterToolRequest(
            Tool=sharpaikit_pb2.ToolDefinition(
                Name=tool_name,
                description=description,
                parameters=pb_params
            )
        )
        try:
            response = self._get_stub().RegisterTool(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to register tool: {str(e)}", "TOOL_ERROR") from e
    
    def list_tools(self) -> List[Dict]:
        """List all registered tools."""
        request = sharpaikit_pb2.ListToolsRequest()
        try:
            response = self._get_stub().ListTools(request)
            if response.success:
                return [
                    {
                        "name": tool.name,
                        "description": tool.Description,
                        "parameters": [
                            {
                                "name": param.name,
                                "type": param.Type,
                                "description": param.Description,
                                "required": param.Required
                            }
                            for param in tool.Parameters
                        ]
                    }
                    for tool in response.tools
                ]
            raise SharpAIKitError(response.error or "Unknown error", "TOOL_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to list tools: {str(e)}", "TOOL_ERROR") from e
    
    def unregister(self, tool_name: str) -> bool:
        """Unregister a tool."""
        request = sharpaikit_pb2.UnregisterToolRequest(tool_name=tool_name)
        try:
            response = self._get_stub().UnregisterTool(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to unregister tool: {str(e)}", "TOOL_ERROR") from e

