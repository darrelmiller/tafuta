using Microsoft.AspNetCore.Mvc;
using TafutaLib;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ApiDescriptionService>();

var app = builder.Build();

app.UseHttpsRedirection();

// See ../../apidesc/main.tsp for API Description

app.MapPost("/apiDescriptions", async (ApiDescriptionService apiDescriptionService, [FromQuery] string apiDescriptionUrl) =>
{
    var created = await apiDescriptionService.AddApiDescription(apiDescriptionUrl);

    if (created)
    {
        return Results.Created();
    } else {
        return Results.Ok();
    }
});


app.MapGet("/apiOperations/search", (ApiDescriptionService apiDescriptionService, [FromQuery] string query) =>
{
        return apiDescriptionService.Search(query);
});

app.Run();
