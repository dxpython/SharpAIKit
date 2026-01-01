"""
Prompt service client for prompt template management.
"""
from typing import Optional, List
from .unified_client import UnifiedGrpcClient
from .models import ChatMessage
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class Prompt:
    """Client for Prompt operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get PromptService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("PromptService")
        return self._stub
    
    def create_template(self, template_id: str, template: str) -> bool:
        """Create a prompt template."""
        request = sharpaikit_pb2.CreateTemplateRequest(
            template_id=template_id,
            template=template
        )
        try:
            response = self._get_stub().CreateTemplate(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create template: {str(e)}", "PROMPT_ERROR") from e
    
    def format_template(self, template_id: str, variables: dict) -> str:
        """Format a prompt template with variables."""
        request = sharpaikit_pb2.FormatTemplateRequest(
            template_id=template_id,
            variables=variables
        )
        try:
            response = self._get_stub().FormatTemplate(request)
            if response.success:
                return response.formatted
            raise SharpAIKitError(response.error or "Unknown error", "PROMPT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to format template: {str(e)}", "PROMPT_ERROR") from e
    
    def create_chat_template(self, template_id: str, messages: List[ChatMessage]) -> bool:
        """Create a chat prompt template."""
        pb_messages = [
            sharpaikit_pb2.ChatMessage(
                Role=msg.role,
                Content=msg.content,
                Name=msg.name or ""
            )
            for msg in messages
        ]
        request = sharpaikit_pb2.CreateChatTemplateRequest(
            template_id=template_id,
            Messages=pb_messages
        )
        try:
            response = self._get_stub().CreateChatTemplate(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create chat template: {str(e)}", "PROMPT_ERROR") from e
    
    def format_chat_template(self, template_id: str, variables: dict) -> List[ChatMessage]:
        """Format a chat template with variables."""
        request = sharpaikit_pb2.FormatChatTemplateRequest(
            template_id=template_id,
            variables=variables
        )
        try:
            response = self._get_stub().FormatChatTemplate(request)
            if response.success:
                return [
                    ChatMessage(
                        role=msg.role,
                        content=msg.content,
                        name=msg.name if msg.name else None
                    )
                    for msg in response.messages
                ]
            raise SharpAIKitError(response.error or "Unknown error", "PROMPT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to format chat template: {str(e)}", "PROMPT_ERROR") from e

