using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using EcoData.Common.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcoData.Common.Messaging.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of <see cref="IMessageTransport"/>.
/// Pub/sub only in this iteration; queue (point-to-point) APIs are not yet implemented.
/// </summary>
public sealed class AzureServiceBusTransport : IMessageTransport, IAsyncDisposable
{
    private const string LogicalTopicProperty = "LogicalTopic";
    private const string MessageTypeProperty = "MessageType";

    private readonly AzureServiceBusOptions _options;
    private readonly ILogger<AzureServiceBusTransport> _logger;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly ServiceBusSender _sender;
    private readonly SemaphoreSlim _topologyLock = new(1, 1);
    private bool _topologyEnsured;

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
        _adminClient = new ServiceBusAdministrationClient(_options.ConnectionString);
        _sender = _client.CreateSender(_options.TopicName);
    }

    public async Task PublishAsync<T>(
        string topic,
        MessageEnvelope<T> envelope,
        CancellationToken cancellationToken = default)
    {
        await EnsureTopologyAsync(cancellationToken);

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

        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public async IAsyncEnumerable<MessageEnvelope<T>> SubscribeAsync<T>(
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureTopologyAsync(cancellationToken);

        var channel = Channel.CreateUnbounded<MessageEnvelope<T>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        var processor = _client.CreateProcessor(
            _options.TopicName,
            _options.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1,
            });

        processor.ProcessMessageAsync += async args =>
        {
            if (!args.Message.ApplicationProperties.TryGetValue(LogicalTopicProperty, out var msgTopic)
                || !string.Equals(msgTopic?.ToString(), topic, StringComparison.Ordinal))
            {
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<T>(args.Message.Body.ToArray());
                if (payload is null)
                {
                    await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", "Payload was null", args.CancellationToken);
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
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle Service Bus message {MessageId} on topic {Topic}", args.Message.MessageId, topic);
                await args.DeadLetterMessageAsync(args.Message, "HandlerException", ex.Message, args.CancellationToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus processor error on {EntityPath}", args.EntityPath);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(cancellationToken);

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

    public Task SendAsync<T>(string queue, MessageEnvelope<T> envelope, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Queue (point-to-point) messaging is not implemented in this iteration.");

    public IAsyncEnumerable<MessageEnvelope<T>> ReceiveAsync<T>(string queue, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Queue (point-to-point) messaging is not implemented in this iteration.");

    private async Task EnsureTopologyAsync(CancellationToken cancellationToken)
    {
        if (_topologyEnsured)
        {
            return;
        }

        await _topologyLock.WaitAsync(cancellationToken);
        try
        {
            if (_topologyEnsured)
            {
                return;
            }

            // Best-effort: in production the topology is provisioned via infra-as-code
            // (Aspire-emitted Bicep), and the Service Bus emulator may reject admin
            // create operations. We try to self-heal in dev but never block startup on it.
            try
            {
                if (!await _adminClient.TopicExistsAsync(_options.TopicName, cancellationToken))
                {
                    await _adminClient.CreateTopicAsync(_options.TopicName, cancellationToken);
                }

                if (!await _adminClient.SubscriptionExistsAsync(_options.TopicName, _options.SubscriptionName, cancellationToken))
                {
                    await _adminClient.CreateSubscriptionAsync(_options.TopicName, _options.SubscriptionName, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not verify or auto-create Service Bus topology for topic {Topic}/{Subscription}; assuming it is provisioned externally.",
                    _options.TopicName,
                    _options.SubscriptionName);
            }

            _topologyEnsured = true;
        }
        finally
        {
            _topologyLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
        _topologyLock.Dispose();
    }
}
