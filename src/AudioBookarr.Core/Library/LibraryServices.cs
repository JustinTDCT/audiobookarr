using AudioBookarr.Core.Metadata;

namespace AudioBookarr.Core.Library;

public sealed record AddBookRequest(
    MetadataSearchResult Metadata,
    bool Monitored,
    string? RootFolder,
    string QualityProfile = "standard");

public interface ILibraryRepository
{
    Task<LibraryState> GetStateAsync(CancellationToken cancellationToken);

    Task SaveStateAsync(LibraryState state, CancellationToken cancellationToken);
}

public interface ILibraryService
{
    Task<LibraryState> GetStateAsync(CancellationToken cancellationToken);

    Task<Book> AddBookAsync(AddBookRequest request, CancellationToken cancellationToken);

    Task<Book?> SetMonitoringAsync(string bookId, bool monitored, CancellationToken cancellationToken);
}

public sealed class LibraryService(ILibraryRepository repository) : ILibraryService
{
    public Task<LibraryState> GetStateAsync(CancellationToken cancellationToken) =>
        repository.GetStateAsync(cancellationToken);

    public async Task<Book> AddBookAsync(AddBookRequest request, CancellationToken cancellationToken)
    {
        var state = await repository.GetStateAsync(cancellationToken);
        var primaryEdition = request.Metadata.Editions.FirstOrDefault();
        var naturalKey = primaryEdition?.Asin ?? primaryEdition?.Isbn ?? request.Metadata.ProviderId;
        var existing = state.Books.FirstOrDefault(book =>
            book.Editions.Any(edition =>
                (!string.IsNullOrWhiteSpace(primaryEdition?.Asin) && edition.Asin == primaryEdition.Asin) ||
                (!string.IsNullOrWhiteSpace(primaryEdition?.Isbn) && edition.Isbn == primaryEdition.Isbn)) ||
            string.Equals(book.Title, request.Metadata.Title, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            var updated = existing with
            {
                Monitored = request.Monitored,
                RootFolder = request.RootFolder ?? existing.RootFolder,
                QualityProfile = request.QualityProfile
            };

            ReplaceBook(state, updated);
            await repository.SaveStateAsync(state, cancellationToken);
            return updated;
        }

        var book = new Book(
            $"book-{Slugify(request.Metadata.Title)}-{Slugify(naturalKey)}",
            request.Metadata.Title,
            request.Metadata.Subtitle,
            request.Metadata.Authors,
            request.Metadata.Editions,
            request.Metadata.Series,
            request.Metadata.Description,
            request.Metadata.CoverUrl,
            request.Metadata.Genres,
            request.Monitored,
            request.RootFolder ?? state.RootFolders.FirstOrDefault(folder => folder.IsDefault)?.Path,
            request.QualityProfile,
            request.Metadata.Provider,
            DateTimeOffset.UtcNow);

        state.Books.Add(book);
        await repository.SaveStateAsync(state, cancellationToken);
        return book;
    }

    public async Task<Book?> SetMonitoringAsync(string bookId, bool monitored, CancellationToken cancellationToken)
    {
        var state = await repository.GetStateAsync(cancellationToken);
        var existing = state.Books.FirstOrDefault(book => book.Id == bookId);

        if (existing is null)
        {
            return null;
        }

        var updated = existing with { Monitored = monitored };
        ReplaceBook(state, updated);
        await repository.SaveStateAsync(state, cancellationToken);
        return updated;
    }

    private static void ReplaceBook(LibraryState state, Book updated)
    {
        var index = state.Books.FindIndex(book => book.Id == updated.Id);
        if (index >= 0)
        {
            state.Books[index] = updated;
        }
    }

    private static string Slugify(string? value)
    {
        var chars = (value ?? "unknown")
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
