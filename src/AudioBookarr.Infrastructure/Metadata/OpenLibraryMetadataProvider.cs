using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AudioBookarr.Core.Library;
using AudioBookarr.Core.Metadata;

namespace AudioBookarr.Infrastructure.Metadata;

public sealed class OpenLibraryMetadataProvider(HttpClient httpClient) : IMetadataProvider
{
    public string Name => "Open Library";

    public int Priority => 20;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        MetadataSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(request);

        var url = $"search.json?q={Uri.EscapeDataString(query)}&limit={request.Limit}&fields=key,title,subtitle,author_name,author_key,first_publish_year,cover_i,isbn,language,publisher,subject";
        var response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url, cancellationToken);

        return response?.Docs?
            .Where(doc => !string.IsNullOrWhiteSpace(doc.Title))
            .Select(doc => ToResult(doc))
            .ToList() ?? [];
    }

    private static string BuildQuery(MetadataSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            return $"isbn:{request.Isbn}";
        }

        return request.Field switch
        {
            MetadataSearchField.Author => $"author:{request.Query}",
            MetadataSearchField.Title => $"title:{request.Query}",
            MetadataSearchField.Series => request.Query,
            _ => request.Query
        };
    }

    private MetadataSearchResult ToResult(OpenLibraryDoc doc)
    {
        var authors = doc.AuthorName?
            .Select((name, index) => new Author(
                doc.AuthorKey?.ElementAtOrDefault(index) ?? $"openlibrary-author-{index}",
                name))
            .ToList() ?? [];

        var edition = new BookEdition(
            Id: doc.Key ?? $"openlibrary-{doc.Title}",
            Isbn: doc.Isbn?.FirstOrDefault(),
            Publisher: doc.Publisher?.FirstOrDefault(),
            PublishedDate: doc.FirstPublishYear?.ToString(),
            Language: doc.Language?.FirstOrDefault());

        var coverUrl = doc.CoverId is null
            ? null
            : $"https://covers.openlibrary.org/b/id/{doc.CoverId}-L.jpg";

        return new MetadataSearchResult(
            Name,
            doc.Key ?? doc.Title!,
            doc.Title!,
            doc.Subtitle,
            authors,
            [edition],
            [],
            null,
            coverUrl,
            doc.Subject?.Take(5).ToList() ?? [],
            70,
            [
                new("title", Name),
                new("authors", Name),
                new("isbn", Name),
                new("coverUrl", Name)
            ]);
    }

    private sealed record OpenLibrarySearchResponse(
        [property: JsonPropertyName("docs")] List<OpenLibraryDoc>? Docs);

    private sealed record OpenLibraryDoc(
        [property: JsonPropertyName("key")] string? Key,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("subtitle")] string? Subtitle,
        [property: JsonPropertyName("author_name")] List<string>? AuthorName,
        [property: JsonPropertyName("author_key")] List<string>? AuthorKey,
        [property: JsonPropertyName("first_publish_year")] int? FirstPublishYear,
        [property: JsonPropertyName("cover_i")] int? CoverId,
        [property: JsonPropertyName("isbn")] List<string>? Isbn,
        [property: JsonPropertyName("language")] List<string>? Language,
        [property: JsonPropertyName("publisher")] List<string>? Publisher,
        [property: JsonPropertyName("subject")] List<string>? Subject);
}
