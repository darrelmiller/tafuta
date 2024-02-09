using Microsoft.AspNetCore.Mvc;
using TafutaLib;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ApiDescriptionService>();

var app = builder.Build();
var defaultCollection = "apiindex2";

app.UseHttpsRedirection();

// See ../../apidesc/main.tsp for API Description

app.MapPost("/apiDescriptions", async (ApiDescriptionService apiDescriptionService, 
                                        [FromQuery] string apiDescriptionUrl,
                                        [FromQuery] string? collection = null) =>
{
    var created = await apiDescriptionService.AddApiDescription(apiDescriptionUrl,collection ?? defaultCollection);

    if (created)
    {
        return Results.Created();
    } else {
        return Results.Ok();
    }
});


app.MapGet("/apiOperations/search", (ApiDescriptionService apiDescriptionService, 
                                        [FromQuery] string query,
                                        [FromQuery] string? collection = null) =>
{
        return apiDescriptionService.Search(collection ?? defaultCollection,query);
});

app.Run();
