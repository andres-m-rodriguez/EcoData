using EcoData.Wildlife.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Mcp;

public static class WildlifeMcpExtensions
{
    public static IMcpServerBuilder AddWildlifeMcpTools(this IMcpServerBuilder builder) =>
        builder
            .WithTools<SpeciesTools>()
            .WithTools<ConservationTools>();
}
