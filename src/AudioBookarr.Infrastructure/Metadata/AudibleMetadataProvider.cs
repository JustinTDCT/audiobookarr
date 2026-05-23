using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AudioBookarr.Core.Library;
using AudioBookarr.Core.Metadata;

namespace AudioBookarr.Infrastructure.Metadata;

public sealed class AudibleMetadataProvider(HttpClient httpClient) : IMetadataProvider
{
    public string Name => "Audible";

    public int Priority => 10;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        MetadataSearchRequest request,
        CancellationToken cancellationToken)
    {
        var keywords = !string.IsNullOrWhiteSpace(request.Asin)
            ? request.Asin
            : string.Join(' ', new[] { request.Query, request.Author, request.Narrator }.Where(value => !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrWhiteSpace(keywords))
        {
            return [];
        }

        var products = await FetchProductsAsync(keywords, request.Limit, cancellationToken);

        return products
            .Where(product => !string.IsNullOrWhiteSpace(product.Title))
            .Select(ToResult)
            .ToList();
    }

    private async Task<IReadOnlyList<AudibleProduct>> FetchProductsAsync(
        string keywords,
        int limit,
        CancellationToken cancellationToken)
    {
        const int pageSize = 50;
        var responseGroups = "contributors,media,product_attrs,product_desc,product_details,product_extended_attrs,rating,series";
        var products = new List<AudibleProduct>();
        var encodedKeywords = Uri.EscapeDataString(keywords);
        var page = 0;
        int? totalResults = null;

        while (products.Count < limit)
        {
            var remaining = limit - products.Count;
            var requestedPageSize = Math.Min(pageSize, remaining);
            var url = $"1.0/catalog/products?keywords={encodedKeywords}&num_results={requestedPageSize}&page={page}&response_groups={responseGroups}&image_sizes=1215,900,500,558,252";
            var response = await httpClient.GetFromJsonAsync<AudibleSearchResponse>(url, cancellationToken);
            var pageProducts = response?.Products ?? [];

            totalResults ??= response?.TotalResults;
            products.AddRange(pageProducts);

            if (pageProducts.Count == 0 ||
                pageProducts.Count < requestedPageSize ||
                (totalResults is not null && products.Count >= totalResults))
            {
                break;
            }

            page++;
        }

        return products;
    }

    private MetadataSearchResult ToResult(AudibleProduct product)
    {
        var authors = product.Authors?
            .Where(author => !string.IsNullOrWhiteSpace(author.Name))
            .Select(author => new Author(author.Asin ?? $"audible-author-{author.Name}", author.Name!))
            .ToList() ?? [];

        var narrators = product.Narrators?
            .Where(narrator => !string.IsNullOrWhiteSpace(narrator.Name))
            .Select(narrator => narrator.Name!)
            .ToList() ?? [];

        var edition = new BookEdition(
            Id: product.Asin ?? $"audible-{product.Title}",
            Asin: product.Asin,
            Publisher: product.PublisherName,
            PublishedDate: product.ReleaseDate,
            Language: product.Language,
            Narrators: narrators,
            DurationMinutes: product.RuntimeLengthMinutes);

        var series = product.Series?
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new SeriesInfo(item.Title!, item.Sequence))
            .ToList() ?? [];

        return new MetadataSearchResult(
            Name,
            product.Asin ?? product.Title!,
            product.Title!,
            product.Subtitle,
            authors,
            [edition],
            series,
            product.PublisherSummary,
            SelectCoverUrl(product.ProductImages),
            product.Categories?.Select(category => category.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Cast<string>().Take(5).ToList() ?? [],
            95,
            [
                new("title", Name),
                new("authors", Name),
                new("narrators", Name),
                new("duration", Name),
                new("asin", Name),
                new("coverUrl", Name)
            ]);
    }

    private static string? SelectCoverUrl(IReadOnlyDictionary<string, string>? images)
    {
        if (images is null || images.Count == 0)
        {
            return null;
        }

        foreach (var preferredSize in new[] { "500", "558", "900", "1215", "252" })
        {
            if (images.TryGetValue(preferredSize, out var url) && !string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return images.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private sealed record AudibleSearchResponse(
        [property: JsonPropertyName("products")] List<AudibleProduct>? Products,
        [property: JsonPropertyName("total_results")] int? TotalResults);

    private sealed record AudibleProduct(
        [property: JsonPropertyName("asin")] string? Asin,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("subtitle")] string? Subtitle,
        [property: JsonPropertyName("authors")] List<AudibleContributor>? Authors,
        [property: JsonPropertyName("narrators")] List<AudibleContributor>? Narrators,
        [property: JsonPropertyName("publisher_name")] string? PublisherName,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("runtime_length_min")] int? RuntimeLengthMinutes,
        [property: JsonPropertyName("publisher_summary")] string? PublisherSummary,
        [property: JsonPropertyName("product_images")] Dictionary<string, string>? ProductImages,
        [property: JsonPropertyName("series")] List<AudibleSeries>? Series,
        [property: JsonPropertyName("category_ladders")] List<AudibleCategory>? Categories);

    private sealed record AudibleContributor(
        [property: JsonPropertyName("asin")] string? Asin,
        [property: JsonPropertyName("name")] string? Name);

    private sealed record AudibleSeries(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("sequence")] string? Sequence);

    private sealed record AudibleCategory(
        [property: JsonPropertyName("name")] string? Name);
}
