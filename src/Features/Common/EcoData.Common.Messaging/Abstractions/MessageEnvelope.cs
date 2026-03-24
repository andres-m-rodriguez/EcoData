namespace EcoData.Common.Messaging.Abstractions;

/// <summary>
/// Wrapper for messages with metadata for routing and tracing.
/// </summary>
/// <typeparam name="T">The type of the message payload.</typeparam>
public sealed record MessageEnvelope<T>
{
    /// <summary>
    /// The message payload.
    /// </summary>
    public required T Payload { get; init; }

    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public Guid MessageId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Correlation ID for tracing related messages across services.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// The topic or queue this message is targeted to.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The CLR type name of the payload for deserialization.
    /// </summary>
    public string MessageType { get; init; } = typeof(T).AssemblyQualifiedName!;

    /// <summary>
    /// Optional reply-to address for request/response patterns.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Additional metadata as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
