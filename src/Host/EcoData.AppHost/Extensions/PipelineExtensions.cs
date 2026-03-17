#pragma warning disable ASPIREPIPELINES001

using Microsoft.Extensions.Logging;
using static EcoData.AppHost.Extensions.AzureCliHelper;

namespace EcoData.AppHost.Extensions;

public static class PipelineExtensions
{
    private const string CustomDomain = "portal.ecodatapr.com";
    private const string ContainerAppName = "ecoportal";

    public static IDistributedApplicationBuilder AddMigrationsStep(
        this IDistributedApplicationBuilder builder
    )
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
                    context.CancellationToken
                );

                await using (task.ConfigureAwait(false))
                {
                    context.Logger.LogInformation("Triggering seeder job to apply migrations...");

                    var (exitCode, _, error) = await RunCommandAsync(
                        $"containerapp job start --name seeder --resource-group {ResourceGroup}",
                        context.CancellationToken
                    );

                    if (exitCode != 0)
                    {
                        context.Logger.LogError("Failed to start seeder job: {Error}", error);
                        throw new InvalidOperationException($"Migration job failed: {error}");
                    }

                    context.Logger.LogInformation("Seeder job triggered successfully");
                }
            },
            dependsOn: "deploy-seeder"
        );

        return builder;
    }

    public static IDistributedApplicationBuilder AddCustomDomainStep(
        this IDistributedApplicationBuilder builder
    )
    {
        builder.Pipeline.AddStep(
            "configure-custom-domain",
            async (context) =>
            {
                if (string.IsNullOrEmpty(ResourceGroup))
                {
                    context.Logger.LogWarning(
                        "AZURE_RESOURCE_GROUP not set, skipping custom domain"
                    );
                    return;
                }

                var task = await context.ReportingStep.CreateTaskAsync(
                    "Configuring custom domain",
                    context.CancellationToken
                );

                await using (task.ConfigureAwait(false))
                {
                    // Check if hostname already exists
                    var (checkExit, checkOutput, _) = await RunCommandAsync(
                        $"containerapp hostname list --name {ContainerAppName} --resource-group {ResourceGroup} --query \"[?name=='{CustomDomain}']\" -o tsv",
                        context.CancellationToken
                    );

                    if (checkExit == 0 && !string.IsNullOrWhiteSpace(checkOutput))
                    {
                        context.Logger.LogInformation(
                            "Custom domain {Domain} already configured",
                            CustomDomain
                        );
                        return;
                    }

                    context.Logger.LogInformation("Adding custom domain {Domain}...", CustomDomain);

                    var (exitCode, _, error) = await RunCommandAsync(
                        $"containerapp hostname add --name {ContainerAppName} --resource-group {ResourceGroup} --hostname {CustomDomain}",
                        context.CancellationToken
                    );

                    if (exitCode != 0)
                    {
                        context.Logger.LogError("Failed to add custom domain: {Error}", error);
                        throw new InvalidOperationException(
                            $"Custom domain configuration failed: {error}"
                        );
                    }

                    // Bind managed certificate
                    context.Logger.LogInformation("Binding managed certificate...");

                    var (certExit, _, certError) = await RunCommandAsync(
                        $"containerapp hostname bind --name {ContainerAppName} --resource-group {ResourceGroup} --hostname {CustomDomain} --environment {ResourceGroup} --validation-method CNAME",
                        context.CancellationToken
                    );

                    if (certExit != 0)
                    {
                        context.Logger.LogWarning(
                            "Certificate binding may require manual setup: {Error}",
                            certError
                        );
                    }
                    else
                    {
                        context.Logger.LogInformation("Custom domain configured successfully");
                    }
                }
            },
            dependsOn: $"deploy-{ContainerAppName}"
        );

        return builder;
    }
}
