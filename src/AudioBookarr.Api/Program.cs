using AudioBookarr.Core.Integrations;
using AudioBookarr.Core.Library;
using AudioBookarr.Core.Metadata;
using AudioBookarr.Core.System;
using AudioBookarr.Infrastructure;
using AudioBookarr.Infrastructure.Integrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAudioBookarrInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api/v1");

api.MapGet("/system/status", async (
    AppPaths paths,
    IPathValidator validator,
    CancellationToken cancellationToken) =>
{
    var pathHealth = await validator.ValidateAsync(paths, cancellationToken);
    var warnings = pathHealth
        .Where(path => !path.Exists || !path.Writable)
        .Select(path => $"{path.Path}: {path.Message ?? "Path is not healthy."}")
        .ToList();

    return Results.Ok(new SystemStatus(
        "AudioBookarr",
        typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.1.0",
        paths,
        pathHealth,
        warnings));
});

api.MapGet("/metadata/search", async (
    string q,
    string? author,
    string? narrator,
    string? isbn,
    string? asin,
    int? limit,
    IMetadataSearchService metadata,
    CancellationToken cancellationToken) =>
{
    var results = await metadata.SearchAsync(
        new MetadataSearchRequest(q, author, narrator, isbn, asin, limit ?? MetadataSearchLimits.Default),
        cancellationToken);

    return Results.Ok(results);
});

api.MapGet("/library", async (ILibraryService library, CancellationToken cancellationToken) =>
    Results.Ok(await library.GetStateAsync(cancellationToken)));

api.MapPost("/library/books", async (
    AddBookRequest request,
    ILibraryService library,
    CancellationToken cancellationToken) =>
{
    var book = await library.AddBookAsync(request, cancellationToken);
    return Results.Created($"/api/v1/library/books/{book.Id}", book);
});

api.MapPut("/library/books/{bookId}/monitor", async (
    string bookId,
    MonitorRequest request,
    ILibraryService library,
    CancellationToken cancellationToken) =>
{
    var book = await library.SetMonitoringAsync(bookId, request.Monitored, cancellationToken);
    return book is null ? Results.NotFound() : Results.Ok(book);
});

api.MapGet("/integrations", async (IIntegrationService integrations, CancellationToken cancellationToken) =>
    Results.Ok(await integrations.GetStateAsync(cancellationToken)));

api.MapPost("/integrations/download-clients", async (
    DownloadClientConfig request,
    IIntegrationService integrations,
    CancellationToken cancellationToken) =>
{
    var client = await integrations.UpsertDownloadClientAsync(request, cancellationToken);
    return Results.Ok(client);
});

api.MapPost("/integrations/download-clients/test", async (
    DownloadClientConfig request,
    IIntegrationService integrations,
    CancellationToken cancellationToken) =>
{
    var result = await integrations.TestDownloadClientAsync(request, cancellationToken);
    return Results.Ok(result);
});

api.MapPost("/integrations/indexers", async (
    IndexerConfig request,
    IIntegrationService integrations,
    CancellationToken cancellationToken) =>
{
    var indexer = await integrations.UpsertIndexerAsync(request, cancellationToken);
    return Results.Ok(indexer);
});

api.MapPost("/integrations/indexers/test", async (
    IndexerConfig request,
    IIntegrationService integrations,
    CancellationToken cancellationToken) =>
{
    var result = await integrations.TestIndexerAsync(request, cancellationToken);
    return Results.Ok(result);
});

app.MapFallbackToFile("index.html");

app.Run();

public sealed record MonitorRequest(bool Monitored);
