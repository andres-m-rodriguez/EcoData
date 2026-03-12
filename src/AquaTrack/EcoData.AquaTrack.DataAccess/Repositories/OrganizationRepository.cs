using System.Runtime.CompilerServices;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class OrganizationRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IOrganizationRepository
{
    public async IAsyncEnumerable<OrganizationDtoForList> GetOrganizationsAsync(
        OrganizationParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Organizations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(search));
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(o => o.Id > parameters.Cursor.Value);
        }

        query = query.OrderBy(o => o.Id).Take(parameters.PageSize);

        await foreach (
            var organization in query
                .Select(o => new OrganizationDtoForList(
                    o.Id,
                    o.Name,
                    o.ProfilePictureUrl,
                    o.CardPictureUrl,
                    o.AboutUs,
                    o.WebsiteUrl,
                    o.CreatedAt
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return organization;
        }
    }

    public async Task<OrganizationDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Organizations.Where(o => o.Id == id)
            .Select(o => new OrganizationDtoForDetail(
                o.Id,
                o.Name,
                o.ProfilePictureUrl,
                o.CardPictureUrl,
                o.AboutUs,
                o.WebsiteUrl,
                o.CreatedAt,
                o.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OrganizationDtoForCreated?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Organizations.Where(o => o.Name == name)
            .Select(o => new OrganizationDtoForCreated(o.Id, o.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Organizations.AnyAsync(o => o.Name == name, cancellationToken);
    }

    public async Task<OrganizationDtoForCreated> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = dto.Name,
            ProfilePictureUrl = dto.ProfilePictureUrl,
            CardPictureUrl = dto.CardPictureUrl,
            AboutUs = dto.AboutUs,
            WebsiteUrl = dto.WebsiteUrl,
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.Organizations.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new OrganizationDtoForCreated(entity.Id, entity.Name);
    }

    public async Task<OrganizationDtoForDetail?> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context
            .Organizations.AsTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Name = dto.Name;
        entity.ProfilePictureUrl = dto.ProfilePictureUrl;
        entity.CardPictureUrl = dto.CardPictureUrl;
        entity.AboutUs = dto.AboutUs;
        entity.WebsiteUrl = dto.WebsiteUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return new OrganizationDtoForDetail(
            entity.Id,
            entity.Name,
            entity.ProfilePictureUrl,
            entity.CardPictureUrl,
            entity.AboutUs,
            entity.WebsiteUrl,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context
            .Organizations.AsTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        context.Organizations.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async IAsyncEnumerable<MyOrganizationDto> GetMyOrganizationsAsync(
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context
            .OrganizationMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Include(m => m.Organization)
            .Include(m => m.Role)
            .Select(m => new MyOrganizationDto(
                m.Organization!.Id,
                m.Organization.Name,
                m.Organization.ProfilePictureUrl,
                m.Organization.WebsiteUrl,
                m.Role!.Name
            ));

        await foreach (var org in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return org;
        }
    }
}
