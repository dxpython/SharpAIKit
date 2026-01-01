using Grpc.Core;
using SharpAIKit.DocumentLoader;
using Microsoft.Extensions.Logging;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for DocumentLoader operations
/// </summary>
public class DocumentLoaderServiceImpl : DocumentLoaderService.DocumentLoaderServiceBase
{
    private readonly ILogger<DocumentLoaderServiceImpl> _logger;

    public DocumentLoaderServiceImpl(ILogger<DocumentLoaderServiceImpl> logger)
    {
        _logger = logger;
    }

    public override async Task<LoadTextResponse> LoadText(LoadTextRequest request, ServerCallContext context)
    {
        try
        {
            var encoding = System.Text.Encoding.GetEncoding(request.Encoding ?? "utf-8");
            var content = await File.ReadAllTextAsync(request.FilePath, encoding, context.CancellationToken);

            return new LoadTextResponse
            {
                Success = true,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading text file: {FilePath}", request.FilePath);
            return new LoadTextResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<LoadCSVResponse> LoadCSV(LoadCSVRequest request, ServerCallContext context)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(request.FilePath, context.CancellationToken);
            var delimiter = request.Delimiter ?? ",";
            
            var response = new LoadCSVResponse { Success = true };
            
            if (lines.Length == 0)
            {
                return response;
            }

            string[]? headers = null;
            int startIndex = 0;

            if (request.HasHeader && lines.Length > 0)
            {
                headers = lines[0].Split(delimiter);
                response.Headers.AddRange(headers);
                startIndex = 1;
            }

            for (int i = startIndex; i < lines.Length; i++)
            {
                var values = lines[i].Split(delimiter);
                var row = new CSVRow();
                
                if (headers != null)
                {
                    for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
                    {
                        row.Values[headers[j]] = values[j];
                    }
                }
                else
                {
                    for (int j = 0; j < values.Length; j++)
                    {
                        row.Values[$"Column{j}"] = values[j];
                    }
                }
                
                response.Rows.Add(row);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading CSV file: {FilePath}", request.FilePath);
            return new LoadCSVResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<LoadJSONResponse> LoadJSON(LoadJSONRequest request, ServerCallContext context)
    {
        try
        {
            var content = await File.ReadAllTextAsync(request.FilePath, context.CancellationToken);

            return new LoadJSONResponse
            {
                Success = true,
                Json = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON file: {FilePath}", request.FilePath);
            return new LoadJSONResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<LoadMarkdownResponse> LoadMarkdown(LoadMarkdownRequest request, ServerCallContext context)
    {
        try
        {
            var content = await File.ReadAllTextAsync(request.FilePath, context.CancellationToken);

            return new LoadMarkdownResponse
            {
                Success = true,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Markdown file: {FilePath}", request.FilePath);
            return new LoadMarkdownResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<LoadWebResponse> LoadWeb(LoadWebRequest request, ServerCallContext context)
    {
        try
        {
            using var httpClient = new HttpClient();
            
            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var content = await httpClient.GetStringAsync(request.Url, context.CancellationToken);

            return new LoadWebResponse
            {
                Success = true,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading web content: {Url}", request.Url);
            return new LoadWebResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<LoadDirectoryResponse> LoadDirectory(LoadDirectoryRequest request, ServerCallContext context)
    {
        try
        {
            var searchOption = request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(request.DirectoryPath, request.SearchPattern ?? "*.*", searchOption);

            var response = new LoadDirectoryResponse { Success = true };

            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file, context.CancellationToken);
                    var docInfo = new DocumentInfo
                    {
                        FilePath = file,
                        Content = content
                    };
                    docInfo.Metadata["filename"] = Path.GetFileName(file);
                    docInfo.Metadata["extension"] = Path.GetExtension(file);
                    docInfo.Metadata["size"] = new FileInfo(file).Length.ToString();

                    response.Documents.Add(docInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading file: {FilePath}", file);
                    // Continue with other files
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading directory: {DirectoryPath}", request.DirectoryPath);
            return new LoadDirectoryResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

