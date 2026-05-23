using AudioBookarr.Core.Integrations;

namespace AudioBookarr.Infrastructure.Integrations;

public interface IIntegrationService
{
    Task<IntegrationState> GetStateAsync(CancellationToken cancellationToken);

    Task<DownloadClientConfig> UpsertDownloadClientAsync(DownloadClientConfig client, CancellationToken cancellationToken);

    Task<IndexerConfig> UpsertIndexerAsync(IndexerConfig indexer, CancellationToken cancellationToken);

    Task<IntegrationTestResult> TestDownloadClientAsync(DownloadClientConfig client, CancellationToken cancellationToken);

    Task<IntegrationTestResult> TestIndexerAsync(IndexerConfig indexer, CancellationToken cancellationToken);
}

public sealed class IntegrationService(IIntegrationRepository repository, IHttpClientFactory httpClientFactory) : IIntegrationService
{
    public Task<IntegrationState> GetStateAsync(CancellationToken cancellationToken) =>
        repository.GetStateAsync(cancellationToken);

    public async Task<DownloadClientConfig> UpsertDownloadClientAsync(DownloadClientConfig client, CancellationToken cancellationToken)
    {
        var state = await repository.GetStateAsync(cancellationToken);
        var normalized = string.IsNullOrWhiteSpace(client.Id)
            ? client with { Id = $"download-{Guid.NewGuid():N}" }
            : client;

        state.DownloadClients.RemoveAll(existing => existing.Id == normalized.Id);
        state.DownloadClients.Add(normalized);
        await repository.SaveStateAsync(state, cancellationToken);
        return normalized;
    }

    public async Task<IndexerConfig> UpsertIndexerAsync(IndexerConfig indexer, CancellationToken cancellationToken)
    {
        var state = await repository.GetStateAsync(cancellationToken);
        var normalized = string.IsNullOrWhiteSpace(indexer.Id)
            ? indexer with { Id = $"indexer-{Guid.NewGuid():N}" }
            : indexer;

        state.Indexers.RemoveAll(existing => existing.Id == normalized.Id);
        state.Indexers.Add(normalized);
        await repository.SaveStateAsync(state, cancellationToken);
        return normalized;
    }

    public async Task<IntegrationTestResult> TestDownloadClientAsync(
        DownloadClientConfig client,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        var baseUri = BuildBaseUri(client.UseSsl, client.Host, client.Port, client.UrlBase);
        var testPath = client.Implementation.Equals("sabnzbd", StringComparison.OrdinalIgnoreCase)
            ? "api?mode=version&output=json"
            : "api/v2/app/version";

        try
        {
            using var response = await httpClient.GetAsync(new Uri(baseUri, testPath), cancellationToken);
            return response.IsSuccessStatusCode
                ? new IntegrationTestResult(true, $"{client.Name} responded successfully.")
                : new IntegrationTestResult(false, $"{client.Name} returned {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return new IntegrationTestResult(false, exception.Message);
        }
    }

    public async Task<IntegrationTestResult> TestIndexerAsync(
        IndexerConfig indexer,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        var separator = indexer.BaseUrl.Contains('?') ? '&' : '?';
        var url = $"{indexer.BaseUrl.TrimEnd('/')}/api{separator}t=caps";

        if (!string.IsNullOrWhiteSpace(indexer.ApiKey))
        {
            url += $"&apikey={Uri.EscapeDataString(indexer.ApiKey)}";
        }

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode
                ? new IntegrationTestResult(true, $"{indexer.Name} capabilities loaded.")
                : new IntegrationTestResult(false, $"{indexer.Name} returned {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return new IntegrationTestResult(false, exception.Message);
        }
    }

    private static Uri BuildBaseUri(bool useSsl, string host, int port, string? urlBase)
    {
        var scheme = useSsl ? "https" : "http";
        var normalizedBase = string.IsNullOrWhiteSpace(urlBase)
            ? string.Empty
            : $"/{urlBase.Trim('/')}";

        return new Uri($"{scheme}://{host}:{port}{normalizedBase}/");
    }
}
