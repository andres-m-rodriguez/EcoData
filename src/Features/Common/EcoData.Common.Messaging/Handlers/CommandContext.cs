namespace EcoData.Common.Messaging.Handlers;

/// <summary>
/// Context information for command handlers.
/// </summary>
public sealed record CommandContext
{
    /// <summary>
    /// The queue the command was received from.
    /// </summary>
    public required string Queue { get; init; }

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
    /// Reply-to address for sending the response.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Additional metadata as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
