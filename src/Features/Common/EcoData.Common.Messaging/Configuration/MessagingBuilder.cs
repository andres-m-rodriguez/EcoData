using EcoData.Common.Messaging.Abstractions;
using EcoData.Common.Messaging.Handlers;
using EcoData.Common.Messaging.InMemory;
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
    /// Configures the messaging system to use in-memory transport.
    /// </summary>
    public MessagingBuilder UseInMemoryTransport()
    {
        if (_transportConfigured)
        {
            throw new InvalidOperationException("A transport has already been configured.");
        }

        _services.AddSingleton<IMessageTransport, InMemoryTransport>();
        _services.AddSingleton<IMessageBus, InMemoryMessageBus>();
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
            UseInMemoryTransport();
        }
    }
}
