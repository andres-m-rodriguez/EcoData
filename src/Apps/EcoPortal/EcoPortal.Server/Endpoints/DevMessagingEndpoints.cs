using EcoData.Common.Messaging.Abstractions;

namespace EcoPortal.Server.Endpoints;

/// <summary>
/// Dev-only endpoint that publishes a <see cref="DemoEvent"/> through <see cref="IMessageBus"/>
/// and waits for it to come back through a subscription. Proves the configured transport
/// (InMemory or AzureServiceBus) round-trips end-to-end without needing a real publisher.
/// </summary>
public static class DevMessagingEndpoints
{
    private const string Topic = "dev-messaging-roundtrip";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public sealed record DemoEvent(string Marker, string Message, DateTimeOffset SentAt)
    {
        public const string SubscriptionName = "demoevent";
    }

    public static IEndpointRouteBuilder MapDevMessagingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Run requests sequentially. Multiple concurrent calls share the same Service Bus
        // subscription (competing consumer); a non-matching processor will discard a message
        // intended for another caller. The SSE hybrid bridge in the migration plan removes
        // this constraint.
        endpoints.MapGet("/dev/messaging/roundtrip", async (
            string? message,
            IMessageBus bus,
            CancellationToken ct) =>
        {
            var marker = Guid.NewGuid().ToString("N");
            var payload = new DemoEvent(marker, message ?? "hello service bus", DateTimeOffset.UtcNow);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(Timeout);

            // Start the subscriber first so we don't race the publish.
            var receiveTask = WaitForMatchingAsync(bus, marker, timeoutCts.Token);

            await bus.PublishEventAsync(payload, topic: Topic, cancellationToken: ct);

            try
            {
                var received = await receiveTask;
                return Results.Ok(new
                {
                    success = true,
                    marker,
                    sent = payload,
                    received,
                    roundTripMs = (DateTimeOffset.UtcNow - payload.SentAt).TotalMilliseconds,
                });
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                return Results.Problem(
                    title: "Round-trip timed out",
                    detail: $"No matching message received within {Timeout.TotalSeconds:0}s for marker {marker}",
                    statusCode: StatusCodes.Status504GatewayTimeout);
            }
        }).AllowAnonymous();

        return endpoints;
    }

    private static async Task<DemoEvent> WaitForMatchingAsync(
        IMessageBus bus,
        string marker,
        CancellationToken ct)
    {
        await foreach (var evt in bus.SubscribeToEventsAsync<DemoEvent>(Topic, ct))
        {
            if (evt.Marker == marker)
            {
                return evt;
            }
        }

        throw new OperationCanceledException(ct);
    }
}
