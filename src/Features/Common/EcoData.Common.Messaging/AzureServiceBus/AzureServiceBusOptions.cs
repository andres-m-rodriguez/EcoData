namespace EcoData.Common.Messaging.AzureServiceBus;

/// <summary>
/// Options for the Azure Service Bus transport.
/// </summary>
public sealed class AzureServiceBusOptions
{
    public const string SectionName = "Messaging:ServiceBus";

    /// <summary>
    /// How to reach the Service Bus namespace. Accepts either shape that Aspire hands us:
    /// <list type="bullet">
    /// <item><description>
    /// A full connection string containing <c>Endpoint=</c> (and a shared access key) — this is what
    /// the local Service Bus emulator supplies, e.g.
    /// <c>Endpoint=sb://localhost:5672;SharedAccessKeyName=...;SharedAccessKey=...;UseDevelopmentEmulator=true</c>.
    /// </description></item>
    /// <item><description>
    /// A bare namespace endpoint with no key, e.g. <c>https://contoso.servicebus.windows.net:443/</c> —
    /// this is what a provisioned namespace supplies when authentication is via managed identity.
    /// </description></item>
    /// </list>
    /// The property name is kept for compatibility with the <c>Messaging:ServiceBus:ConnectionString</c>
    /// configuration key that AppHost sets; the transport picks the right client overload at runtime.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the topic that all events are published to. Subscribers route by app-properties.
    /// </summary>
    public string TopicName { get; set; } = "ecodata-events";

    /// <summary>
    /// Optional prefix prepended to per-type subscription names (e.g. <c>"ecoportal-"</c> →
    /// <c>"ecoportal-demoevent"</c>). Lets multi-instance deployments disambiguate. Empty by default.
    /// </summary>
    public string SubscriptionPrefix { get; set; } = string.Empty;
}
