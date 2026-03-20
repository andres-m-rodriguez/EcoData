namespace EcoData.Common.Messaging;

/// <summary>
/// A simple pub/sub message broker interface.
/// Supports multiple subscribers per topic and multiple topics simultaneously.
/// </summary>
/// <typeparam name="T">The type of message being published/subscribed.</typeparam>
public interface IMessageBroker<T>
{
    /// <summary>
    /// Publishes a message to all subscribers of the specified topic.
    /// </summary>
    /// <param name="topic">The topic to publish to (e.g., sensor ID).</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(string topic, T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages for a specific topic.
    /// Returns an async enumerable that yields messages as they are published.
    /// The subscription is automatically cleaned up when the cancellation token is triggered
    /// or when the caller stops enumerating.
    /// </summary>
    /// <param name="topic">The topic to subscribe to (e.g., sensor ID).</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription.</param>
    /// <returns>An async enumerable of messages.</returns>
    IAsyncEnumerable<T> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
}
