"""
Memory service client for conversation memory management.
"""
from typing import Optional, List
from .unified_client import UnifiedGrpcClient
from .models import ChatMessage
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class Memory:
    """Client for Memory operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get MemoryService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("MemoryService")
        return self._stub
    
    def create(self, memory_id: str, memory_type: str,
               api_key: Optional[str] = None,
               base_url: str = "https://api.openai.com/v1",
               model: str = "gpt-3.5-turbo",
               options: Optional[dict] = None) -> bool:
        """Create a new memory instance."""
        request = sharpaikit_pb2.CreateMemoryRequest(
            memory_id=memory_id,
            memory_type=memory_type,
            api_key=api_key or "",
            base_url=base_url,
            model=model,
            options=options or {}
        )
        try:
            response = self._get_stub().CreateMemory(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create memory: {str(e)}", "MEMORY_ERROR") from e
    
    def add_message(self, memory_id: str, message: ChatMessage) -> bool:
        """Add a message to memory."""
        request = sharpaikit_pb2.AddMessageRequest(
            memory_id=memory_id,
            message=sharpaikit_pb2.ChatMessage(
                Role=message.role,
                Content=message.content,
                Name=message.name or ""
            )
        )
        try:
            response = self._get_stub().AddMessage(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to add message: {str(e)}", "MEMORY_ERROR") from e
    
    def add_exchange(self, memory_id: str, user_message: str, assistant_message: str) -> bool:
        """Add a user-assistant exchange to memory."""
        request = sharpaikit_pb2.AddExchangeRequest(
            memory_id=memory_id,
            Usermessage=user_message,
            Assistantmessage=assistant_message
        )
        try:
            response = self._get_stub().AddExchange(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to add exchange: {str(e)}", "MEMORY_ERROR") from e
    
    def get_messages(self, memory_id: str, query: Optional[str] = None) -> List[ChatMessage]:
        """Get messages from memory."""
        request = sharpaikit_pb2.GetMessagesRequest(
            memory_id=memory_id,
            query=query or ""
        )
        try:
            response = self._get_stub().GetMessages(request)
            if response.success:
                return [
                    ChatMessage(
                        role=msg.role,
                        content=msg.content,
                        name=msg.name if msg.name else None
                    )
                    for msg in response.messages
                ]
            raise SharpAIKitError(response.error or "Unknown error", "MEMORY_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to get messages: {str(e)}", "MEMORY_ERROR") from e
    
    def get_context_string(self, memory_id: str, query: Optional[str] = None) -> str:
        """Get memory as formatted context string."""
        request = sharpaikit_pb2.GetContextStringRequest(
            memory_id=memory_id,
            query=query or ""
        )
        try:
            response = self._get_stub().GetContextString(request)
            if response.success:
                return response.ContextString
            raise SharpAIKitError(response.error or "Unknown error", "MEMORY_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to get context string: {str(e)}", "MEMORY_ERROR") from e
    
    def clear(self, memory_id: str) -> bool:
        """Clear all messages from memory."""
        request = sharpaikit_pb2.ClearMemoryRequest(memory_id=memory_id)
        try:
            response = self._get_stub().ClearMemory(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to clear memory: {str(e)}", "MEMORY_ERROR") from e
    
    def get_count(self, memory_id: str) -> int:
        """Get the number of messages in memory."""
        request = sharpaikit_pb2.GetCountRequest(memory_id=memory_id)
        try:
            response = self._get_stub().GetCount(request)
            if response.success:
                return response.count
            raise SharpAIKitError(response.error or "Unknown error", "MEMORY_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to get count: {str(e)}", "MEMORY_ERROR") from e

