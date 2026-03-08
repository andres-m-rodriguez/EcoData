using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations").WithTags("Organizations");

        group.MapGet("/", GetOrganizations).WithName("GetOrganizations");
        group.MapGet("/{id:guid}", GetOrganizationById).WithName("GetOrganizationById");
        group
            .MapPost("/", CreateOrganization)
            .WithName("CreateOrganization")
            .RequireAuthorization("Admin");
        group
            .MapPut("/{id:guid}", UpdateOrganization)
            .WithName("UpdateOrganization")
            .RequireAuthorization("Admin");
        group
            .MapDelete("/{id:guid}", DeleteOrganization)
            .WithName("DeleteOrganization")
            .RequireAuthorization("Admin");

        return app;
    }

    private static IAsyncEnumerable<OrganizationDtoForList> GetOrganizations(
        [AsParameters] OrganizationParameters parameters,
        IOrganizationRepository repository,
        CancellationToken ct
    ) => repository.GetOrganizationsAsync(parameters, ct);

    private static async Task<IResult> GetOrganizationById(
        Guid id,
        IOrganizationRepository repository,
        CancellationToken ct
    )
    {
        var organization = await repository.GetByIdAsync(id, ct);
        return organization is null ? Results.NotFound() : Results.Ok(organization);
    }

    private static async Task<IResult> CreateOrganization(
        OrganizationDtoForCreate dto,
        IOrganizationRepository repository,
        CancellationToken ct
    )
    {
        var exists = await repository.ExistsAsync(dto.Name, ct);
        if (exists)
        {
            return Results.Conflict("An organization with this name already exists.");
        }

        var created = await repository.CreateAsync(dto, ct);
        return Results.Created($"/api/organizations/{created.Id}", created);
    }

    private static async Task<IResult> UpdateOrganization(
        Guid id,
        OrganizationDtoForUpdate dto,
        IOrganizationRepository repository,
        CancellationToken ct
    )
    {
        var updated = await repository.UpdateAsync(id, dto, ct);
        return updated is null ? Results.NotFound() : Results.Ok(updated);
    }

    private static async Task<IResult> DeleteOrganization(
        Guid id,
        IOrganizationRepository repository,
        CancellationToken ct
    )
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
