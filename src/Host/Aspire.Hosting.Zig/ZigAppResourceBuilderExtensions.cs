using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Zig application resources to the distributed application builder.
/// </summary>
public static class ZigAppResourceBuilderExtensions
{
    private static bool _buildEventRegistered;

    /// <summary>
    /// Adds a Zig application resource to the application model.
    /// The Zig project will be built using `zig build` and optionally run.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="projectDirectory">The path to the directory containing build.zig.</param>
    /// <param name="zigPath">Optional path to the zig executable. If null, uses "zig" from PATH.</param>
    /// <returns>A resource builder for further configuration.</returns>
    public static IResourceBuilder<ZigAppResource> AddZigApp(
        this IDistributedApplicationBuilder builder,
        string name,
        string projectDirectory,
        string? zigPath = null
    )
    {
        // Register build event handler once
        if (!_buildEventRegistered)
        {
            RegisterZigBuildEvent(builder);
            _buildEventRegistered = true;
        }

        var workingDirectory = Path.GetFullPath(projectDirectory);
        var zigExecutable = zigPath ?? "zig";

        var resource = new ZigAppResource(name, workingDirectory) { ZigPath = zigPath };

        return builder
            .AddResource(resource)
            .WithArgs(context =>
            {
                context.Args.Add("build");
                context.Args.Add(resource.RunStep);

                if (resource.OptimizeMode.HasValue)
                {
                    context.Args.Add($"-Doptimize={resource.OptimizeMode.Value}");
                }
            })
            .WithOtlpExporter();
    }

    private static void RegisterZigBuildEvent(IDistributedApplicationBuilder builder)
    {
        builder.Eventing.Subscribe<BeforeStartEvent>(
            async (@event, ct) =>
            {
                var zigResources = @event.Model.Resources.OfType<ZigAppResource>().ToList();

                if (zigResources.Count == 0)
                {
                    return;
                }

                var logger = @event
                    .Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Aspire.Hosting.Zig");

                logger.LogInformation("Building {Count} Zig application(s)...", zigResources.Count);

                var buildTasks = zigResources.Select(resource =>
                    BuildZigAppAsync(resource, logger, ct)
                );
                await Task.WhenAll(buildTasks);
            }
        );
    }

