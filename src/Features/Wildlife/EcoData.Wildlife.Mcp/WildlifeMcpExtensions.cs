using EcoData.Wildlife.Mcp.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Mcp;

/// <summary>
/// Mounts the wildlife catalogue as an MCP server so it can be added to Claude
/// (or any MCP client) as a custom connector.
///
/// <para>The transport is Streamable HTTP, which is what a remote connector
/// requires — a stdio server can only be reached from the machine it runs on.
/// The tools read through the same repositories the HTTP endpoints use, in the
/// same request scope, so there is no second copy of the query logic and no
/// loopback call into our own API.</para>
///
/// <para>Nothing here is authenticated, matching the wildlife endpoints
/// themselves: the catalogue is public reference data and every tool is
/// read-only. Anything that later exposes per-user or write access needs OAuth
/// in front of it first.</para>
/// </summary>
public static class WildlifeMcpExtensions
{
    public static IServiceCollection AddWildlifeMcp(this IServiceCollection services)
    {
        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<SpeciesTools>()
            .WithTools<ConservationTools>();

        return services;
    }

    /// <summary>
    /// Serves the connector at <c>/mcp</c>. This is the URL a reader pastes into
    /// their client, so it is deliberately boring and stable.
    /// </summary>
    public static IEndpointRouteBuilder MapWildlifeMcp(this IEndpointRouteBuilder app)
    {
        app.MapMcp("/mcp");

        return app;
    }
}
