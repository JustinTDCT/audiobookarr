using AudioBookarr.Core.System;

namespace AudioBookarr.Infrastructure.System;

public sealed class FileSystemPathValidator : IPathValidator
{
    public Task<IReadOnlyList<PathHealth>> ValidateAsync(AppPaths paths, CancellationToken cancellationToken)
    {
        IReadOnlyList<PathHealth> results =
        [
            ValidateWritable(paths.ConfigPath),
            ValidateReadable(paths.AudiobooksPath),
            ValidateReadable(paths.DownloadsPath)
        ];

        return Task.FromResult(results);
    }

    private static PathHealth ValidateWritable(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probePath = Path.Combine(path, ".audiobookarr-write-test");
            File.WriteAllText(probePath, DateTimeOffset.UtcNow.ToString("O"));
            File.Delete(probePath);
            return new PathHealth(path, true, true, null);
        }
        catch (Exception exception)
        {
            return new PathHealth(path, Directory.Exists(path), false, exception.Message);
        }
    }

    private static PathHealth ValidateReadable(string path)
    {
        try
        {
            var exists = Directory.Exists(path);

            if (exists)
            {
                _ = Directory.EnumerateFileSystemEntries(path).Take(1).ToList();
            }

            return new PathHealth(path, exists, exists, exists ? null : "Path is not mounted or does not exist.");
        }
        catch (Exception exception)
        {
            return new PathHealth(path, true, false, exception.Message);
        }
    }
}