    private static async Task BuildZigAppAsync(
        ZigAppResource resource,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var zigPath = resource.ZigPath ?? "zig";

        var arguments = new List<string> { "build", resource.BuildStep };

        if (resource.OptimizeMode.HasValue)
        {
            arguments.Add($"-Doptimize={resource.OptimizeMode.Value}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = zigPath,
            Arguments = string.Join(" ", arguments),
            WorkingDirectory = resource.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        logger.LogInformation(
            "Building Zig app '{Name}': {Command} {Args}",
            resource.Name,
            zigPath,
            string.Join(" ", arguments)
        );

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                logger.LogError(
                    "Failed to build Zig app '{Name}'. Exit code: {ExitCode}\nError: {Error}",
                    resource.Name,
                    process.ExitCode,
                    error
                );
                throw new InvalidOperationException(
                    $"Zig build failed for '{resource.Name}': {error}"
                );
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                logger.LogDebug("Zig build output for '{Name}':\n{Output}", resource.Name, output);
            }

            logger.LogInformation("Successfully built Zig app '{Name}'", resource.Name);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Build of Zig app '{Name}' was cancelled", resource.Name);
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "Error building Zig app '{Name}'", resource.Name);
            throw;
        }
    }

    /// <summary>
    /// Configures the Zig application to use a specific optimization mode.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="mode">The optimization mode to use.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithOptimization(
        this IResourceBuilder<ZigAppResource> builder,
        ZigOptimizeMode mode
    )
    {
        builder.Resource.OptimizeMode = mode;
        return builder;
    }

    /// <summary>
    /// Configures the Zig application to use a custom build step.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="step">The build step name (e.g., "install", "run", "test").</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithBuildStep(
        this IResourceBuilder<ZigAppResource> builder,
        string step
    )
    {
        builder.Resource.BuildStep = step;
        return builder;
    }

    /// <summary>
    /// Configures the Zig application to use a custom run step.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="step">The run step name.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithRunStep(
        this IResourceBuilder<ZigAppResource> builder,
        string step
    )
    {
        builder.Resource.RunStep = step;
        return builder;
    }

    /// <summary>
    /// Configures an HTTP endpoint for the Zig application.
    /// Use this when your Zig app exposes an HTTP server.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="targetPort">The port the Zig app listens on.</param>
    /// <param name="endpointName">The endpoint name (defaults to "http").</param>
    /// <param name="isProxied">Whether the endpoint should be proxied through Aspire (defaults to true).</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithZigHttpEndpoint(
        this IResourceBuilder<ZigAppResource> builder,
        int targetPort,
        string endpointName = "http",
        bool isProxied = true
    )
    {
        builder.Resource.HttpPort = targetPort;
        builder.Resource.HttpEndpointName = endpointName;

        // For non-container resources, only specify targetPort and let Aspire assign the external port
        return builder.WithHttpEndpoint(
            targetPort: targetPort,
            name: endpointName,
            isProxied: isProxied
        );
    }

    /// <summary>
    /// Configures the Zig application with additional command line arguments.
    /// These are passed after the build step (e.g., `zig build run -- [args]`).
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="args">The additional arguments to pass.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithZigArgs(
        this IResourceBuilder<ZigAppResource> builder,
        params string[] args
    )
    {
        return builder.WithArgs(context =>
        {
            // Add separator for app arguments
            if (args.Length > 0)
            {
                context.Args.Add("--");
                foreach (var arg in args)
                {
                    context.Args.Add(arg);
                }
            }
        });
    }

    /// <summary>
    /// Configures the Zig application to only build without running.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> BuildOnly(
        this IResourceBuilder<ZigAppResource> builder
    )
    {
        builder.Resource.RunAfterBuild = false;
        builder.Resource.RunStep = "install";
        return builder;
    }

    /// <summary>
    /// Configures the Zig application to run tests.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithTests(
        this IResourceBuilder<ZigAppResource> builder
    )
    {
        builder.Resource.RunStep = "test";
        return builder;
    }

    /// <summary>
    /// Adds a watch mode command that rebuilds the Zig app when source files change.
    /// Uses `zig build --watch` which is available in Zig 0.16.0+.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithWatchCommand(
        this IResourceBuilder<ZigAppResource> builder
    )
    {
        return builder.WithCommand(
            name: "watch",
            displayName: "Watch Mode",
            executeCommand: context => Task.FromResult(CommandResults.Success()),
            commandOptions: new CommandOptions
            {
                IconName = "Eye",
                IconVariant = IconVariant.Regular,
            }
        );
    }

    /// <summary>
    /// Adds a rebuild command to the Aspire dashboard.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithRebuildCommand(
        this IResourceBuilder<ZigAppResource> builder
    )
    {
        return builder.WithCommand(
            name: "rebuild",
            displayName: "Rebuild",
            executeCommand: context => Task.FromResult(CommandResults.Success()),
            commandOptions: new CommandOptions
            {
                IconName = "ArrowSync",
                IconVariant = IconVariant.Regular,
            }
        );
    }

    /// <summary>
    /// Sets a custom path to the Zig executable.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="zigPath">The full path to the zig executable.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithZigPath(
        this IResourceBuilder<ZigAppResource> builder,
        string zigPath
    )
    {
        builder.Resource.ZigPath = zigPath;
        return builder;
    }

    /// <summary>
    /// Configures environment variables for the Zig application.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The environment variable name.</param>
    /// <param name="value">The environment variable value.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ZigAppResource> WithZigEnvironment(
        this IResourceBuilder<ZigAppResource> builder,
        string name,
        string value
    )
    {
        return builder.WithEnvironment(name, value);
    }
}
