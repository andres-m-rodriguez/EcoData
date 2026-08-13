using EcoData.Wildlife.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Mcp;

/// <summary>
/// Contributes the wildlife tools to whatever MCP server the host is building.
///
/// <para>The feature adds tools rather than standing up a server of its own:
/// a connector is one endpoint with one tool list, so the host owns the server
/// and each feature hands it what it can answer.</para>
///
/// <para>The tools read through the same repositories the HTTP endpoints use,
/// in the same request scope, so there is no second copy of the query logic and
/// no loopback call into our own API.</para>
/// </summary>
public static class WildlifeMcpExtensions
{
    public static IMcpServerBuilder AddWildlifeMcpTools(this IMcpServerBuilder builder) =>
        builder
            .WithTools<SpeciesTools>()
            .WithTools<ConservationTools>();
}
