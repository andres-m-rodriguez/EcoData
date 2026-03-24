using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.IntegrationTests;

public static class AspireHelpers
{
    public static async Task<string> GetConnectionStringAsync(
        this DistributedApplication app,
        string resourceName,
        CancellationToken ct = default
    )
    {
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource =
            model.Resources.SingleOrDefault(r => r.Name == resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");

        if (resource is not IResourceWithConnectionString connStrResource)
            throw new InvalidOperationException(
                $"Resource '{resourceName}' does not expose a connection string."
            );

        return await connStrResource.GetConnectionStringAsync(ct)
            ?? throw new InvalidOperationException(
                $"Connection string for '{resourceName}' is null."
            );
    }
}
