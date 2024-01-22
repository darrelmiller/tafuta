
using System.Net;
using System.Reflection.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.OpenApi.Readers;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;

namespace TafutaLib;

public class ApiDescriptionService
{
    private readonly ILogger<ApiDescriptionService> logger;
    HttpClient _httpClient;

#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    ISemanticTextMemory _memoryStore;
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public ApiDescriptionService(ILogger<ApiDescriptionService> logger)
    {
        this.logger = logger;        
        _httpClient = new HttpClient(new RetryHandler { InnerHandler = new HttpClientHandler()}  );

        #pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        #pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        #pragma warning disable SKEXP0052 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        #pragma warning disable SKEXP0026 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var azureOpenAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        if (string.IsNullOrEmpty(azureOpenAIKey)) {
            throw new Exception("AZURE_OPENAI_KEY environment variable is not set");
        }
        var azureOpenAIDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENTNAME");
        if (string.IsNullOrEmpty(azureOpenAIDeploymentName)) {
            throw new Exception("AZURE_OPENAI_DEPLOYMENTNAME environment variable is not set");
        }
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        if (string.IsNullOrEmpty(azureOpenAIEndpoint)) {
            throw new Exception("AZURE_OPENAI_ENDPOINT environment variable is not set");
        }
        Console.WriteLine($"Using Azure OpenAI Endpoint {azureOpenAIEndpoint} and Deployment {azureOpenAIDeploymentName}");

        var qdrantEndpoint = Environment.GetEnvironmentVariable("QDRANT_ENDPOINT");
        if (string.IsNullOrEmpty(qdrantEndpoint)) {
            throw new Exception("QDRANT_ENDPOINT environment variable is not set");
        }

        _memoryStore = new MemoryBuilder()
        .WithHttpClient(_httpClient) 
        .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIDeploymentName,azureOpenAIEndpoint,azureOpenAIKey)
        //.WithMemoryStore(new VolatileMemoryStore())
        .WithMemoryStore(new QdrantMemoryStore(qdrantEndpoint,1536))
        .Build();

        #pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        #pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        #pragma warning restore SKEXP0052 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        #pragma warning restore SKEXP0026 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }



    public async Task<bool> AddApiDescription(string apiDescriptionUrl)
    {
        // Dereference apiDescriptionUrl
        var response = await _httpClient.GetAsync(apiDescriptionUrl);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Could not retreive {apiDescriptionUrl}");
        }
        // Parse API Description
        var openApiStream = await response.Content.ReadAsStreamAsync();
        var reader= new OpenApiStreamReader();
        var result = await reader.ReadAsync(openApiStream);
        if (result.OpenApiDiagnostic.Errors.Count > 0)
        {
            throw new Exception("Could not parse {apiDescriptionUrl} due to {result.OpenApiDiagnostic.Errors}");
        }
        
        var apiDescription = result.OpenApiDocument;
        // Loop through paths and operations and create ApiOperation object for each
        foreach (var path in apiDescription.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var apiOperation = new ApiOperation
                {
                    ApiDescriptionUrl = apiDescriptionUrl,
                    UriTemplate = path.Key,
                    HttpMethod = operation.Key.ToString(),
                    Summary = operation.Value.Summary,
                    Description = operation.Value.Description,
                    RequestBodyDescription = operation.Value.RequestBody?.Description,
                    ParameterDescriptions = operation.Value.Parameters.Select(p => new ApiParameterDescription
                    {
                        Name = p.Name,
                        Description = p.Description,
                        Type = p.Schema.Type
                    }).ToArray()
                };
                logger.LogInformation($"Adding {apiOperation.OperationKey}");
                // Store ApiOperation
                await _memoryStore.SaveInformationAsync("apiindex",apiOperation.ToJson(),apiOperation.OperationKey, apiDescription.Info.Title);
            }
        }   

        
        return true; // if created
    }

    public async Task<ApiOperation[]> Search(string query)
    {
        var results =  _memoryStore.SearchAsync("apiindex",query);
        var apiOperations = new List<ApiOperation>();
        await foreach (var result in results) {
            apiOperations.Add(ApiOperation.Parse(result.Metadata.Text));
        }
        return apiOperations.ToArray();        
    }
}
