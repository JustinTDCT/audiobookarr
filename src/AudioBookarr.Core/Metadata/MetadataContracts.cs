using AudioBookarr.Core.Library;

namespace AudioBookarr.Core.Metadata;

public enum MetadataSearchField
{
    Author,
    Title,
    Series
}

public sealed record MetadataSearchRequest(
    string Query,
    MetadataSearchField Field = MetadataSearchField.Author,
    string? Author = null,
    string? Narrator = null,
    string? Isbn = null,
    string? Asin = null,
    int Limit = 50);

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

public static class MetadataSearchLimits
{
    public const int Default = 50;

    public const int All = 0;

    public const int Maximum = 500;
}

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
        var parsedRequest = ParseFieldSuffix(request);

        if (string.IsNullOrWhiteSpace(parsedRequest.Query) &&
            string.IsNullOrWhiteSpace(request.Isbn) &&
            string.IsNullOrWhiteSpace(request.Asin))
        {
            return [];
        }

        var normalizedLimit = parsedRequest.Limit == MetadataSearchLimits.All
            ? MetadataSearchLimits.Maximum
            : Math.Clamp(parsedRequest.Limit, 1, MetadataSearchLimits.Maximum);
        var normalizedRequest = parsedRequest with { Limit = normalizedLimit };
        var searches = _providers.Select(provider => SearchProviderAsync(provider, normalizedRequest, cancellationToken));
        var providerResults = await Task.WhenAll(searches);

        return providerResults
            .SelectMany(results => results)
            .Where(result => MatchesSearchField(result, normalizedRequest))
            .DistinctBy(result => $"{result.Provider}:{result.ProviderId}")
            .OrderByDescending(result => result.Score)
            .ThenBy(result => _providers.FirstOrDefault(provider => provider.Name == result.Provider)?.Priority ?? int.MaxValue)
            .Take(normalizedRequest.Limit)
            .ToList();
    }

    private static MetadataSearchRequest ParseFieldSuffix(MetadataSearchRequest request)
    {
        var query = request.Query.Trim();
        var suffixIndex = query.LastIndexOf(':');

        if (suffixIndex <= 0 || suffixIndex == query.Length - 1)
        {
            return request with { Query = query };
        }

        var suffix = query[(suffixIndex + 1)..];

        if (!Enum.TryParse<MetadataSearchField>(suffix, true, out var field))
        {
            return request with { Query = query };
        }

        return request with
        {
            Query = query[..suffixIndex].Trim(),
            Field = field
        };
    }

    private static bool MatchesSearchField(
        MetadataSearchResult result,
        MetadataSearchRequest request)
    {
        var query = request.Query;

        return request.Field switch
        {
            MetadataSearchField.Author => result.Authors.Any(author => Contains(author.Name, query)),
            MetadataSearchField.Title => Contains(result.Title, query) || Contains(result.Subtitle, query),
            MetadataSearchField.Series => result.Series.Any(series => Contains(series.Name, query)),
            _ => true
        };
    }

    private static bool Contains(string? value, string query) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

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
