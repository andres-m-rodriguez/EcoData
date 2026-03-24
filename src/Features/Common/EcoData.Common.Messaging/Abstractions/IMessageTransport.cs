namespace EcoData.Common.Messaging.Abstractions;

/// <summary>
/// Low-level transport abstraction for message delivery.
/// Implementations handle the actual sending/receiving of messages.
/// </summary>
public interface IMessageTransport
{
    /// <summary>
    /// Publishes a message to a topic (fan-out to all subscribers).
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="envelope">The message envelope containing the payload and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(string topic, MessageEnvelope<T> envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages from a topic.
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    /// <returns>An async enumerable of message envelopes.</returns>
    IAsyncEnumerable<MessageEnvelope<T>> SubscribeAsync<T>(string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to a queue (point-to-point, single consumer).
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="queue">The queue to send to.</param>
    /// <param name="envelope">The message envelope containing the payload and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync<T>(string queue, MessageEnvelope<T> envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives messages from a queue (competing consumer).
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="queue">The queue to receive from.</param>
    /// <param name="cancellationToken">Cancellation token to stop receiving.</param>
    /// <returns>An async enumerable of message envelopes.</returns>
    IAsyncEnumerable<MessageEnvelope<T>> ReceiveAsync<T>(string queue, CancellationToken cancellationToken = default);
}
