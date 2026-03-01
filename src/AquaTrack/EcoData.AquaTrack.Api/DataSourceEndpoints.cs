using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class DataSourceEndpoints
{
    public static IEndpointRouteBuilder MapDataSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/datasources").WithTags("DataSources");

        group.MapGet("/", GetDataSources).WithName("GetDataSources");

        return app;
    }

    private static async Task<IReadOnlyList<DataSourceDtoForList>> GetDataSources(
        IDataSourceRepository repository,
        CancellationToken ct
    ) => await repository.GetAllAsync(ct);
}
