using AudioBookarr.Core.Library;

namespace AudioBookarr.Core.Metadata;

public sealed record MetadataSearchRequest(
    string Query,
    string? Author = null,
    string? Narrator = null,
    string? Isbn = null,
    string? Asin = null,
    int Limit = 10);

public sealed record MetadataFieldSource(
    string Field,
    string Provider);

public sealed record MetadataSearchResult(
    string Provider,
    string ProviderId,
    string Title,
    string? Subtitle,
    IReadOnlyList<Author> Authors,
    IReadOnlyList<BookEdition> Editions,
    IReadOnlyList<SeriesInfo> Series,
    string? Description,
    string? CoverUrl,
    IReadOnlyList<string> Genres,
    double Score,
    IReadOnlyList<MetadataFieldSource> FieldSources);

public interface IMetadataProvider
{
    string Name { get; }

    int Priority { get; }

    Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        MetadataSearchRequest request,
        CancellationToken cancellationToken);
}

public interface IMetadataSearchService
{
    Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        MetadataSearchRequest request,
        CancellationToken cancellationToken);
}

public sealed class MetadataSearchService(IEnumerable<IMetadataProvider> providers) : IMetadataSearchService
{
    private readonly IReadOnlyList<IMetadataProvider> _providers = providers
        .OrderBy(provider => provider.Priority)
        .ToList();

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        MetadataSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) &&
            string.IsNullOrWhiteSpace(request.Isbn) &&
            string.IsNullOrWhiteSpace(request.Asin))
        {
            return [];
        }

        var searches = _providers.Select(provider => SearchProviderAsync(provider, request, cancellationToken));
        var providerResults = await Task.WhenAll(searches);

        return providerResults
            .SelectMany(results => results)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => _providers.FirstOrDefault(provider => provider.Name == result.Provider)?.Priority ?? int.MaxValue)
            .Take(request.Limit)
            .ToList();
    }

    private static async Task<IReadOnlyList<MetadataSearchResult>> SearchProviderAsync(
        IMetadataProvider provider,
        MetadataSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(request, cancellationToken);
        }
        catch
        {
            // Provider outages should not make add/search unusable.
            return [];
        }
    }
}
