namespace EcoData.Common.Messaging.Abstractions;

/// <summary>
/// High-level message bus API for publishing events and sending commands.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes an event to all subscribers of the specified topic.
    /// </summary>
    Task PublishEventAsync<TEvent>(
        TEvent @event,
        string? topic = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Subscribes to events of a specific type from a topic.
    /// </summary>
    IAsyncEnumerable<TEvent> SubscribeToEventsAsync<TEvent>(
        string? topic = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends a command and waits for the result.
    /// </summary>
    Task<TResult> SendCommandAsync<TCommand, TResult>(
        TCommand command,
        string? queue = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends a command without waiting for a result (fire-and-forget).
    /// </summary>
    Task SendCommandAsync<TCommand>(
        TCommand command,
        string? queue = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );
}
