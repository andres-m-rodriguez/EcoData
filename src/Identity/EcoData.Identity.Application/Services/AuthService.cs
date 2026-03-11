using System.Runtime.CompilerServices;
using System.Security.Claims;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Contracts.Results;
using EcoData.Identity.Database;
using EcoData.Identity.Database.Models;
using EcoData.Identity.DataAccess.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Identity.Application.Services;

public sealed class AuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IDbContextFactory<IdentityDbContext> contextFactory,
    IAccessRequestRepository accessRequestRepository,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<UpdateAccessRequestStatusRequest> updateStatusValidator
) : IAuthService
{
    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ValidationFailed(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return new EmailAlreadyExists();
        }

        var existingRequest = await accessRequestRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingRequest is not null)
        {
            return new PendingAccessRequest();
        }

        var passwordHasher = new PasswordHasher<AccessRequest>();
        var accessRequest = new AccessRequest
        {
            Id = Guid.CreateVersion7(),
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = passwordHasher.HashPassword(null!, request.Password),
            Status = AccessRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await accessRequestRepository.CreateAsync(accessRequest, cancellationToken);

        return new UserInfo(
            accessRequest.Id,
            accessRequest.Email,
            accessRequest.DisplayName,
            "Pending",
            null,
            accessRequest.CreatedAt
        );
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ValidationFailed(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            var pendingRequest = await accessRequestRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (pendingRequest is not null && pendingRequest.Status == AccessRequestStatus.Pending)
            {
                return new AccountNotApproved();
            }
            return new InvalidCredentials();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return new AccountLocked();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true
        );

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return new AccountLocked();
            }
            return new InvalidCredentials();
        }

        return new UserInfo(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.Role.ToString(),
            user.GlobalRole.HasValue ? (Contracts.Authorization.GlobalRole)user.GlobalRole.Value : null,
            user.CreatedAt
        );
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await signInManager.SignOutAsync();
    }

    public async Task<UserInfo?> GetCurrentUserAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        return new UserInfo(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.Role.ToString(),
            user.GlobalRole.HasValue ? (Contracts.Authorization.GlobalRole)user.GlobalRole.Value : null,
            user.CreatedAt
        );
    }

    public IAsyncEnumerable<UserInfo> GetUsersAsync(UserParameters parameters, CancellationToken cancellationToken = default)
    {
        return GetUsersInternalAsync(parameters, cancellationToken);
    }

    private async IAsyncEnumerable<UserInfo> GetUsersInternalAsync(
        UserParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(search)
                || u.DisplayName.ToLower().Contains(search)
            );
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(u => u.Id > parameters.Cursor.Value);
        }

        await foreach (var user in query
            .OrderBy(u => u.Id)
            .Take(parameters.PageSize + 1)
            .Select(u => new UserInfo(
                u.Id,
                u.Email!,
                u.DisplayName,
                u.Role.ToString(),
                u.GlobalRole.HasValue ? (Contracts.Authorization.GlobalRole)u.GlobalRole.Value : null,
                u.CreatedAt
            ))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            yield return user;
        }
    }

    public IAsyncEnumerable<AccessRequestResponse> GetAccessRequestsAsync(
        AccessRequestParameters parameters,
        CancellationToken cancellationToken = default)
    {
        return accessRequestRepository.GetAccessRequestsAsync(parameters, cancellationToken);
    }

    public async Task<UpdateAccessRequestResult> UpdateAccessRequestStatusAsync(
        Guid id,
        UpdateAccessRequestStatusRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await updateStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ValidationFailed(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var accessRequest = await accessRequestRepository.GetByIdAsync(id, cancellationToken);
        if (accessRequest is null)
        {
            return new AccessRequestNotFound();
        }

        if (accessRequest.Status != AccessRequestStatus.Pending)
        {
            return new AccessRequestAlreadyProcessed();
        }

        var reviewer = await userManager.GetUserAsync(principal);

        accessRequest.Status = request.Approved ? AccessRequestStatus.Approved : AccessRequestStatus.Rejected;
        accessRequest.ReviewNotes = request.ReviewNotes;
        accessRequest.ReviewedById = reviewer?.Id;
        accessRequest.ReviewedAt = DateTimeOffset.UtcNow;

        await accessRequestRepository.UpdateAsync(accessRequest, cancellationToken);

        if (request.Approved)
        {
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = accessRequest.Email,
                Email = accessRequest.Email,
                EmailConfirmed = true,
                DisplayName = accessRequest.DisplayName,
                Role = UserRole.Viewer,
                GlobalRole = null,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var passwordHasher = new PasswordHasher<AccessRequest>();
            var verifyResult = passwordHasher.VerifyHashedPassword(null!, accessRequest.PasswordHash, "");

            // Create user with the stored password hash
            var createResult = await userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                // Set the password hash directly since we already have it hashed
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
                var dbUser = await context.Users.FirstAsync(u => u.Id == user.Id, cancellationToken);
                dbUser.PasswordHash = accessRequest.PasswordHash;
                context.Users.Update(dbUser);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        return new AccessRequestResponse(
            accessRequest.Id,
            accessRequest.Email,
            accessRequest.DisplayName,
            accessRequest.Status.ToString(),
            accessRequest.ReviewNotes,
            accessRequest.ReviewedById,
            reviewer?.DisplayName,
            accessRequest.ReviewedAt,
            accessRequest.CreatedAt
        );
    }
}
