using EcoData.Common.Messaging.Abstractions;
using EcoData.Common.Messaging.AzureServiceBus;
using EcoData.Common.Messaging.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Common.Messaging.Configuration;

/// <summary>
/// Fluent builder for configuring the messaging system.
/// </summary>
public sealed class MessagingBuilder
{
    private readonly IServiceCollection _services;
    private bool _transportConfigured;

    public MessagingBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Configures the messaging system to use the Azure Service Bus transport.
    /// Pub/sub only in this iteration; command APIs throw <see cref="NotSupportedException"/>.
    /// </summary>
    public MessagingBuilder UseAzureServiceBus(Action<AzureServiceBusOptions> configure)
    {
        if (_transportConfigured)
        {
            throw new InvalidOperationException("A transport has already been configured.");
        }

        _services.Configure(configure);
        _services.AddSingleton<IMessageTransport, AzureServiceBusTransport>();
        _services.AddSingleton<IMessageBus, AzureServiceBusMessageBus>();
        _transportConfigured = true;

        return this;
    }

    /// <summary>
    /// Configures the messaging system to use the Azure Service Bus transport, binding options from configuration.
    /// </summary>
    public MessagingBuilder UseAzureServiceBus(IConfiguration configuration)
    {
        if (_transportConfigured)
        {
            throw new InvalidOperationException("A transport has already been configured.");
        }

        _services.Configure<AzureServiceBusOptions>(configuration);
        _services.AddSingleton<IMessageTransport, AzureServiceBusTransport>();
        _services.AddSingleton<IMessageBus, AzureServiceBusMessageBus>();
        _transportConfigured = true;

        return this;
    }

    /// <summary>
    /// Registers an event handler.
    /// </summary>
    public MessagingBuilder AddEventHandler<TEvent, THandler>()
        where THandler : class, IEventHandler<TEvent>
    {
        _services.AddScoped<IEventHandler<TEvent>, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a command handler.
    /// </summary>
    public MessagingBuilder AddCommandHandler<TCommand, TResult, THandler>()
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        _services.AddScoped<ICommandHandler<TCommand, TResult>, THandler>();
        return this;
    }

    /// <summary>
    /// Configures messaging options.
    /// </summary>
    public MessagingBuilder Configure(Action<MessagingOptions> configure)
    {
        _services.Configure(configure);
        return this;
    }

    internal void EnsureTransportConfigured()
    {
        if (!_transportConfigured)
        {
            throw new InvalidOperationException(
                "No messaging transport configured. Call UseAzureServiceBus(...) on the MessagingBuilder.");
        }
    }
}
