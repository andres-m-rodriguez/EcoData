using System.Text.Json;
using EcoData.Common.Messaging.Abstractions;
using EcoData.Common.Messaging.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcoData.Common.Messaging.Endpoints;

/// <summary>
/// Extension methods for mapping event and command endpoints in minimal API style.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps an SSE streaming endpoint for events of type TEvent.
    /// </summary>
    public static EventEndpointBuilder<TEvent> MapEventStream<TEvent>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string? sseEventType = null)
    {
        var builder = new EventEndpointBuilder<TEvent>(
            endpoints.MapGet(pattern, async (HttpContext context, IMessageBus bus, CancellationToken ct) =>
            {
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var topic = context.GetRouteValue("topic")?.ToString()
                    ?? context.Request.Query["topic"].FirstOrDefault()
                    ?? typeof(TEvent).Name;

                var eventType = sseEventType ?? typeof(TEvent).Name;

                await foreach (var @event in bus.SubscribeToEventsAsync<TEvent>(topic, ct))
                {
                    var json = JsonSerializer.Serialize(@event);
                    await context.Response.WriteAsync($"event: {eventType}\n", ct);
                    await context.Response.WriteAsync($"data: {json}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                }
            }));

        return builder;
    }

    /// <summary>
    /// Maps a background event handler for events of type TEvent.
    /// </summary>
    public static void MapEvent<TEvent>(
        this IEndpointRouteBuilder endpoints,
        string topic,
        Func<TEvent, EventContext, Task> handler)
    {
        var services = endpoints.ServiceProvider;
        var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
        var bus = services.GetRequiredService<IMessageBus>();

        _ = Task.Run(async () =>
        {
            var ct = lifetime.ApplicationStopping;

            await foreach (var @event in bus.SubscribeToEventsAsync<TEvent>(topic, ct))
            {
                var context = new EventContext
                {
                    Topic = topic,
                    MessageId = Guid.NewGuid(),
                    Timestamp = DateTimeOffset.UtcNow
                };

                try
                {
                    await handler(@event, context);
                }
                catch
                {
                    // Handler failed, continue processing
                }
            }
        });
    }

    /// <summary>
    /// Maps a background command handler.
    /// </summary>
    public static CommandEndpointBuilder<TCommand, TResult> MapCommand<TCommand, TResult>(
        this IEndpointRouteBuilder endpoints,
        string queue,
        Func<TCommand, CommandContext, Task<TResult>> handler)
    {
        var services = endpoints.ServiceProvider;
        var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
        var transport = services.GetRequiredService<IMessageTransport>();
        var bus = services.GetRequiredService<IMessageBus>();

        _ = Task.Run(async () =>
        {
            var ct = lifetime.ApplicationStopping;

            await foreach (var envelope in transport.ReceiveAsync<TCommand>(queue, ct))
            {
                var context = new CommandContext
                {
                    Queue = queue,
                    MessageId = envelope.MessageId,
                    CorrelationId = envelope.CorrelationId,
                    Timestamp = envelope.Timestamp,
                    ReplyTo = envelope.ReplyTo,
                    Metadata = envelope.Metadata
                };

                try
                {
                    var result = await handler(envelope.Payload, context);

                    if (envelope.CorrelationId is not null && bus is InMemory.InMemoryMessageBus inMemoryBus)
                    {
                        inMemoryBus.SendResponse(envelope.CorrelationId, result);
                    }
                }
                catch
                {
                    // Handler failed
                }
            }
        });

        return new CommandEndpointBuilder<TCommand, TResult>(queue);
    }
}
