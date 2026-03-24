using Microsoft.AspNetCore.Builder;

namespace EcoData.Common.Messaging.Endpoints;

/// <summary>
/// Fluent builder for configuring command handlers via minimal API.
/// </summary>
/// <typeparam name="TCommand">The type of command.</typeparam>
/// <typeparam name="TResult">The type of result.</typeparam>
public sealed class CommandEndpointBuilder<TCommand, TResult>
{
    private readonly string _queue;
    private readonly RouteHandlerBuilder? _routeBuilder;

    internal CommandEndpointBuilder(string queue, RouteHandlerBuilder? routeBuilder = null)
    {
        _queue = queue;
        _routeBuilder = routeBuilder;
    }

    /// <summary>
    /// Gets the queue name for this command handler.
    /// </summary>
    public string Queue => _queue;

    /// <summary>
    /// Configures the underlying route handler.
    /// </summary>
    public CommandEndpointBuilder<TCommand, TResult> WithRouteOptions(Action<RouteHandlerBuilder> configure)
    {
        if (_routeBuilder is not null)
        {
            configure(_routeBuilder);
        }
        return this;
    }
}
