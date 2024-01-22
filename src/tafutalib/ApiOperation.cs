using System.Text.Json;

namespace TafutaLib;

public class ApiOperation {
    public string? ApiDescriptionUrl { get; set; }
    public ApiParameterDescription[]? ParameterDescriptions { get; set; }
    public string? RequestBodyDescription { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? UriTemplate { get; set; }
    public string? HttpMethod { get; set; }

    internal static ApiOperation? Parse(string text)
    {
        return JsonSerializer.Deserialize<ApiOperation>(text);    
    }

    public string OperationKey => ApiDescriptionUrl + "#" + UriTemplate + "#" + HttpMethod;

    public string ToJson() {
        var text = JsonSerializer.Serialize(this);
        return text;
    }
}
