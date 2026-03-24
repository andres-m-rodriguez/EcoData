using EcoData.Common.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Common.Messaging;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the messaging infrastructure with fluent configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for the messaging builder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        Action<MessagingBuilder> configure
    )
    {
        var builder = new MessagingBuilder(services);
        configure(builder);
        builder.EnsureTransportConfigured();

        return services;
    }

    /// <summary>
    /// Adds the messaging infrastructure with default in-memory transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        return services.AddMessaging(builder => builder.UseInMemoryTransport());
    }
}
