using Fluxify.Playground.DelegatingHandlers;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpHandlers(this IServiceCollection services)
    {
        string[] redactedHeaders = [];

        var httpFilePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Debug.http"));

        if (File.Exists(httpFilePath))
        {
            File.Delete(httpFilePath);
        }

        services.AddTransient(_ => new HttpFileHandler(httpFilePath, redactedHeaders));

        services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(cfg => cfg
                .AddHttpMessageHandler<HttpFileHandler>()
            );

        services.AddHttpClient();

        return services;
    }
}
