using AudioBookarr.Core.Integrations;
using AudioBookarr.Core.Library;
using AudioBookarr.Core.Metadata;
using AudioBookarr.Core.System;
using AudioBookarr.Infrastructure.Integrations;
using AudioBookarr.Infrastructure.Metadata;
using AudioBookarr.Infrastructure.Persistence;
using AudioBookarr.Infrastructure.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AudioBookarr.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAudioBookarrInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var paths = new AppPaths(
            configuration["AUDIOBOOKARR_CONFIG"] ?? configuration["Paths:Config"] ?? "/config",
            configuration["AUDIOBOOKARR_AUDIOBOOKS"] ?? configuration["Paths:Audiobooks"] ?? "/audiobooks",
            configuration["AUDIOBOOKARR_DOWNLOADS"] ?? configuration["Paths:Downloads"] ?? "/downloads");

        services.AddSingleton(paths);
        services.AddSingleton<JsonFileStore>();
        services.AddSingleton<ILibraryRepository>(provider =>
            new FileLibraryRepository(provider.GetRequiredService<JsonFileStore>(), paths.ConfigPath));
        services.AddSingleton<IIntegrationRepository>(provider =>
            new FileIntegrationRepository(provider.GetRequiredService<JsonFileStore>(), paths.ConfigPath));

        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<IMetadataSearchService, MetadataSearchService>();
        services.AddSingleton<IIntegrationService, IntegrationService>();
        services.AddSingleton<IPathValidator, FileSystemPathValidator>();

        services.AddHttpClient<AudibleMetadataProvider>(client =>
        {
            client.BaseAddress = new Uri(configuration["Metadata:Audible:BaseUrl"] ?? "https://api.audible.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AudioBookarr/0.1");
        });

        services.AddHttpClient<OpenLibraryMetadataProvider>(client =>
        {
            client.BaseAddress = new Uri("https://openlibrary.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AudioBookarr/0.1");
        });

        services.AddSingleton<IMetadataProvider>(provider =>
            provider.GetRequiredService<AudibleMetadataProvider>());
        services.AddSingleton<IMetadataProvider>(provider =>
            provider.GetRequiredService<OpenLibraryMetadataProvider>());

        return services;
    }
}
