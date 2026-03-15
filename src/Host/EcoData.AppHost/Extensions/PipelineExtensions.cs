#pragma warning disable ASPIREPIPELINES001

using Microsoft.Extensions.Logging;
using static EcoData.AppHost.Extensions.AzureCliHelper;

namespace EcoData.AppHost.Extensions;

public static class PipelineExtensions
{
    public static IDistributedApplicationBuilder AddMigrationsStep(
        this IDistributedApplicationBuilder builder)
    {
        builder.Pipeline.AddStep(
            "run-migrations",
            async (context) =>
            {
                if (string.IsNullOrEmpty(ResourceGroup))
                {
                    context.Logger.LogWarning("AZURE_RESOURCE_GROUP not set, skipping migrations");
                    return;
                }

                var task = await context.ReportingStep.CreateTaskAsync(
                    "Running database migrations",
                    context.CancellationToken);

                await using (task.ConfigureAwait(false))
                {
                    context.Logger.LogInformation("Triggering seeder job to apply migrations...");

                    var (exitCode, _, error) = await RunCommandAsync(
                        $"containerapp job start --name seeder --resource-group {ResourceGroup}",
                        context.CancellationToken);

                    if (exitCode != 0)
                    {
                        context.Logger.LogError("Failed to start seeder job: {Error}", error);
                        throw new InvalidOperationException($"Migration job failed: {error}");
                    }

                    context.Logger.LogInformation("Seeder job triggered successfully");
                }
            },
            dependsOn: "deploy-seeder");

        return builder;
    }
}
