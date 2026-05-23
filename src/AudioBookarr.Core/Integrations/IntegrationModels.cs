namespace AudioBookarr.Core.Integrations;

public sealed record DownloadClientConfig(
    string Id,
    string Name,
    string Implementation,
    string Host,
    int Port,
    bool UseSsl,
    string? UrlBase,
    string Category,
    bool Enabled);

public sealed record IndexerConfig(
    string Id,
    string Name,
    string Implementation,
    string BaseUrl,
    string? ApiKey,
    bool EnableRss,
    bool EnableAutomaticSearch,
    bool EnableInteractiveSearch);

public sealed record IntegrationTestResult(
    bool Success,
    string Message);

public sealed record IntegrationState
{
    public List<DownloadClientConfig> DownloadClients { get; init; } = [];

    public List<IndexerConfig> Indexers { get; init; } = [];
}

public interface IIntegrationRepository
{
    Task<IntegrationState> GetStateAsync(CancellationToken cancellationToken);

    Task SaveStateAsync(IntegrationState state, CancellationToken cancellationToken);
}
