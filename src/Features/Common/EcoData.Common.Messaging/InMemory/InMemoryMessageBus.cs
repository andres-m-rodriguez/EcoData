using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using EcoData.Common.Messaging.Abstractions;

namespace EcoData.Common.Messaging.InMemory;

/// <summary>
/// In-memory implementation of IMessageBus using InMemoryTransport.
/// </summary>
public sealed class InMemoryMessageBus : IMessageBus
{
    private readonly IMessageTransport _transport;
    private readonly ConcurrentDictionary<string, object> _responseChannels = new();

    public InMemoryMessageBus(IMessageTransport transport)
    {
        _transport = transport;
    }

    public async Task PublishEventAsync<TEvent>(
        TEvent @event,
        string? topic = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        var destinationTopic = topic ?? typeof(TEvent).Name;
        var envelope = new MessageEnvelope<TEvent>
        {
            Payload = @event,
            Destination = destinationTopic,
            CorrelationId = correlationId,
        };

        await _transport.PublishAsync(destinationTopic, envelope, cancellationToken);
    }

    public async IAsyncEnumerable<TEvent> SubscribeToEventsAsync<TEvent>(
        string? topic = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var destinationTopic = topic ?? typeof(TEvent).Name;

        await foreach (
            var envelope in _transport.SubscribeAsync<TEvent>(destinationTopic, cancellationToken)
        )
        {
            yield return envelope.Payload;
        }
    }

    public async Task<TResult> SendCommandAsync<TCommand, TResult>(
        TCommand command,
        string? queue = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        var destinationQueue = queue ?? typeof(TCommand).Name;
        var responseCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var responseChannel = Channel.CreateBounded<TResult>(
            new BoundedChannelOptions(1) { SingleReader = true, SingleWriter = true }
        );

        _responseChannels[responseCorrelationId] = responseChannel;

        try
        {
            var envelope = new MessageEnvelope<TCommand>
            {
                Payload = command,
                Destination = destinationQueue,
                CorrelationId = responseCorrelationId,
                ReplyTo = $"response-{responseCorrelationId}",
            };

            await _transport.SendAsync(destinationQueue, envelope, cancellationToken);

            return await responseChannel.Reader.ReadAsync(cancellationToken);
        }
        finally
        {
            _responseChannels.TryRemove(responseCorrelationId, out _);
        }
    }

    public async Task SendCommandAsync<TCommand>(
        TCommand command,
        string? queue = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        var destinationQueue = queue ?? typeof(TCommand).Name;

        var envelope = new MessageEnvelope<TCommand>
        {
            Payload = command,
            Destination = destinationQueue,
            CorrelationId = correlationId,
        };

        await _transport.SendAsync(destinationQueue, envelope, cancellationToken);
    }

    internal void SendResponse<TResult>(string correlationId, TResult result)
    {
        if (
            _responseChannels.TryGetValue(correlationId, out var channelObj)
            && channelObj is Channel<TResult> channel
        )
        {
            channel.Writer.TryWrite(result);
            channel.Writer.TryComplete();
        }
    }
}
