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
            dependsOn: "provision-seeder-containerapp"
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
                    // Get the container app environment name
                    var (envExit, envName, _) = await RunCommandAsync(
                        $"containerapp env list --resource-group {ResourceGroup} --query \"[0].name\" -o tsv",
                        context.CancellationToken
                    );

                    if (envExit != 0 || string.IsNullOrWhiteSpace(envName))
                    {
                        context.Logger.LogError("Failed to get container app environment name");
                        throw new InvalidOperationException(
                            "Could not determine container app environment"
                        );
                    }

                    envName = envName.Trim();

                    // Check if hostname exists AND has SSL binding enabled
                    var (checkExit, checkOutput, _) = await RunCommandAsync(
                        $"containerapp hostname list --name {ContainerAppName} --resource-group {ResourceGroup} --query \"[?name=='{CustomDomain}'].bindingType\" -o tsv",
                        context.CancellationToken
                    );

                    var bindingType = checkOutput?.Trim();
                    var hostnameExists = checkExit == 0 && !string.IsNullOrWhiteSpace(bindingType);
                    var sslEnabled = string.Equals(
                        bindingType,
                        "SniEnabled",
                        StringComparison.OrdinalIgnoreCase
                    );

                    if (hostnameExists && sslEnabled)
                    {
                        context.Logger.LogInformation(
                            "Custom domain {Domain} already configured with SSL",
                            CustomDomain
                        );
                        return;
                    }

                    // Add hostname if it doesn't exist
                    if (!hostnameExists)
                    {
                        context.Logger.LogInformation(
                            "Adding custom domain {Domain}...",
                            CustomDomain
                        );

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
                    }
                    else
                    {
                        // Hostname exists but SSL is disabled - this is what caused prod to go down
                        context.Logger.LogWarning(
                            "Custom domain {Domain} exists but SSL binding is '{BindingType}', re-binding certificate...",
                            CustomDomain,
                            bindingType
                        );
                    }

                    // Ensure SSL certificate is bound
                    context.Logger.LogInformation(
                        "Binding managed certificate for {Domain}...",
                        CustomDomain
                    );

                    var (certExit, _, certError) = await RunCommandAsync(
                        $"containerapp hostname bind --name {ContainerAppName} --resource-group {ResourceGroup} --hostname {CustomDomain} --environment {envName} --validation-method CNAME",
                        context.CancellationToken
                    );

                    if (certExit != 0)
                    {
                        context.Logger.LogError(
                            "Failed to bind SSL certificate: {Error}",
                            certError
                        );
                        throw new InvalidOperationException(
                            $"SSL certificate binding failed: {certError}"
                        );
                    }

                    context.Logger.LogInformation(
                        "Custom domain {Domain} configured with SSL",
                        CustomDomain
                    );
                }
            },
            dependsOn: $"provision-{ContainerAppName}-containerapp"
        );

        return builder;
    }
}
