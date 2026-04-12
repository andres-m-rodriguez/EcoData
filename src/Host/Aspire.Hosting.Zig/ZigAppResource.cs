namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a Zig application resource that can be built and run by Aspire.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="workingDirectory">The working directory containing the Zig project (build.zig).</param>
public class ZigAppResource(string name, string workingDirectory)
    : ExecutableResource(name, "zig", workingDirectory), IResourceWithServiceDiscovery
{
    /// <summary>
    /// Gets or sets the build step to execute (defaults to "install" which builds the project).
    /// </summary>
    public string BuildStep { get; set; } = "install";

    /// <summary>
    /// Gets or sets the run step to execute (defaults to "run" for running the application).
    /// </summary>
    public string RunStep { get; set; } = "run";

    /// <summary>
    /// Gets or sets the optimization mode for the build.
    /// </summary>
    public ZigOptimizeMode? OptimizeMode { get; set; }

    /// <summary>
    /// Gets or sets whether to run the app after building (default: true).
    /// </summary>
    public bool RunAfterBuild { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the zig executable. If null, uses "zig" from PATH.
    /// </summary>
    public string? ZigPath { get; set; }

    /// <summary>
    /// Gets or sets the HTTP endpoint name for health checks.
    /// </summary>
    public string? HttpEndpointName { get; set; }

    /// <summary>
    /// Gets or sets the HTTP port the Zig app listens on (if applicable).
    /// </summary>
    public int? HttpPort { get; set; }
}

/// <summary>
/// Zig optimization modes as defined in Zig 0.16.0.
/// </summary>
public enum ZigOptimizeMode
{
    /// <summary>
    /// Debug mode - fastest compilation, includes safety checks and debug info.
    /// </summary>
    Debug,

    /// <summary>
    /// Release-safe mode - optimized with safety checks enabled.
    /// </summary>
    ReleaseSafe,

    /// <summary>
    /// Release-fast mode - maximum runtime performance, safety checks disabled.
    /// </summary>
    ReleaseFast,

    /// <summary>
    /// Release-small mode - optimized for smallest binary size.
    /// </summary>
    ReleaseSmall
}
