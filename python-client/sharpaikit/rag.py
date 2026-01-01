"""
RAG service client for Retrieval-Augmented Generation.
"""
from typing import Optional, List, Iterator
from .unified_client import UnifiedGrpcClient
from .models import SearchResult
from ._grpc import sharpaikit_pb2
from .errors import SharpAIKitError


class RAG:
    """Client for RAG operations."""
    
    def __init__(self, client: UnifiedGrpcClient):
        self._client = client
        self._stub = None
    
    def _get_stub(self):
        """Get RAGService stub"""
        if self._stub is None:
            self._stub = self._client._get_stub("RAGService")
        return self._stub
    
    def create(self, rag_id: str, api_key: str,
               base_url: str = "https://api.openai.com/v1",
               model: str = "gpt-3.5-turbo",
               top_k: int = 3,
               system_prompt_template: Optional[str] = None,
               options: Optional[dict] = None) -> bool:
        """Create a new RAG engine."""
        request = sharpaikit_pb2.CreateRAGRequest(
            rag_id=rag_id,
            api_key=api_key,
            base_url=base_url,
            model=model,
            top_k=top_k,
            system_prompt_template=system_prompt_template or "",
            options=options or {}
        )
        try:
            response = self._get_stub().CreateRAG(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to create RAG: {str(e)}", "RAG_ERROR") from e
    
    def index_content(self, rag_id: str, content: str, 
                     metadata: Optional[dict] = None) -> int:
        """Index content into RAG."""
        request = sharpaikit_pb2.IndexContentRequest(
            rag_id=rag_id,
            Content=content,
            Metadata=metadata or {}
        )
        try:
            response = self._get_stub().IndexContent(request)
            if response.Success:
                return response.ChunksIndexed
            raise SharpAIKitError(response.error or "Unknown error", "RAG_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to index content: {str(e)}", "RAG_ERROR") from e
    
    def index_document(self, rag_id: str, file_path: str) -> int:
        """Index a document file."""
        request = sharpaikit_pb2.IndexDocumentRequest(
            rag_id=rag_id,
            FilePath=file_path
        )
        try:
            response = self._get_stub().IndexDocument(request)
            if response.Success:
                return response.ChunksIndexed
            raise SharpAIKitError(response.error or "Unknown error", "RAG_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to index document: {str(e)}", "RAG_ERROR") from e
    
    def index_directory(self, rag_id: str, directory_path: str, 
                       search_pattern: str = "*.txt") -> dict:
        """Index all files in a directory."""
        request = sharpaikit_pb2.IndexDirectoryRequest(
            rag_id=rag_id,
            DirectoryPath=directory_path,
            SearchPattern=search_pattern
        )
        try:
            response = self._get_stub().IndexDirectory(request)
            if response.Success:
                return {
                    "files_indexed": response.files_indexed,
                    "total_chunks": response.total_chunks
                }
            raise SharpAIKitError(response.error or "Unknown error", "RAG_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to index directory: {str(e)}", "RAG_ERROR") from e
    
    def ask(self, rag_id: str, question: str) -> dict:
        """Ask a question using RAG."""
        request = sharpaikit_pb2.AskRequest(
            rag_id=rag_id,
            Question=question
        )
        try:
            response = self._get_stub().Ask(request)
            if response.Success:
                return {
                    "answer": response.answer,
                    "retrieved_docs": [
                        SearchResult(
                            document={
                                "content": doc.document.content,
                                "metadata": dict(doc.document.metadata)
                            },
                            similarity=doc.similarity
                        )
                        for doc in response.retrieved_docs
                    ]
                }
            raise SharpAIKitError(response.error or "Unknown error", "RAG_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to ask question: {str(e)}", "RAG_ERROR") from e
    
    def ask_stream(self, rag_id: str, question: str) -> Iterator[dict]:
        """Ask a question with streaming response."""
        request = sharpaikit_pb2.AskRequest(
            rag_id=rag_id,
            Question=question
        )
        try:
            for chunk in self._get_stub().AskStream(request):
                if chunk.HasField("answer_chunk"):
                    yield {"answer_chunk": chunk.answer_chunk}
                elif chunk.HasField("retrieved_doc"):
                    doc = chunk.retrieved_doc
                    yield {
                        "retrieved_doc": SearchResult(
                            document={
                                "content": doc.document.content,
                                "metadata": dict(doc.document.metadata)
                            },
                            similarity=doc.similarity
                        )
                    }
                elif chunk.HasField("error"):
                    raise SharpAIKitError(chunk.error, "RAG_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to ask question stream: {str(e)}", "RAG_ERROR") from e
    
    def retrieve(self, rag_id: str, query: str, top_k: Optional[int] = None) -> List[SearchResult]:
        """Retrieve relevant documents without generating answer."""
        request = sharpaikit_pb2.RetrieveRequest(
            rag_id=rag_id,
            Query=query,
            top_k=top_k or 0
        )
        try:
            response = self._get_stub().Retrieve(request)
            if response.Success:
                return [
                    SearchResult(
                        document={
                            "content": doc.document.content,
                            "metadata": dict(doc.document.metadata)
                        },
                        similarity=doc.similarity
                    )
                    for doc in response.results
                ]
            raise SharpAIKitError(response.error or "Unknown error", "RAG_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to retrieve: {str(e)}", "RAG_ERROR") from e
    
    def clear_index(self, rag_id: str) -> bool:
        """Clear all indexed documents."""
        request = sharpaikit_pb2.ClearIndexRequest(RagId=rag_id)
        try:
            response = self._get_stub().ClearIndex(request)
            return response.success
        except Exception as e:
            raise SharpAIKitError(f"Failed to clear index: {str(e)}", "RAG_ERROR") from e
    
    def get_document_count(self, rag_id: str) -> int:
        """Get the number of indexed documents."""
        request = sharpaikit_pb2.GetDocumentCountRequest(RagId=rag_id)
        try:
            response = self._get_stub().GetDocumentCount(request)
            if response.Success:
                return response.count
            raise SharpAIKitError(response.error or "Unknown error", "RAG_ERROR")
        except Exception as e:
            if isinstance(e, SharpAIKitError):
                raise
            raise SharpAIKitError(f"Failed to get document count: {str(e)}", "RAG_ERROR") from e

