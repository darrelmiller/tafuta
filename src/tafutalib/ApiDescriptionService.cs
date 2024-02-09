
using System.Net;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Microsoft.VisualBasic;

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



    public async Task<bool> AddApiDescription(string collection, string apiDescriptionUrl)
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
                var apiOperation = ApiOperation.Create(apiDescriptionUrl, path, operation);
                logger.LogInformation($"Adding {apiOperation.OperationKey}");
                var text = apiOperation.CreateText();
                logger.LogDebug(text);
                // Store ApiOperation
                await _memoryStore.SaveReferenceAsync(
                        collection: collection,
                        text: text,
                        externalId: apiOperation.OperationKey,
                        externalSourceName: apiDescription.Info.Title,
                        additionalMetadata: JsonSerializer.Serialize(apiOperation));
            }
        }   

        
        return true; // if created
    }


    public async Task<ApiOperation[]> Search(string collection, string query, int limit = 5)
    {
        var results =  _memoryStore.SearchAsync(collection,query, limit);
        var apiOperations = new List<ApiOperation>();
        await foreach (var result in results) {
            var op = ApiOperation.Parse(result.Metadata.AdditionalMetadata);
            if ( op is not null) {
                apiOperations.Add(op);
            }
        }
        return apiOperations.ToArray();        
    }
}
