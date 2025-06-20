using System.Reflection;

namespace Fluxify.Playground;

public static class EmbeddedResourceLoader
{
    public static async Task<string> LoadAsync(string resourceName, Assembly? assembly = null, CancellationToken cancellationToken = default)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var resource = assembly
            .GetManifestResourceNames()
            .Where(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'");
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
