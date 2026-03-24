namespace EcoData.Common.Messaging.Configuration;

/// <summary>
/// Options for configuring the messaging system.
/// </summary>
public sealed class MessagingOptions
{
    /// <summary>
    /// Default timeout for command responses in milliseconds.
    /// </summary>
    public int DefaultCommandTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Whether to enable message tracing.
    /// </summary>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Maximum retry attempts for failed message deliveries.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retry attempts in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}
