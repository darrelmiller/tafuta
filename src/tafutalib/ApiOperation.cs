using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Microsoft.OpenApi.Models;

namespace TafutaLib;

public class ApiOperation
{
    public string? ApiDescriptionUrl { get; set; }
    public ApiParameterDescription[]? ParameterDescriptions { get; set; }
    public string? RequestBodyDescription { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? UriTemplate { get; set; }
    public string? HttpMethod { get; set; }


    public static ApiOperation Create(string apiDescriptionUrl, KeyValuePair<string, OpenApiPathItem> path, KeyValuePair<OperationType, OpenApiOperation> operation)
    {
        return new ApiOperation
        {
            ApiDescriptionUrl = apiDescriptionUrl,
            UriTemplate = path.Key,
            HttpMethod = operation.Key.ToString(),
            Summary = operation.Value.Summary ?? operation.Value.Description.Substring(0, Math.Min(100, operation.Value.Description.Length)),
            Description = operation.Value.Description,
            RequestBodyDescription = operation.Value.RequestBody?.Description,
            ParameterDescriptions = operation.Value.Parameters.Select(p => new ApiParameterDescription
            {
                Name = p.Name,
                Description = p.Description,
                Type = p.Schema.Type
            }).ToArray()
        };
    }


    public static ApiOperation? Parse(string text)
    {
        return JsonSerializer.Deserialize<ApiOperation>(text);
    }

    public static List<ApiOperation> ParseList(string text)
    {
        var opList = new List<ApiOperation>();
        var list = JsonDocument.Parse(text);
        if (list is null)
        {
            return opList;
        }
        foreach (var item in list.RootElement.EnumerateArray())
        {
            var jsonOp = item.ToJsonString();
            var op = JsonSerializer.Deserialize<ApiOperation>(jsonOp, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (op is not null)
            {
                opList.Add(op);
            }
        }
        return opList;
    }

    public string OperationKey => ApiDescriptionUrl + "#" + UriTemplate + "#" + HttpMethod;

    public string ToJson()
    {
        var text = JsonSerializer.Serialize(this);
        return text;
    }

    public string CreateText()
    {
        var text = new StringBuilder();
        if (!string.IsNullOrEmpty(Summary))
        {
            text.AppendLine($"Summary: {Summary.ToLower()}");
        }
        if (!string.IsNullOrEmpty(Description))
        {
            text.AppendLine($"Description: {Description.ToLower()}");
        }
        text.AppendLine($"### {HttpMethod} {UriTemplate}");
        if (!string.IsNullOrEmpty(RequestBodyDescription))
        {
            text.AppendLine($"RequestBody {RequestBodyDescription.ToLower()}");
        }
        if (ParameterDescriptions != null && ParameterDescriptions.Any())
        {
            text.AppendLine($"Parameters:");
            foreach (var parameter in ParameterDescriptions)
            {
                text.AppendLine($"- Name: {parameter.Name}");
                text.AppendLine($"  Type: {parameter.Type?.ToLower()}");
                if (!string.IsNullOrEmpty(parameter.Description)) {
                    text.AppendLine($"  Description: {parameter.Description.ToLower()}");
                }
            }
        }

        return text.ToString();
    }
}
