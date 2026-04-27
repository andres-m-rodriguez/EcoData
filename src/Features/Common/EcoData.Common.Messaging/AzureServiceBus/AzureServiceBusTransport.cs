using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Azure.Messaging.ServiceBus;
using EcoData.Common.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcoData.Common.Messaging.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of <see cref="IMessageTransport"/>.
/// Pub/sub only in this iteration; queue (point-to-point) APIs are not yet implemented.
/// Topology (topic + subscription) is expected to be provisioned externally — by Aspire
/// in dev (emulator) and by Bicep in production. The transport does not self-heal it,
/// because the Service Bus emulator does not expose the management endpoint that
/// <see cref="Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient"/>
/// requires, and a stale check there silently burns the call's timeout budget.
/// </summary>
public sealed class AzureServiceBusTransport : IMessageTransport, IAsyncDisposable
{
    private const string LogicalTopicProperty = "LogicalTopic";
    private const string MessageTypeProperty = "MessageType";

    private readonly AzureServiceBusOptions _options;
    private readonly ILogger<AzureServiceBusTransport> _logger;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public AzureServiceBusTransport(
        IOptions<AzureServiceBusOptions> options,
        ILogger<AzureServiceBusTransport> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException(
                $"{AzureServiceBusOptions.SectionName}:ConnectionString is required when using the Azure Service Bus transport.");
        }

        _client = new ServiceBusClient(_options.ConnectionString);
        _sender = _client.CreateSender(_options.TopicName);
    }

    public async Task PublishAsync<T>(
        string topic,
        MessageEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(envelope.Payload);
        var message = new ServiceBusMessage(body)
        {
            MessageId = envelope.MessageId.ToString(),
            CorrelationId = envelope.CorrelationId,
            ContentType = "application/json",
            Subject = envelope.MessageType,
        };

        message.ApplicationProperties[LogicalTopicProperty] = topic;
        message.ApplicationProperties[MessageTypeProperty] = typeof(T).AssemblyQualifiedName;

        if (envelope.Metadata is not null)
        {
            foreach (var (key, value) in envelope.Metadata)
            {
                message.ApplicationProperties[key] = value;
            }
        }

        _logger.LogInformation(
            "Publishing message {MessageId} to logical topic {LogicalTopic} on Service Bus topic {ServiceBusTopic}",
            message.MessageId,
            topic,
            _options.TopicName);

        await _sender.SendMessageAsync(message, cancellationToken);

        _logger.LogInformation("Published message {MessageId}", message.MessageId);
    }

    public async IAsyncEnumerable<MessageEnvelope<T>> SubscribeAsync<T>(
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<MessageEnvelope<T>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        var subscriptionName = ResolveSubscriptionName<T>();
        var processor = _client.CreateProcessor(
            _options.TopicName,
            subscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1,
            });

        processor.ProcessMessageAsync += async args =>
        {
            args.Message.ApplicationProperties.TryGetValue(LogicalTopicProperty, out var msgTopic);
            _logger.LogInformation(
                "Received message {MessageId} with logical topic {ReceivedTopic}; subscriber expects {ExpectedTopic}",
                args.Message.MessageId,
                msgTopic,
                topic);

            // Use CancellationToken.None for ack/dead-letter calls. args.CancellationToken
            // gets cancelled when the processor stops, which races the iterator's finally
            // block when the consumer exits. StopProcessingAsync waits for in-flight handlers
            // anyway, so detaching the ack from that token avoids spurious TaskCanceledExceptions
            // and the message redelivery they'd cause.
            if (msgTopic is null || !string.Equals(msgTopic.ToString(), topic, StringComparison.Ordinal))
            {
                await args.CompleteMessageAsync(args.Message, CancellationToken.None);
                return;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<T>(args.Message.Body.ToArray());
                if (payload is null)
                {
                    await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", "Payload was null", CancellationToken.None);
                    return;
                }

                var envelope = new MessageEnvelope<T>
                {
                    Payload = payload,
                    Destination = topic,
                    CorrelationId = args.Message.CorrelationId,
                    MessageId = Guid.TryParse(args.Message.MessageId, out var id) ? id : Guid.NewGuid(),
                    Timestamp = args.Message.EnqueuedTime,
                };

                await channel.Writer.WriteAsync(envelope, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle Service Bus message {MessageId} on topic {Topic}", args.Message.MessageId, topic);
                await args.DeadLetterMessageAsync(args.Message, "HandlerException", ex.Message, CancellationToken.None);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus processor error on {EntityPath}", args.EntityPath);
            return Task.CompletedTask;
        };

        _logger.LogInformation(
            "Starting Service Bus processor for logical topic {LogicalTopic} on subscription {Subscription}",
            topic,
            subscriptionName);

        await processor.StartProcessingAsync(cancellationToken);

        _logger.LogInformation("Service Bus processor started for {LogicalTopic}", topic);

        try
        {
            await foreach (var envelope in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return envelope;
            }
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
            await processor.DisposeAsync();
            channel.Writer.TryComplete();
        }
    }

    private string ResolveSubscriptionName<T>()
    {
        var typeName = typeof(T).Name.ToLowerInvariant();
        return string.IsNullOrEmpty(_options.SubscriptionPrefix)
            ? typeName
            : _options.SubscriptionPrefix + typeName;
    }

    public Task SendAsync<T>(string queue, MessageEnvelope<T> envelope, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Queue (point-to-point) messaging is not implemented in this iteration.");

    public IAsyncEnumerable<MessageEnvelope<T>> ReceiveAsync<T>(string queue, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Queue (point-to-point) messaging is not implemented in this iteration.");

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
