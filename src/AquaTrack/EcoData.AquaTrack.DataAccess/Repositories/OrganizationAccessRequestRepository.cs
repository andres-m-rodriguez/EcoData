using System.Runtime.CompilerServices;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.Identity.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class OrganizationAccessRequestRepository(
    IDbContextFactory<AquaTrackDbContext> contextFactory,
    IUserLookupRepository userLookupRepository
) : IOrganizationAccessRequestRepository
{
    public async IAsyncEnumerable<OrganizationAccessRequestDto> GetByOrganizationAsync(
        Guid organizationId,
        OrganizationAccessRequestParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.OrganizationAccessRequests
            .Where(r => r.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(parameters.Status) &&
            Enum.TryParse<OrganizationAccessRequestStatus>(parameters.Status, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(r => r.Id.CompareTo(parameters.Cursor.Value) > 0);
        }

        var requests = await query
            .OrderBy(r => r.Id)
            .Take(parameters.PageSize)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                r.OrganizationId,
                OrganizationName = r.Organization!.Name,
                r.Status,
                r.RequestMessage,
                r.ReviewNotes,
                r.ReviewedByUserId,
                r.ReviewedAt,
                r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            yield break;
        }

        var userIds = requests
            .SelectMany(r => new[] { r.UserId, r.ReviewedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Concat(requests.Select(r => r.UserId))
            .Distinct();

        var users = await userLookupRepository.GetByIdsAsync(userIds, cancellationToken);

        foreach (var request in requests)
        {
            var user = users.GetValueOrDefault(request.UserId);
            var reviewer = request.ReviewedByUserId.HasValue
                ? users.GetValueOrDefault(request.ReviewedByUserId.Value)
                : null;

            yield return new OrganizationAccessRequestDto(
                request.Id,
                request.UserId,
                user?.Email ?? "",
                user?.DisplayName ?? "",
                request.OrganizationId,
                request.OrganizationName,
                request.Status.ToString(),
                request.RequestMessage,
                request.ReviewNotes,
                request.ReviewedByUserId,
                reviewer?.DisplayName,
                request.ReviewedAt,
                request.CreatedAt
            );
        }
    }

    public async IAsyncEnumerable<OrganizationAccessRequestDto> GetByUserAsync(
        Guid userId,
        OrganizationAccessRequestParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.OrganizationAccessRequests
            .Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(parameters.Status) &&
            Enum.TryParse<OrganizationAccessRequestStatus>(parameters.Status, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrEmpty(parameters.Search))
        {
            var searchLower = parameters.Search.ToLower();
            query = query.Where(r => r.Organization!.Name.ToLower().Contains(searchLower));
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(r => r.Id.CompareTo(parameters.Cursor.Value) > 0);
        }

        var requests = await query
            .OrderBy(r => r.Id)
            .Take(parameters.PageSize)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                r.OrganizationId,
                OrganizationName = r.Organization!.Name,
                r.Status,
                r.RequestMessage,
                r.ReviewNotes,
                r.ReviewedByUserId,
                r.ReviewedAt,
                r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            yield break;
        }

        var user = await userLookupRepository.GetByIdAsync(userId, cancellationToken);

        var reviewerIds = requests
            .Where(r => r.ReviewedByUserId.HasValue)
            .Select(r => r.ReviewedByUserId!.Value)
            .Distinct();

        var reviewers = await userLookupRepository.GetByIdsAsync(reviewerIds, cancellationToken);

        foreach (var request in requests)
        {
            var reviewer = request.ReviewedByUserId.HasValue
                ? reviewers.GetValueOrDefault(request.ReviewedByUserId.Value)
                : null;

            yield return new OrganizationAccessRequestDto(
                request.Id,
                request.UserId,
                user?.Email ?? "",
                user?.DisplayName ?? "",
                request.OrganizationId,
                request.OrganizationName,
                request.Status.ToString(),
                request.RequestMessage,
                request.ReviewNotes,
                request.ReviewedByUserId,
                reviewer?.DisplayName,
                request.ReviewedAt,
                request.CreatedAt
            );
        }
    }

    public async Task<OrganizationAccessRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var request = await context.OrganizationAccessRequests
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                r.OrganizationId,
                OrganizationName = r.Organization!.Name,
                r.Status,
                r.RequestMessage,
                r.ReviewNotes,
                r.ReviewedByUserId,
                r.ReviewedAt,
                r.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null)
        {
            return null;
        }

        var user = await userLookupRepository.GetByIdAsync(request.UserId, cancellationToken);
        var reviewer = request.ReviewedByUserId.HasValue
            ? await userLookupRepository.GetByIdAsync(request.ReviewedByUserId.Value, cancellationToken)
            : null;

        return new OrganizationAccessRequestDto(
            request.Id,
            request.UserId,
            user?.Email ?? "",
            user?.DisplayName ?? "",
            request.OrganizationId,
            request.OrganizationName,
            request.Status.ToString(),
            request.RequestMessage,
            request.ReviewNotes,
            request.ReviewedByUserId,
            reviewer?.DisplayName,
            request.ReviewedAt,
            request.CreatedAt
        );
    }

    public async Task<OrganizationAccessRequestDto> CreateAsync(
        Guid userId,
        Guid organizationId,
        string? requestMessage,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var organization = await context.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => new { o.Id, o.Name })
            .FirstAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new OrganizationAccessRequest
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            OrganizationId = organizationId,
            Status = OrganizationAccessRequestStatus.Pending,
            RequestMessage = requestMessage,
            ReviewNotes = null,
            ReviewedByUserId = null,
            ReviewedAt = null,
            CreatedAt = now,
        };

        context.OrganizationAccessRequests.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        var user = await userLookupRepository.GetByIdAsync(userId, cancellationToken);

        return new OrganizationAccessRequestDto(
            entity.Id,
            entity.UserId,
            user?.Email ?? "",
            user?.DisplayName ?? "",
            entity.OrganizationId,
            organization.Name,
            entity.Status.ToString(),
            entity.RequestMessage,
            null,
            null,
            null,
            null,
            entity.CreatedAt
        );
    }

    public async Task<OrganizationAccessRequestDto?> UpdateStatusAsync(
        Guid id,
        OrganizationAccessRequestStatus status,
        string? reviewNotes,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.OrganizationAccessRequests
            .AsTracking()
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = status;
        entity.ReviewNotes = reviewNotes;
        entity.ReviewedByUserId = reviewedByUserId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        var user = await userLookupRepository.GetByIdAsync(entity.UserId, cancellationToken);
        var reviewer = await userLookupRepository.GetByIdAsync(reviewedByUserId, cancellationToken);

        return new OrganizationAccessRequestDto(
            entity.Id,
            entity.UserId,
            user?.Email ?? "",
            user?.DisplayName ?? "",
            entity.OrganizationId,
            entity.Organization!.Name,
            entity.Status.ToString(),
            entity.RequestMessage,
            entity.ReviewNotes,
            entity.ReviewedByUserId,
            reviewer?.DisplayName,
            entity.ReviewedAt,
            entity.CreatedAt
        );
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.OrganizationAccessRequests
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        context.OrganizationAccessRequests.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<OrganizationAccessRequestDto?> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.OrganizationAccessRequests
            .AsTracking()
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = OrganizationAccessRequestStatus.Cancelled;

        await context.SaveChangesAsync(cancellationToken);

        var user = await userLookupRepository.GetByIdAsync(entity.UserId, cancellationToken);

        return new OrganizationAccessRequestDto(
            entity.Id,
            entity.UserId,
            user?.Email ?? "",
            user?.DisplayName ?? "",
            entity.OrganizationId,
            entity.Organization!.Name,
            entity.Status.ToString(),
            entity.RequestMessage,
            entity.ReviewNotes,
            entity.ReviewedByUserId,
            null,
            entity.ReviewedAt,
            entity.CreatedAt
        );
    }

    public async Task<bool> ExistsPendingAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.OrganizationAccessRequests.AnyAsync(
            r => r.UserId == userId
                 && r.OrganizationId == organizationId
                 && r.Status == OrganizationAccessRequestStatus.Pending,
            cancellationToken
        );
    }
}
