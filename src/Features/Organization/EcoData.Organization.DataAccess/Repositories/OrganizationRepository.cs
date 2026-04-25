using System.Runtime.CompilerServices;
using EcoData.Organization.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Organization.DataAccess.Colors;
using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Organization.DataAccess.Slugs;
using EcoData.Organization.Database;
using EcoData.Organization.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Organization.DataAccess.Repositories;

public sealed class OrganizationRepository(IDbContextFactory<OrganizationDbContext> contextFactory)
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
                    o.Slug,
                    o.Tagline,
                    o.ProfilePictureUrl,
                    o.CardPictureUrl,
                    o.AboutUs,
                    o.WebsiteUrl,
                    o.Location,
                    o.PrimaryColor,
                    o.AccentColor,
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
            .Select(ProjectToDetail)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OrganizationDtoForDetail?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Organizations.Where(o => o.Slug == slug)
            .Select(ProjectToDetail)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Database.Models.Organization, OrganizationDtoForDetail>> ProjectToDetail =
        o => new OrganizationDtoForDetail(
            o.Id,
            o.Name,
            o.Slug,
            o.Tagline,
            o.ProfilePictureUrl,
            o.CardPictureUrl,
            o.AboutUs,
            o.WebsiteUrl,
            o.Location,
            o.FoundedYear,
            o.LegalStatus,
            o.TaxId,
            o.PrimaryColor,
            o.AccentColor,
            o.CreatedAt,
            o.UpdatedAt
        );

    public async Task<OrganizationDtoForCreated?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Organizations.Where(o => o.Name == name)
            .Select(o => new OrganizationDtoForCreated(o.Id, o.Name, o.Slug))
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

        var slug = await ResolveUniqueSlugAsync(
            context,
            string.IsNullOrWhiteSpace(dto.Slug) ? SlugGenerator.FromName(dto.Name) : SlugGenerator.FromName(dto.Slug),
            excludeId: null,
            cancellationToken
        );

        var now = DateTimeOffset.UtcNow;
        var entity = new Database.Models.Organization
        {
            Id = Guid.CreateVersion7(),
            Name = dto.Name,
            Slug = slug,
            Tagline = dto.Tagline,
            ProfilePictureUrl = dto.ProfilePictureUrl,
            CardPictureUrl = dto.CardPictureUrl,
            AboutUs = dto.AboutUs,
            WebsiteUrl = dto.WebsiteUrl,
            Location = dto.Location,
            FoundedYear = dto.FoundedYear,
            LegalStatus = dto.LegalStatus,
            TaxId = dto.TaxId,
            PrimaryColor = HexColor.Normalize(dto.PrimaryColor),
            AccentColor = HexColor.Normalize(dto.AccentColor),
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.Organizations.Add(entity);

        // Seed default roles for the organization
        var defaultRoles = new[]
        {
            new OrganizationRole
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = entity.Id,
                Name = DefaultOrganizationRoles.Owner,
                CreatedAt = now,
            },
            new OrganizationRole
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = entity.Id,
                Name = DefaultOrganizationRoles.Admin,
                CreatedAt = now,
            },
            new OrganizationRole
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = entity.Id,
                Name = DefaultOrganizationRoles.Contributor,
                CreatedAt = now,
            },
            new OrganizationRole
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = entity.Id,
                Name = DefaultOrganizationRoles.Viewer,
                CreatedAt = now,
            },
        };
        context.OrganizationRoles.AddRange(defaultRoles);

        await context.SaveChangesAsync(cancellationToken);

        return new OrganizationDtoForCreated(entity.Id, entity.Name, entity.Slug);
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

        if (!string.IsNullOrWhiteSpace(dto.Slug))
        {
            var requested = SlugGenerator.FromName(dto.Slug);
            if (!string.Equals(requested, entity.Slug, StringComparison.Ordinal))
            {
                entity.Slug = await ResolveUniqueSlugAsync(context, requested, entity.Id, cancellationToken);
            }
        }

        entity.Name = dto.Name;
        entity.Tagline = dto.Tagline;
        entity.ProfilePictureUrl = dto.ProfilePictureUrl;
        entity.CardPictureUrl = dto.CardPictureUrl;
        entity.AboutUs = dto.AboutUs;
        entity.WebsiteUrl = dto.WebsiteUrl;
        entity.Location = dto.Location;
        entity.FoundedYear = dto.FoundedYear;
        entity.LegalStatus = dto.LegalStatus;
        entity.TaxId = dto.TaxId;
        entity.PrimaryColor = HexColor.Normalize(dto.PrimaryColor);
        entity.AccentColor = HexColor.Normalize(dto.AccentColor);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return new OrganizationDtoForDetail(
            entity.Id,
            entity.Name,
            entity.Slug,
            entity.Tagline,
            entity.ProfilePictureUrl,
            entity.CardPictureUrl,
            entity.AboutUs,
            entity.WebsiteUrl,
            entity.Location,
            entity.FoundedYear,
            entity.LegalStatus,
            entity.TaxId,
            entity.PrimaryColor,
            entity.AccentColor,
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
            .Select(m => new MyOrganizationDto(
                m.Organization!.Id,
                m.Organization.Name,
                m.Organization.Slug,
                m.Organization.ProfilePictureUrl,
                m.Organization.WebsiteUrl,
                m.Role!.Name
            ));

        await foreach (var org in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return org;
        }
    }

    // Generates a unique slug by appending -2, -3, ... until no other organization
    // owns the candidate. `excludeId` is set when updating so an org doesn't conflict
    // with its own current slug.
    private static async Task<string> ResolveUniqueSlugAsync(
        OrganizationDbContext context,
        string baseSlug,
        Guid? excludeId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(baseSlug))
        {
            throw new ArgumentException("Slug cannot be empty after normalization", nameof(baseSlug));
        }

        var candidate = baseSlug;
        var attempt = 2;

        while (
            await context.Organizations.AnyAsync(
                o => o.Slug == candidate && (excludeId == null || o.Id != excludeId),
                cancellationToken
            )
        )
        {
            candidate = $"{baseSlug}-{attempt++}";
        }

        return candidate;
    }
}
