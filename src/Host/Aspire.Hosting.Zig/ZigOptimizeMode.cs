namespace Aspire.Hosting.ApplicationModel;

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
