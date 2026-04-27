namespace EcoData.Common.Messaging.AzureServiceBus;

/// <summary>
/// Options for the Azure Service Bus transport.
/// </summary>
public sealed class AzureServiceBusOptions
{
    public const string SectionName = "Messaging:ServiceBus";

    /// <summary>
    /// Service Bus namespace connection string. For local dev this can point at the Service Bus emulator.
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
