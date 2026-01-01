using Grpc.Core;
using SharpAIKit.RAG;
using SharpAIKit.LLM;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for RAG operations
/// </summary>
public class RAGServiceImpl : RAGService.RAGServiceBase
{
    private readonly ILogger<RAGServiceImpl> _logger;
    private readonly ConcurrentDictionary<string, RagEngine> _ragEngines = new();

    public RAGServiceImpl(ILogger<RAGServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<CreateRAGResponse> CreateRAG(CreateRAGRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RagId))
            {
                return Task.FromResult(new CreateRAGResponse
                {
                    Success = false,
                    Error = "RagId is required"
                });
            }

            if (string.IsNullOrEmpty(request.ApiKey))
            {
                return Task.FromResult(new CreateRAGResponse
                {
                    Success = false,
                    Error = "ApiKey is required"
                });
            }

            var llmClient = LLMClientFactory.Create(
                request.ApiKey,
                request.BaseUrl ?? "https://api.openai.com/v1",
                request.Model ?? "gpt-3.5-turbo",
                _logger);

            var rag = new RagEngine(llmClient)
            {
                TopK = request.TopK > 0 ? request.TopK : 3,
                SystemPromptTemplate = request.SystemPromptTemplate ?? """
                    You are a professional AI assistant. Please answer the user's question based on the following reference materials.
                    If the reference materials don't contain relevant information, please inform the user honestly.

                    Reference Materials:
                    {context}
                    """
            };

            _ragEngines[request.RagId] = rag;
            _logger.LogInformation("RAG engine created: {RagId}", request.RagId);

            return Task.FromResult(new CreateRAGResponse
            {
                Success = true,
                RagId = request.RagId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RAG engine: {RagId}", request.RagId);
            return Task.FromResult(new CreateRAGResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override async Task<IndexContentResponse> IndexContent(IndexContentRequest request, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                return new IndexContentResponse
                {
                    Success = false,
                    Error = $"RAG engine {request.RagId} not found"
                };
            }

            var metadata = request.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? null;
            await rag.IndexContentAsync(request.Content, metadata);

            return new IndexContentResponse
            {
                Success = true,
                ChunksIndexed = 1 // Simplified - actual chunks depend on text splitter
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing content: {RagId}", request.RagId);
            return new IndexContentResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<IndexDocumentResponse> IndexDocument(IndexDocumentRequest request, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                return new IndexDocumentResponse
                {
                    Success = false,
                    Error = $"RAG engine {request.RagId} not found"
                };
            }

            await rag.IndexDocumentAsync(request.FilePath);

            return new IndexDocumentResponse
            {
                Success = true,
                ChunksIndexed = 1 // Simplified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document: {RagId}", request.RagId);
            return new IndexDocumentResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<IndexDirectoryResponse> IndexDirectory(IndexDirectoryRequest request, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                return new IndexDirectoryResponse
                {
                    Success = false,
                    Error = $"RAG engine {request.RagId} not found"
                };
            }

            await rag.IndexDirectoryAsync(request.DirectoryPath, request.SearchPattern);

            return new IndexDirectoryResponse
            {
                Success = true,
                FilesIndexed = 1, // Simplified
                TotalChunks = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing directory: {RagId}", request.RagId);
            return new IndexDirectoryResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<AskResponse> Ask(AskRequest request, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                return new AskResponse
                {
                    Success = false,
                    Error = $"RAG engine {request.RagId} not found"
                };
            }

            var answer = await rag.AskAsync(request.Question);
            var retrieved = await rag.RetrieveAsync(request.Question, rag.TopK);

            var response = new AskResponse
            {
                Success = true,
                Answer = answer
            };

            foreach (var result in retrieved)
            {
                var grpcDoc = new VectorDocument
                {
                    Content = result.Document.Content
                };
                grpcDoc.Embedding.AddRange(result.Document.Embedding);
                foreach (var kvp in result.Document.Metadata)
                {
                    grpcDoc.Metadata[kvp.Key] = kvp.Value;
                }

                response.RetrievedDocs.Add(new SearchResult
                {
                    Document = grpcDoc,
                    Similarity = result.Score
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking RAG: {RagId}", request.RagId);
            return new AskResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task AskStream(AskRequest request, IServerStreamWriter<AskStreamChunk> responseStream, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                await responseStream.WriteAsync(new AskStreamChunk
                {
                    Error = $"RAG engine {request.RagId} not found"
                });
                return;
            }

            // Send retrieved docs first
            var retrieved = await rag.RetrieveAsync(request.Question, rag.TopK);
            foreach (var result in retrieved)
            {
                var grpcDoc = new VectorDocument
                {
                    Content = result.Document.Content
                };
                grpcDoc.Embedding.AddRange(result.Document.Embedding);
                foreach (var kvp in result.Document.Metadata)
                {
                    grpcDoc.Metadata[kvp.Key] = kvp.Value;
                }

                await responseStream.WriteAsync(new AskStreamChunk
                {
                    RetrievedDoc = new SearchResult
                    {
                        Document = grpcDoc,
                        Similarity = result.Score
                    }
                });
            }

            // Stream answer
            await foreach (var chunk in rag.AskStreamAsync(request.Question))
            {
                await responseStream.WriteAsync(new AskStreamChunk
                {
                    TextChunk = chunk
                });
            }

            await responseStream.WriteAsync(new AskStreamChunk { Done = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AskStream: {RagId}", request.RagId);
            await responseStream.WriteAsync(new AskStreamChunk
            {
                Error = ex.Message
            });
        }
    }

    public override async Task<RetrieveResponse> Retrieve(RetrieveRequest request, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                return new RetrieveResponse
                {
                    Success = false,
                    Error = $"RAG engine {request.RagId} not found"
                };
            }

            var results = await rag.RetrieveAsync(request.Query, request.TopK > 0 ? request.TopK : rag.TopK);

            var response = new RetrieveResponse { Success = true };
            foreach (var result in results)
            {
                var grpcDoc = new VectorDocument
                {
                    Content = result.Document.Content
                };
                grpcDoc.Embedding.AddRange(result.Document.Embedding);
                foreach (var kvp in result.Document.Metadata)
                {
                    grpcDoc.Metadata[kvp.Key] = kvp.Value;
                }

                response.Results.Add(new SearchResult
                {
                    Document = grpcDoc,
                    Similarity = result.Score
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from RAG: {RagId}", request.RagId);
            return new RetrieveResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<ClearIndexResponse> ClearIndex(ClearIndexRequest request, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                return new ClearIndexResponse
                {
                    Success = false,
                    Error = $"RAG engine {request.RagId} not found"
                };
            }

            await rag.ClearIndexAsync();

            return new ClearIndexResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing RAG index: {RagId}", request.RagId);
            return new ClearIndexResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<GetDocumentCountResponse> GetDocumentCount(GetDocumentCountRequest request, ServerCallContext context)
    {
        try
        {
            if (!_ragEngines.TryGetValue(request.RagId, out var rag))
            {
                return new GetDocumentCountResponse
                {
                    Success = false,
                    Error = $"RAG engine {request.RagId} not found"
                };
            }

            var count = await rag.GetDocumentCountAsync();

            return new GetDocumentCountResponse
            {
                Success = true,
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count: {RagId}", request.RagId);
            return new GetDocumentCountResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

