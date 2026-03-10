using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class ApiKeyEndpoints
{
    public static IEndpointRouteBuilder MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/api-keys")
            .WithTags("API Keys")
            .RequireAuthorization("Admin");

        group.MapGet("/", GetApiKeys).WithName("GetApiKeys");
        group.MapGet("/{id:guid}", GetApiKeyById).WithName("GetApiKeyById");
        group.MapPost("/", CreateApiKey).WithName("CreateApiKey");
        group.MapDelete("/{id:guid}", RevokeApiKey).WithName("RevokeApiKey");

        return app;
    }

    private static async Task<IResult> GetApiKeys(
        Guid organizationId,
        IApiKeyRepository repository,
        IOrganizationRepository organizationRepository,
        CancellationToken ct
    )
    {
        var organization = await organizationRepository.GetByIdAsync(organizationId, ct);
        if (organization is null)
        {
            return Results.NotFound("Organization not found");
        }

        var keys = await repository.GetByOrganizationAsync(organizationId, ct);
        return Results.Ok(keys);
    }

    private static async Task<IResult> GetApiKeyById(
        Guid organizationId,
        Guid id,
        IApiKeyRepository repository,
        CancellationToken ct
    )
    {
        var key = await repository.GetByIdAsync(id, ct);
        if (key is null || key.OrganizationId != organizationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(key);
    }

    private static async Task<IResult> CreateApiKey(
        Guid organizationId,
        ApiKeyDtoForCreate dto,
        IApiKeyRepository repository,
        IOrganizationRepository organizationRepository,
        CancellationToken ct
    )
    {
        var organization = await organizationRepository.GetByIdAsync(organizationId, ct);
        if (organization is null)
        {
            return Results.NotFound("Organization not found");
        }

        var created = await repository.CreateAsync(organizationId, dto, ct);
        return Results.Created(
            $"/api/organizations/{organizationId}/api-keys/{created.Id}",
            created
        );
    }

    private static async Task<IResult> RevokeApiKey(
        Guid organizationId,
        Guid id,
        IApiKeyRepository repository,
        CancellationToken ct
    )
    {
        var key = await repository.GetByIdAsync(id, ct);
        if (key is null || key.OrganizationId != organizationId)
        {
            return Results.NotFound();
        }

        var revoked = await repository.RevokeAsync(id, ct);
        return revoked ? Results.NoContent() : Results.NotFound();
    }
}
