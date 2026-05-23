using AudioBookarr.Core.Integrations;
using AudioBookarr.Core.Library;

namespace AudioBookarr.Infrastructure.Persistence;

public sealed class FileLibraryRepository(JsonFileStore store, string configPath) : ILibraryRepository
{
    private readonly string _path = Path.Combine(configPath, "library.json");

    public Task<LibraryState> GetStateAsync(CancellationToken cancellationToken) =>
        store.ReadAsync(_path, () => new LibraryState(), cancellationToken);

    public Task SaveStateAsync(LibraryState state, CancellationToken cancellationToken) =>
        store.WriteAsync(_path, state, cancellationToken);
}

public sealed class FileIntegrationRepository(JsonFileStore store, string configPath) : IIntegrationRepository
{
    private readonly string _path = Path.Combine(configPath, "integrations.json");

    public Task<IntegrationState> GetStateAsync(CancellationToken cancellationToken) =>
        store.ReadAsync(_path, () => new IntegrationState(), cancellationToken);

    public Task SaveStateAsync(IntegrationState state, CancellationToken cancellationToken) =>
        store.WriteAsync(_path, state, cancellationToken);
}
