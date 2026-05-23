namespace AudioBookarr.Core.System;

public sealed record AppPaths(
    string ConfigPath,
    string AudiobooksPath,
    string DownloadsPath);

public sealed record PathHealth(
    string Path,
    bool Exists,
    bool Writable,
    string? Message);

public sealed record SystemStatus(
    string AppName,
    string Version,
    AppPaths Paths,
    IReadOnlyList<PathHealth> PathHealth,
    IReadOnlyList<string> Warnings);

public interface IPathValidator
{
    Task<IReadOnlyList<PathHealth>> ValidateAsync(AppPaths paths, CancellationToken cancellationToken);
}
