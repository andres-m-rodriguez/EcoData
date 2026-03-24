namespace EcoData.Common.Messaging.Handlers;

/// <summary>
/// Context information for event handlers.
/// </summary>
public sealed record EventContext
{
    /// <summary>
    /// The topic the event was published to.
    /// </summary>
    public required string Topic { get; init; }

    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Correlation ID for tracing related messages.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Additional metadata as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
