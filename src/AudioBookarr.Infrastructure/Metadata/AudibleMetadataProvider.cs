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

        var responseGroups = "contributors,product_attrs,product_desc,product_details,product_extended_attrs,rating,series";
        var url = $"1.0/catalog/products?keywords={Uri.EscapeDataString(keywords)}&num_results={request.Limit}&response_groups={responseGroups}&image_sizes=500";
        var response = await httpClient.GetFromJsonAsync<AudibleSearchResponse>(url, cancellationToken);

        return response?.Products?
            .Where(product => !string.IsNullOrWhiteSpace(product.Title))
            .Select(ToResult)
            .ToList() ?? [];
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
            product.ImageUrl,
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

    private sealed record AudibleSearchResponse(
        [property: JsonPropertyName("products")] List<AudibleProduct>? Products);

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
        [property: JsonPropertyName("image_url")] string? ImageUrl,
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
