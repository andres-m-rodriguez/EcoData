using EcoData.Identity.Application.Server.Services;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Database;
using EcoData.Identity.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Identity.Application.Services;

public sealed class SensorIdentityProviderService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IJwtTokenService jwtTokenService
) : ISensorIdentityProviderService
{
    public async Task<SensorProvisionResponse> ProvisionAsync(
        Guid sensorId,
        Guid organizationId,
        string organizationName,
        string sensorName,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var credential = new SensorCredential
        {
            SensorId = sensorId,
            OrganizationId = organizationId,
            OrganizationName = organizationName,
            SensorName = sensorName,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        context.SensorCredentials.Add(credential);
        await context.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = jwtTokenService.GenerateSensorToken(
            sensorId,
            organizationId,
            organizationName,
            sensorName
        );

        return new SensorProvisionResponse(sensorId, token, expiresAt);
    }

    public async Task<SensorTokenResponse?> RefreshTokenAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var credential = await context.SensorCredentials.FirstOrDefaultAsync(
            c => c.SensorId == sensorId,
            cancellationToken
        );

        if (credential is null)
            return null;

        var (token, expiresAt) = jwtTokenService.GenerateSensorToken(
            credential.SensorId,
            credential.OrganizationId,
            credential.OrganizationName,
            credential.SensorName
        );

        return new SensorTokenResponse(token, expiresAt);
    }

    public async Task<bool> IsProvisionedAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SensorCredentials.AnyAsync(
            c => c.SensorId == sensorId,
            cancellationToken
        );
    }

    public async Task RevokeAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var credential = await context.SensorCredentials.FirstOrDefaultAsync(
            c => c.SensorId == sensorId,
            cancellationToken
        );

        if (credential is not null)
        {
            context.SensorCredentials.Remove(credential);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
