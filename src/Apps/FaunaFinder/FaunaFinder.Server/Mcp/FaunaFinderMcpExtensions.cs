using EcoData.Locations.Mcp;
using EcoData.Wildlife.Mcp;

namespace FaunaFinder.Server.Mcp;

/// <summary>
/// Assembles FaunaFinder's MCP connector: one server, one endpoint, with each
/// feature contributing the tools it can answer.
///
/// <para>The transport is Streamable HTTP, which is what a remote connector
/// requires — a stdio server can only be reached from the machine it runs
/// on.</para>
///
/// <para>Nothing here is authenticated, matching the endpoints the tools read
/// through: the catalogue and the municipio list are public reference data and
/// every tool is read-only. Anything that later exposes per-user or write
/// access needs OAuth in front of it first.</para>
/// </summary>
public static class FaunaFinderMcpExtensions
{
    public static IServiceCollection AddFaunaFinderMcp(this IServiceCollection services)
    {
        services
            .AddMcpServer()
            .WithHttpTransport()
            .AddWildlifeMcpTools()
            .AddLocationsMcpTools();

        return services;
    }

    /// <summary>
    /// Serves the connector at <c>/mcp</c>. This is the URL a reader pastes into
    /// their client, so it is deliberately boring and stable.
    /// </summary>
    public static IEndpointRouteBuilder MapFaunaFinderMcp(this IEndpointRouteBuilder app)
    {
        app.MapMcp("/mcp");

        return app;
    }
}
