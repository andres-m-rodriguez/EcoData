using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Common.Messaging;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an in-memory message broker as a singleton.
    /// Use this for single-instance deployments.
    /// </summary>
    /// <typeparam name="T">The type of message being published/subscribed.</typeparam>
    public static IServiceCollection AddInMemoryMessageBroker<T>(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBroker<T>, InMemoryMessageBroker<T>>();
        return services;
    }
}
