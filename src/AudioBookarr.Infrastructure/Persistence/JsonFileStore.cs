using System.Text.Json;

namespace AudioBookarr.Infrastructure.Persistence;

public sealed class JsonFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<T> ReadAsync<T>(string path, Func<T> createDefault, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            if (!File.Exists(path))
            {
                var state = createDefault();
                await WriteUnsafeAsync(path, state, cancellationToken);
                return state;
            }

            await using var stream = File.OpenRead(path);
            var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
            return value ?? createDefault();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await WriteUnsafeAsync(path, value, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task WriteUnsafeAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var temporaryPath = $"{path}.tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }
}
