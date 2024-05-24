using Microsoft.AspNetCore.Mvc;
using TafutaLib;

//Load environment variables from the AZD Environment



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

app.MapGet("/apiOperations/search", async (ApiDescriptionService apiDescriptionService, [FromQuery] string query, HttpResponse response ) =>
{
        // Transform the search results into a HTML list
        var results = await apiDescriptionService.Search(query);
        response.ContentType = "text/html";
        response.StatusCode = 200;
        
        foreach (var result in results)
        {
            await response.WriteAsync($"<details>");
            var apiHost = new Uri(result.ApiDescriptionUrl).Host;   
            await response.WriteAsync($"<summary>{apiHost} - {result.HttpMethod} {result.UriTemplate}</summary>");
            await response.WriteAsync($"<h4>{result.Summary}</h4>");
            await response.WriteAsync($"<p>{result.Description}</p>");
            await response.WriteAsync($"<link href='{result.ApiDescriptionUrl}' >{result.ApiDescriptionUrl}</p>");
            await response.WriteAsync($"</details>");
        }
        
        return ;
});

app.MapGet("/search", async (ApiDescriptionService apiDescriptionService, HttpResponse response) =>
{
 var assembly = typeof(Program).Assembly;
    using var stream = assembly.GetManifestResourceStream("api.search.html");
    if (stream == null) {
        response.StatusCode = 404;
        return;
    }
    response.ContentType = "text/html";
    response.StatusCode = 200;
    await stream.CopyToAsync(response.Body);
});



app.Run();
