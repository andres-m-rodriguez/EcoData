using EcoData.Locations.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Locations.Mcp;

/// <summary>
/// Contributes the location tools to whatever MCP server the host is building.
///
/// <para>The feature adds tools rather than standing up a server of its own:
/// a connector is one endpoint with one tool list, so the host owns the server
/// and each feature hands it what it can answer.</para>
/// </summary>
public static class LocationsMcpExtensions
{
    public static IMcpServerBuilder AddLocationsMcpTools(this IMcpServerBuilder builder) =>
        builder.WithTools<MunicipalityTools>();
}
