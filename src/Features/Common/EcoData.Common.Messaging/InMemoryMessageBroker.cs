using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EcoData.Common.Messaging;

/// <summary>
/// In-memory implementation of IMessageBroker using Channel&lt;T&gt;.
/// Supports multiple subscribers per topic and handles cleanup automatically.
/// </summary>
/// <typeparam name="T">The type of message being published/subscribed.</typeparam>
public sealed class InMemoryMessageBroker<T> : IMessageBroker<T>
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<T>>> _subscriptions = new();

    public async Task PublishAsync(string topic, T message, CancellationToken cancellationToken = default)
    {
        if (!_subscriptions.TryGetValue(topic, out var subscribers))
        {
            return; // No subscribers for this topic
        }

        // Write to all subscriber channels for this topic
        foreach (var (subscriberId, channel) in subscribers)
        {
            try
            {
                // TryWrite is non-blocking; if the channel is full, the message is dropped
                // For bounded channels, consider using WriteAsync with timeout
                if (!channel.Writer.TryWrite(message))
                {
                    // Channel is full or completed, try async write with cancellation
                    await channel.Writer.WriteAsync(message, cancellationToken);
                }
            }
            catch (ChannelClosedException)
            {
                // Subscriber disconnected, remove them
                subscribers.TryRemove(subscriberId, out _);
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, stop publishing
                break;
            }
        }

        // Clean up empty topic subscriptions
        if (subscribers.IsEmpty)
        {
            _subscriptions.TryRemove(topic, out _);
        }
    }

    public async IAsyncEnumerable<T> SubscribeAsync(
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Register the subscriber
        var subscribers = _subscriptions.GetOrAdd(topic, _ => new ConcurrentDictionary<Guid, Channel<T>>());
        subscribers.TryAdd(subscriberId, channel);

        try
        {
            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return message;
            }
        }
        finally
        {
            // Clean up: remove subscriber and close channel
            if (subscribers.TryRemove(subscriberId, out var removedChannel))
            {
                removedChannel.Writer.TryComplete();
            }

            // Clean up empty topic
            if (subscribers.IsEmpty)
            {
                _subscriptions.TryRemove(topic, out _);
            }
        }
    }
}
