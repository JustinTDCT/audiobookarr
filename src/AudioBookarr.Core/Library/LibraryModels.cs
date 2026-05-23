namespace AudioBookarr.Core.Library;

public sealed record Author(
    string Id,
    string Name,
    string? SortName = null);

public sealed record SeriesInfo(
    string Name,
    string? Sequence = null);

public sealed record BookEdition(
    string Id,
    string? Isbn = null,
    string? Asin = null,
    string? Publisher = null,
    string? PublishedDate = null,
    string? Language = null,
    IReadOnlyList<string>? Narrators = null,
    int? DurationMinutes = null);

public sealed record Book(
    string Id,
    string Title,
    string? Subtitle,
    IReadOnlyList<Author> Authors,
    IReadOnlyList<BookEdition> Editions,
    IReadOnlyList<SeriesInfo> Series,
    string? Description,
    string? CoverUrl,
    IReadOnlyList<string> Genres,
    bool Monitored,
    string? RootFolder,
    string QualityProfile,
    string MetadataSource,
    DateTimeOffset AddedAt);

public sealed record RootFolder(
    string Path,
    bool IsDefault = false);

public sealed record QualityProfile(
    string Id,
    string Name,
    bool PreferUnabridged,
    int? MinimumBitrate,
    IReadOnlyList<string> PreferredFormats);

public sealed class LibraryState
{
    public List<Book> Books { get; init; } = [];

    public List<RootFolder> RootFolders { get; init; } =
    [
        new("/audiobooks", true)
    ];

    public List<QualityProfile> QualityProfiles { get; init; } =
    [
        new("standard", "Standard", true, null, ["m4b", "mp3"])
    ];
}
