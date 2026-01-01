"""
OutputParser service client for output parsing.
"""
from typing import Optional, List, Dict
from .unified_client import UnifiedGrpcClient
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class OutputParser:
    """Client for OutputParser operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get OutputParserService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("OutputParserService")
        return self._stub
    
    def parse_json(self, text: str) -> Dict:
        """Parse JSON from text."""
        request = sharpaikit_pb2.ParseJSONRequest(text=text)
        try:
            response = self._get_stub().ParseJSON(request)
            if response.success:
                import json
                return json.loads(response.parsed)
            raise SharpAIKitError(response.error or "Unknown error", "PARSER_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to parse JSON: {str(e)}", "PARSER_ERROR") from e
    
    def parse_boolean(self, text: str) -> bool:
        """Parse boolean from text."""
        request = sharpaikit_pb2.ParseBooleanRequest(text=text)
        try:
            response = self._get_stub().ParseBoolean(request)
            if response.success:
                return response.parsed
            raise SharpAIKitError(response.error or "Unknown error", "PARSER_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to parse boolean: {str(e)}", "PARSER_ERROR") from e
    
    def parse_list(self, text: str, separator: str = ",") -> List[str]:
        """Parse list from text."""
        request = sharpaikit_pb2.ParseListRequest(
            text=text,
            separator=separator
        )
        try:
            response = self._get_stub().ParseList(request)
            if response.success:
                return list(response.parsed)
            raise SharpAIKitError(response.error or "Unknown error", "PARSER_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to parse list: {str(e)}", "PARSER_ERROR") from e
    
    def parse_xml(self, text: str, tag: str) -> str:
        """Parse XML tag content from text."""
        request = sharpaikit_pb2.ParseXMLRequest(
            text=text,
            tag=tag
        )
        try:
            response = self._get_stub().ParseXML(request)
            if response.success:
                return response.parsed
            raise SharpAIKitError(response.error or "Unknown error", "PARSER_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to parse XML: {str(e)}", "PARSER_ERROR") from e
    
    def parse_regex(self, text: str, pattern: str) -> Optional[str]:
        """Parse text using regex pattern."""
        request = sharpaikit_pb2.ParseRegexRequest(
            text=text,
            pattern=pattern
        )
        try:
            response = self._get_stub().ParseRegex(request)
            if response.success:
                return response.parsed if response.parsed else None
            raise SharpAIKitError(response.error or "Unknown error", "PARSER_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to parse regex: {str(e)}", "PARSER_ERROR") from e

