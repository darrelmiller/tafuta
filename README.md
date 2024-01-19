# Tafuta

This is an API that provides programmatic access to index API descriptions and query that index.

The API can be started using 

```shell
dotnet run --project .\src\api\api.csproj
```

To index API descriptions the Azure OpenAI service is required. The following Environment variables must be set:

AZURE_OPENAI_KEY
AZURE_OPENAI_DEPLOYMENTNAME
AZURE_OPENAI_ENDPOINT

Currently the index is stored in volatile memory, so when the API process is stopped all the vectors are lost.

See the ./src/api/api.http file for examples of how to call the API.