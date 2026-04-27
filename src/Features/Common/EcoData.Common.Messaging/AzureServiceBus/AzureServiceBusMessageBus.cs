using System.Runtime.CompilerServices;
using EcoData.Common.Messaging.Abstractions;

namespace EcoData.Common.Messaging.AzureServiceBus;

/// <summary>
/// <see cref="IMessageBus"/> implementation backed by <see cref="AzureServiceBusTransport"/>.
/// Pub/sub only; command APIs are not implemented in this iteration.
/// </summary>
public sealed class AzureServiceBusMessageBus : IMessageBus
{
    private readonly IMessageTransport _transport;

    public AzureServiceBusMessageBus(IMessageTransport transport)
    {
        _transport = transport;
    }

    public Task PublishEventAsync<TEvent>(
        TEvent @event,
        string? topic = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var destination = topic ?? typeof(TEvent).Name;
        var envelope = new MessageEnvelope<TEvent>
        {
            Payload = @event,
            Destination = destination,
            CorrelationId = correlationId,
        };

        return _transport.PublishAsync(destination, envelope, cancellationToken);
    }

    public async IAsyncEnumerable<TEvent> SubscribeToEventsAsync<TEvent>(
        string? topic = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var destination = topic ?? typeof(TEvent).Name;

        await foreach (var envelope in _transport.SubscribeAsync<TEvent>(destination, cancellationToken))
        {
            yield return envelope.Payload;
        }
    }

    public Task<TResult> SendCommandAsync<TCommand, TResult>(
        TCommand command,
        string? queue = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command (request/response) messaging is not implemented in this iteration.");

    public Task SendCommandAsync<TCommand>(
        TCommand command,
        string? queue = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command (fire-and-forget) messaging is not implemented in this iteration.");
}
