using EcoData.Locations.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Locations.Mcp;

public static class LocationsMcpExtensions
{
    public static IMcpServerBuilder AddLocationsMcpTools(this IMcpServerBuilder builder) =>
        builder.WithTools<MunicipalityTools>();
}
