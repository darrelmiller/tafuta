import "@typespec/http";
using TypeSpec.Http;

@service({
  title: "Tafuta API",
  version: "0.0.1",
  description: "Semantic index of HTTP API operations"
})
@server("https://example.com", "local server endpoint")
namespace Tafuta;

@doc("Adds or updates operations from an API Description in the search index")
@route("/ApiDescriptions")
@post op AddApiDescription(@query() apiDescriptionUrl: string) : OkResponse | CreatedResponse;

@doc("Searches the index for API Entries matching the query")
@route("/ApiOperations/search")
@get op Search(@query() query: string) : ApiOperation[];

// This model is specifically intended to be used for semantic indexing of API operations
// It is not intended to be complete for the purpose of executing the API operations
// The apiDescriptionUrl is used to retrieve the full API Description
// When ingesting the API Description, description fields should be synthesized from information in the API Description
model ApiOperation {
    apiDescriptionUrl: string;
    parameterDescriptions?: ParameterDescription[];
    requestBodyDescription?: string;
    summary?: string;
    description?: string;
    uriTemplate: string;
    httpMethod: string;
}

model ParameterDescription {
    name: string;
    description?: string;
    type?: string;  // Combination of type and format, if present
}