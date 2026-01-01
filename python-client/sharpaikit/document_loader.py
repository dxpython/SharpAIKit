"""
DocumentLoader service client for document loading.
"""
from typing import List
from .unified_client import UnifiedGrpcClient
from .models import Document
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class DocumentLoader:
    """Client for DocumentLoader operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get DocumentLoaderService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("DocumentLoaderService")
        return self._stub
    
    def load_text(self, file_path: str) -> List[Document]:
        """Load text file."""
        request = sharpaikit_pb2.LoadTextRequest(file_path=file_path)
        try:
            response = self._get_stub().LoadText(request)
            if response.success:
                return [
                    Document(
                        content=doc.content,
                        metadata=dict(doc.Metadata),
                        source=doc.Source if doc.Source else None
                    )
                    for doc in response.Documents
                ]
            raise SharpAIKitError(response.error or "Unknown error", "DOCUMENT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to load text: {str(e)}", "DOCUMENT_ERROR") from e
    
    def load_csv(self, file_path: str) -> List[Document]:
        """Load CSV file."""
        request = sharpaikit_pb2.LoadCSVRequest(file_path=file_path)
        try:
            response = self._get_stub().LoadCSV(request)
            if response.success:
                return [
                    Document(
                        content=doc.content,
                        metadata=dict(doc.Metadata),
                        source=doc.Source if doc.Source else None
                    )
                    for doc in response.Documents
                ]
            raise SharpAIKitError(response.error or "Unknown error", "DOCUMENT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to load CSV: {str(e)}", "DOCUMENT_ERROR") from e
    
    def load_json(self, file_path: str) -> List[Document]:
        """Load JSON file."""
        request = sharpaikit_pb2.LoadJSONRequest(file_path=file_path)
        try:
            response = self._get_stub().LoadJSON(request)
            if response.success:
                return [
                    Document(
                        content=doc.content,
                        metadata=dict(doc.Metadata),
                        source=doc.Source if doc.Source else None
                    )
                    for doc in response.Documents
                ]
            raise SharpAIKitError(response.error or "Unknown error", "DOCUMENT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to load JSON: {str(e)}", "DOCUMENT_ERROR") from e
    
    def load_markdown(self, file_path: str) -> List[Document]:
        """Load Markdown file."""
        request = sharpaikit_pb2.LoadMarkdownRequest(file_path=file_path)
        try:
            response = self._get_stub().LoadMarkdown(request)
            if response.success:
                return [
                    Document(
                        content=doc.content,
                        metadata=dict(doc.Metadata),
                        source=doc.Source if doc.Source else None
                    )
                    for doc in response.Documents
                ]
            raise SharpAIKitError(response.error or "Unknown error", "DOCUMENT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to load Markdown: {str(e)}", "DOCUMENT_ERROR") from e
    
    def load_web(self, url: str) -> List[Document]:
        """Load web page."""
        request = sharpaikit_pb2.LoadWebRequest(url=url)
        try:
            response = self._get_stub().LoadWeb(request)
            if response.success:
                return [
                    Document(
                        content=doc.content,
                        metadata=dict(doc.Metadata),
                        source=doc.Source if doc.Source else None
                    )
                    for doc in response.Documents
                ]
            raise SharpAIKitError(response.error or "Unknown error", "DOCUMENT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to load web: {str(e)}", "DOCUMENT_ERROR") from e
    
    def load_directory(self, directory_path: str, pattern: str = "*.txt") -> List[Document]:
        """Load all files from directory."""
        request = sharpaikit_pb2.LoadDirectoryRequest(
            directory_path=directory_path,
            pattern=pattern
        )
        try:
            response = self._get_stub().LoadDirectory(request)
            if response.success:
                return [
                    Document(
                        content=doc.content,
                        metadata=dict(doc.Metadata),
                        source=doc.Source if doc.Source else None
                    )
                    for doc in response.Documents
                ]
            raise SharpAIKitError(response.error or "Unknown error", "DOCUMENT_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to load directory: {str(e)}", "DOCUMENT_ERROR") from e

