using Grpc.Core;
using SharpAIKit.Output;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SharpAIKit.Grpc.Services;

/// <summary>
/// gRPC service implementation for OutputParser operations
/// </summary>
public class OutputParserServiceImpl : OutputParserService.OutputParserServiceBase
{
    private readonly ILogger<OutputParserServiceImpl> _logger;

    public OutputParserServiceImpl(ILogger<OutputParserServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<ParseJSONResponse> ParseJSON(ParseJSONRequest request, ServerCallContext context)
    {
        try
        {
            // Try to extract JSON from text
            var jsonText = ExtractJsonFromText(request.Text);
            
            // Validate JSON if schema provided
            if (!string.IsNullOrEmpty(request.Schema))
            {
                // Schema validation would go here (simplified)
            }

            return Task.FromResult(new ParseJSONResponse
            {
                Success = true,
                Json = jsonText
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON");
            return Task.FromResult(new ParseJSONResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<ParseBooleanResponse> ParseBoolean(ParseBooleanRequest request, ServerCallContext context)
    {
        try
        {
            var parser = new BooleanParser();
            var value = parser.Parse(request.Text);

            return Task.FromResult(new ParseBooleanResponse
            {
                Success = true,
                Value = value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing boolean");
            return Task.FromResult(new ParseBooleanResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<ParseListResponse> ParseList(ParseListRequest request, ServerCallContext context)
    {
        try
        {
            var parser = new CommaSeparatedListParser();
            var items = parser.Parse(request.Text);

            var response = new ParseListResponse
            {
                Success = true
            };
            response.Items.AddRange(items);

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing list");
            return Task.FromResult(new ParseListResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<ParseXMLResponse> ParseXML(ParseXMLRequest request, ServerCallContext context)
    {
        try
        {
            var parser = new XMLTagParser(request.RootTag);
            var result = parser.Parse(request.Text);

            // Convert to XML string (simplified)
            var xml = $"<{request.RootTag}>{string.Join("", result.Values.Select(v => $"<item>{v}</item>"))}</{request.RootTag}>";

            return Task.FromResult(new ParseXMLResponse
            {
                Success = true,
                Xml = xml
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing XML");
            return Task.FromResult(new ParseXMLResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public override Task<ParseRegexResponse> ParseRegex(ParseRegexRequest request, ServerCallContext context)
    {
        try
        {
            var parser = new RegexParser();
            parser.AddField("match", request.Pattern);
            var result = parser.Parse(request.Text);

            var response = new ParseRegexResponse
            {
                Success = true
            };

            if (result.TryGetValue("match", out var match))
            {
                response.Match = match;
            }

            // Extract groups (simplified)
            var regex = new System.Text.RegularExpressions.Regex(request.Pattern);
            var regexMatch = regex.Match(request.Text);
            if (regexMatch.Success)
            {
                foreach (System.Text.RegularExpressions.Group group in regexMatch.Groups)
                {
                    response.Groups.Add(group.Value);
                }
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing regex");
            return Task.FromResult(new ParseRegexResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    private string ExtractJsonFromText(string text)
    {
        // Try to find JSON in markdown code blocks
        var jsonBlockPattern = new System.Text.RegularExpressions.Regex(@"```(?:json)?\s*([\s\S]*?)```", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match = jsonBlockPattern.Match(text);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Try to find JSON object
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return text.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        return text.Trim();
    }
}

