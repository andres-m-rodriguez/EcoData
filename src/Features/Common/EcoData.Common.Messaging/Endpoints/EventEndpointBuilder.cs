using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Common.Messaging.Endpoints;

/// <summary>
/// Fluent builder for configuring event subscriptions via minimal API.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
public sealed class EventEndpointBuilder<TEvent>
{
    private readonly RouteHandlerBuilder _routeBuilder;
    private Func<HttpContext, string>? _topicResolver;
    private string? _staticTopic;

    internal EventEndpointBuilder(RouteHandlerBuilder routeBuilder)
    {
        _routeBuilder = routeBuilder;
    }

    /// <summary>
    /// Sets a static topic to subscribe to.
    /// </summary>
    /// <param name="topic">The topic name.</param>
    public EventEndpointBuilder<TEvent> WithTopic(string topic)
    {
        _staticTopic = topic;
        return this;
    }

    /// <summary>
    /// Sets a dynamic topic resolver based on the HTTP context.
    /// </summary>
    /// <param name="topicResolver">Function to resolve the topic from the request context.</param>
    public EventEndpointBuilder<TEvent> WithTopic(Func<HttpContext, string> topicResolver)
    {
        _topicResolver = topicResolver;
        return this;
    }

    /// <summary>
    /// Gets the resolved topic for a given context.
    /// </summary>
    internal string GetTopic(HttpContext context)
    {
        if (_staticTopic is not null)
        {
            return _staticTopic;
        }

        if (_topicResolver is not null)
        {
            return _topicResolver(context);
        }

        return typeof(TEvent).Name;
    }

    /// <summary>
    /// Configures the underlying route handler.
    /// </summary>
    public EventEndpointBuilder<TEvent> WithRouteOptions(Action<RouteHandlerBuilder> configure)
    {
        configure(_routeBuilder);
        return this;
    }

    internal RouteHandlerBuilder RouteBuilder => _routeBuilder;
}
