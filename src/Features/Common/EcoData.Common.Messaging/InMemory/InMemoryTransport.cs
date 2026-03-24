using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using EcoData.Common.Messaging.Abstractions;

namespace EcoData.Common.Messaging.InMemory;

/// <summary>
/// In-memory implementation of IMessageTransport using System.Threading.Channels.
/// Supports both topic-based pub/sub (events) and queue-based point-to-point (commands).
/// </summary>
public sealed class InMemoryTransport : IMessageTransport
{
    // Topic subscriptions: topic -> (subscriberId -> channel)
    private readonly ConcurrentDictionary<
        string,
        ConcurrentDictionary<Guid, object>
    > _topicSubscriptions = new();

    // Queue subscriptions: queue -> channel (single consumer per queue)
    private readonly ConcurrentDictionary<string, object> _queueChannels = new();

    // Lock objects for queue creation
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _queueLocks = new();

    public async Task PublishAsync<T>(
        string topic,
        MessageEnvelope<T> envelope,
        CancellationToken cancellationToken = default
    )
    {
        if (!_topicSubscriptions.TryGetValue(topic, out var subscribers))
        {
            return; // No subscribers for this topic
        }

        // Write to all subscriber channels for this topic
        var subscribersToRemove = new List<Guid>();

        foreach (var (subscriberId, channelObj) in subscribers)
        {
            if (channelObj is not Channel<MessageEnvelope<T>> channel)
            {
                continue;
            }

            try
            {
                if (!channel.Writer.TryWrite(envelope))
                {
                    await channel.Writer.WriteAsync(envelope, cancellationToken);
                }
            }
            catch (ChannelClosedException)
            {
                subscribersToRemove.Add(subscriberId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Clean up disconnected subscribers
        foreach (var subscriberId in subscribersToRemove)
        {
            subscribers.TryRemove(subscriberId, out _);
        }

        // Clean up empty topic subscriptions
        if (subscribers.IsEmpty)
        {
            _topicSubscriptions.TryRemove(topic, out _);
        }
    }

    public async IAsyncEnumerable<MessageEnvelope<T>> SubscribeAsync<T>(
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<MessageEnvelope<T>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

        var subscribers = _topicSubscriptions.GetOrAdd(
            topic,
            _ => new ConcurrentDictionary<Guid, object>()
        );
        subscribers.TryAdd(subscriberId, channel);

        try
        {
            await foreach (var envelope in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return envelope;
            }
        }
        finally
        {
            if (
                subscribers.TryRemove(subscriberId, out var removedChannel)
                && removedChannel is Channel<MessageEnvelope<T>> ch
            )
            {
                ch.Writer.TryComplete();
            }

            if (subscribers.IsEmpty)
            {
                _topicSubscriptions.TryRemove(topic, out _);
            }
        }
    }

    public async Task SendAsync<T>(
        string queue,
        MessageEnvelope<T> envelope,
        CancellationToken cancellationToken = default
    )
    {
        var channel = GetOrCreateQueueChannel<T>(queue);

        try
        {
            if (!channel.Writer.TryWrite(envelope))
            {
                await channel.Writer.WriteAsync(envelope, cancellationToken);
            }
        }
        catch (ChannelClosedException)
        {
            // Queue was closed, recreate and retry
            _queueChannels.TryRemove(queue, out _);
            var newChannel = GetOrCreateQueueChannel<T>(queue);
            await newChannel.Writer.WriteAsync(envelope, cancellationToken);
        }
    }

    public async IAsyncEnumerable<MessageEnvelope<T>> ReceiveAsync<T>(
        string queue,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var channel = GetOrCreateQueueChannel<T>(queue);

        await foreach (var envelope in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return envelope;
        }
    }

    private Channel<MessageEnvelope<T>> GetOrCreateQueueChannel<T>(string queue)
    {
        if (
            _queueChannels.TryGetValue(queue, out var existing)
            && existing is Channel<MessageEnvelope<T>> existingChannel
        )
        {
            return existingChannel;
        }

        var lockObj = _queueLocks.GetOrAdd(queue, _ => new SemaphoreSlim(1, 1));

        lockObj.Wait();
        try
        {
            if (
                _queueChannels.TryGetValue(queue, out existing)
                && existing is Channel<MessageEnvelope<T>> channel
            )
            {
                return channel;
            }

            var newChannel = Channel.CreateUnbounded<MessageEnvelope<T>>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
            );

            _queueChannels[queue] = newChannel;
            return newChannel;
        }
        finally
        {
            lockObj.Release();
        }
    }
}
