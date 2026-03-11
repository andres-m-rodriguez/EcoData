using System.Runtime.CompilerServices;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.DataAccess.Interfaces;
using EcoData.Identity.Database;
using EcoData.Identity.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Identity.DataAccess.Repositories;

public sealed class AccessRequestRepository(IDbContextFactory<IdentityDbContext> contextFactory)
    : IAccessRequestRepository
{
    public async Task<AccessRequest?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AccessRequests.FirstOrDefaultAsync(
            ar => ar.Id == id,
            cancellationToken
        );
    }

    public async Task<AccessRequest?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AccessRequests.FirstOrDefaultAsync(
            ar => ar.Email.ToLower() == email.ToLower(),
            cancellationToken
        );
    }

    public async Task<AccessRequest> CreateAsync(
        AccessRequest accessRequest,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.AccessRequests.Add(accessRequest);
        await context.SaveChangesAsync(cancellationToken);
        return accessRequest;
    }

    public async Task<AccessRequest> UpdateAsync(
        AccessRequest accessRequest,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.AccessRequests.Update(accessRequest);
        await context.SaveChangesAsync(cancellationToken);
        return accessRequest;
    }

    public IAsyncEnumerable<AccessRequestResponse> GetAccessRequestsAsync(
        AccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        return GetAccessRequestsInternalAsync(parameters, cancellationToken);
    }

    private async IAsyncEnumerable<AccessRequestResponse> GetAccessRequestsInternalAsync(
        AccessRequestParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.AccessRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(ar =>
                ar.Email.ToLower().Contains(search) || ar.DisplayName.ToLower().Contains(search)
            );
        }

        if (
            !string.IsNullOrWhiteSpace(parameters.Status)
            && Enum.TryParse<AccessRequestStatus>(parameters.Status, true, out var status)
        )
        {
            query = query.Where(ar => ar.Status == status);
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(ar => ar.Id > parameters.Cursor.Value);
        }

        await foreach (
            var ar in query
                .OrderBy(ar => ar.Id)
                .Take(parameters.PageSize + 1)
                .Select(static ar => new AccessRequestResponse(
                    ar.Id,
                    ar.Email,
                    ar.DisplayName,
                    ar.Status.ToString(),
                    ar.ReviewNotes,
                    ar.ReviewedById,
                    ar.ReviewedBy != null ? ar.ReviewedBy.DisplayName : null,
                    ar.ReviewedAt,
                    ar.CreatedAt,
                    ar.RequestedOrganizationId,
                    ar.RequestedOrganizationName
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return ar;
        }
    }

    public IAsyncEnumerable<AccessRequestResponse> GetAccessRequestsForOrganizationAsync(
        Guid organizationId,
        AccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        return GetAccessRequestsForOrganizationInternalAsync(
            organizationId,
            parameters,
            cancellationToken
        );
    }

    private async IAsyncEnumerable<AccessRequestResponse> GetAccessRequestsForOrganizationInternalAsync(
        Guid organizationId,
        AccessRequestParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context
            .AccessRequests.AsNoTracking()
            .Where(ar => ar.RequestedOrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(ar =>
                ar.Email.ToLower().Contains(search) || ar.DisplayName.ToLower().Contains(search)
            );
        }

        if (
            !string.IsNullOrWhiteSpace(parameters.Status)
            && Enum.TryParse<AccessRequestStatus>(parameters.Status, true, out var status)
        )
        {
            query = query.Where(ar => ar.Status == status);
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(ar => ar.Id > parameters.Cursor.Value);
        }

        await foreach (
            var ar in query
                .OrderBy(ar => ar.Id)
                .Take(parameters.PageSize + 1)
                .Select(static ar => new AccessRequestResponse(
                    ar.Id,
                    ar.Email,
                    ar.DisplayName,
                    ar.Status.ToString(),
                    ar.ReviewNotes,
                    ar.ReviewedById,
                    ar.ReviewedBy != null ? ar.ReviewedBy.DisplayName : null,
                    ar.ReviewedAt,
                    ar.CreatedAt,
                    ar.RequestedOrganizationId,
                    ar.RequestedOrganizationName
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return ar;
        }
    }
}
