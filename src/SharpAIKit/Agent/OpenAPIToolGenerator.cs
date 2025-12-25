using System.Text.Json;

namespace SharpAIKit.Agent;

/// <summary>
/// Generates tool definitions from OpenAPI/Swagger specifications.
/// </summary>
public class OpenAPIToolGenerator
{
    /// <summary>
    /// Generates tool definitions from an OpenAPI spec.
    /// </summary>
    public static List<ToolDefinition> GenerateFromOpenAPI(string openApiJson)
    {
        var tools = new List<ToolDefinition>();
        
        try
        {
            using var doc = JsonDocument.Parse(openApiJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        if (method.Name is "get" or "post" or "put" or "delete" or "patch")
                        {
                            var tool = CreateToolFromOperation(path.Name, method.Name, method.Value);
                            if (tool != null)
                            {
                                tools.Add(tool);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse OpenAPI spec: {ex.Message}", ex);
        }

        return tools;
    }

    private static ToolDefinition? CreateToolFromOperation(string path, string method, JsonElement operation)
    {
        var operationId = operation.TryGetProperty("operationId", out var opId) 
            ? opId.GetString() 
            : $"{method}_{path.Replace("/", "_").Replace("{", "").Replace("}", "")}";

        var summary = operation.TryGetProperty("summary", out var sum) 
            ? sum.GetString() ?? "" 
            : "";

        var description = operation.TryGetProperty("description", out var desc) 
            ? desc.GetString() ?? summary 
            : summary;

        var parameters = new List<ToolParameter>();
        
        if (operation.TryGetProperty("parameters", out var paramsElement))
        {
            foreach (var param in paramsElement.EnumerateArray())
            {
                var paramName = param.TryGetProperty("name", out var name) ? name.GetString() : "";
                var paramDesc = param.TryGetProperty("description", out var pDesc) ? pDesc.GetString() : "";
                var required = param.TryGetProperty("required", out var req) && req.GetBoolean();
                var paramType = param.TryGetProperty("schema", out var schema) && schema.TryGetProperty("type", out var type)
                    ? type.GetString() ?? "string"
                    : "string";

                if (!string.IsNullOrEmpty(paramName))
                {
                    parameters.Add(new ToolParameter
                    {
                        Name = paramName,
                        Type = MapOpenAPIType(paramType),
                        Description = paramDesc ?? "",
                        Required = required
                    });
                }
            }
        }

        // Handle request body for POST/PUT
        if ((method == "post" || method == "put" || method == "patch") && 
            operation.TryGetProperty("requestBody", out var requestBody))
        {
            parameters.Add(new ToolParameter
            {
                Name = "body",
                Type = "object",
                Description = "Request body",
                Required = true
            });
        }

        return new ToolDefinition
        {
            Name = operationId ?? $"{method}_{path}",
            Description = description ?? $"Call {method.ToUpper()} {path}",
            Parameters = parameters,
            Execute = async args =>
            {
                // This would need to be implemented to actually call the API
                return $"API call to {method.ToUpper()} {path} with args: {JsonSerializer.Serialize(args)}";
            }
        };
    }

    private static string MapOpenAPIType(string? openApiType)
    {
        return openApiType?.ToLower() switch
        {
            "integer" or "int32" or "int64" => "number",
            "number" or "float" or "double" => "number",
            "boolean" => "boolean",
            "array" => "array",
            "object" => "object",
            _ => "string"
        };
    }

    /// <summary>
    /// Generates tool definitions from a Swagger URL.
    /// </summary>
    public static async Task<List<ToolDefinition>> GenerateFromUrlAsync(string swaggerUrl, HttpClient? httpClient = null)
    {
        var client = httpClient ?? new HttpClient();
        var json = await client.GetStringAsync(swaggerUrl);
        return GenerateFromOpenAPI(json);
    }
}

