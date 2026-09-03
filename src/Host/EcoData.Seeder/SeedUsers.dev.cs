using EcoData.Identity.Database;
using EcoData.Identity.Database.Models;
using EcoData.Organization.Contracts;
using EcoData.Organization.Database;
using EcoData.Organization.Database.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Seeder;

// No user is ever seeded in production, so this class has no .prod.cs half.
// Dev accounts follow app@role.dev and share one password. Each one pins a
// distinct permission state so every screen has an account that exercises it.
internal sealed class SeedUsers(
    IdentityDbContext identity,
    OrganizationDbContext organizations,
    ILogger<SeedUsers> logger
)
{
    public const string DevelopmentPassword = "Dev@12345";

    public async Task SeedDevelopmentAsync(CancellationToken ct)
    {
        await EnsureUserAsync("global@admin.dev", "Global Admin", DevelopmentPassword, GlobalRole.GlobalAdmin, ct);

        var owner = await EnsureUserAsync("ecoportal@owner.dev", "EcoPortal Owner", DevelopmentPassword, null, ct);
        var contributor = await EnsureUserAsync("ecoportal@contributor.dev", "EcoPortal Contributor", DevelopmentPassword, null, ct);
        var viewer = await EnsureUserAsync("ecoportal@viewer.dev", "EcoPortal Viewer", DevelopmentPassword, null, ct);
        var portalGuest = await EnsureUserAsync("ecoportal@guest.dev", "EcoPortal Guest", DevelopmentPassword, null, ct);

        var faunaAdmin = await EnsureUserAsync("faunafinder@admin.dev", "FaunaFinder Admin", DevelopmentPassword, null, ct);
        var student = await EnsureUserAsync("faunafinder@student.dev", "FaunaFinder Student", DevelopmentPassword, null, ct);
        var pendingStudent = await EnsureUserAsync("faunafinder@pending.dev", "FaunaFinder Pending", DevelopmentPassword, null, ct);
        await EnsureUserAsync("faunafinder@guest.dev", "FaunaFinder Guest", DevelopmentPassword, null, ct);

        await EnsureMemberAsync(owner, SeedOrganizations.InterMetroSlug, DefaultOrganizationRoles.Owner, ct);
        await EnsureMemberAsync(owner, SeedOrganizations.CoastalLabSlug, DefaultOrganizationRoles.Owner, ct);
        await EnsureMemberAsync(contributor, SeedOrganizations.InterMetroSlug, DefaultOrganizationRoles.Contributor, ct);
        await EnsureMemberAsync(viewer, SeedOrganizations.InterMetroSlug, DefaultOrganizationRoles.Viewer, ct);
        await EnsureMemberAsync(faunaAdmin, SeedOrganizations.InterMetroSlug, "FaunaAdministrator", ct);
        await EnsureMemberAsync(student, SeedOrganizations.InterMetroSlug, "Student", ct);

        // The approved request is what FaunaFinder's account page reads to show
        // "Student access: approved"; membership alone doesn't surface there.
        await EnsureAccessRequestAsync(student, SeedOrganizations.InterMetroSlug, "Student", OrganizationAccessRequestStatus.Approved, faunaAdmin, ct);
        await EnsureAccessRequestAsync(pendingStudent, SeedOrganizations.InterMetroSlug, "Student", OrganizationAccessRequestStatus.Pending, null, ct);
        await EnsureAccessRequestAsync(portalGuest, SeedOrganizations.RioPiedrasWatchersSlug, DefaultOrganizationRoles.Viewer, OrganizationAccessRequestStatus.Pending, null, ct);
    }

    private async Task<Guid> EnsureUserAsync(
        string email,
        string displayName,
        string password,
        GlobalRole? globalRole,
        CancellationToken ct
    )
    {
        var normalizedEmail = email.ToUpperInvariant();
        var existingId = await identity
            .Users.Where(u => u.NormalizedEmail == normalizedEmail)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);

        if (existingId is { } id)
            return id;

        logger.LogInformation("Creating user {Email}...", email);

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            NormalizedUserName = normalizedEmail,
            Email = email,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = true,
            DisplayName = displayName,
            GlobalRole = globalRole,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        identity.Users.Add(user);
        await identity.SaveChangesAsync(ct);

        return user.Id;
    }

    private async Task EnsureMemberAsync(Guid userId, string slug, string roleName, CancellationToken ct)
    {
        var role = await organizations
            .OrganizationRoles.Where(r => r.Organization!.Slug == slug && r.Name == roleName)
            .Select(r => new { r.Id, r.OrganizationId })
            .FirstAsync(ct);

        var isMember = await organizations.OrganizationMembers.AnyAsync(
            m => m.UserId == userId && m.OrganizationId == role.OrganizationId,
            ct
        );
        if (isMember)
            return;

        organizations.OrganizationMembers.Add(
            new OrganizationMember
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                OrganizationId = role.OrganizationId,
                RoleId = role.Id,
                CreatedAt = DateTimeOffset.UtcNow,
            }
        );
        await organizations.SaveChangesAsync(ct);
        logger.LogInformation("Added {Role} member to organization '{Slug}'", roleName, slug);
    }

    private async Task EnsureAccessRequestAsync(
        Guid userId,
        string slug,
        string roleName,
        OrganizationAccessRequestStatus status,
        Guid? reviewedByUserId,
        CancellationToken ct
    )
    {
        var role = await organizations
            .OrganizationRoles.Where(r => r.Organization!.Slug == slug && r.Name == roleName)
            .Select(r => new { r.Id, r.OrganizationId })
            .FirstAsync(ct);

        var hasRequest = await organizations.OrganizationAccessRequests.AnyAsync(
            r => r.UserId == userId && r.OrganizationId == role.OrganizationId,
            ct
        );
        if (hasRequest)
            return;

        var now = DateTimeOffset.UtcNow;
        organizations.OrganizationAccessRequests.Add(
            new OrganizationAccessRequest
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                OrganizationId = role.OrganizationId,
                RoleId = role.Id,
                Status = status,
                RequestMessage = null,
                ReviewNotes = null,
                ReviewedByUserId = reviewedByUserId,
                ReviewedAt = status == OrganizationAccessRequestStatus.Pending ? null : now,
                CreatedAt = now,
            }
        );
        await organizations.SaveChangesAsync(ct);
        logger.LogInformation("Added {Status} {Role} access request to organization '{Slug}'", status, roleName, slug);
    }
}
