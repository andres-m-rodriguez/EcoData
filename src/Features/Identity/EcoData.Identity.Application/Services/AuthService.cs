using System.Runtime.CompilerServices;
using System.Security.Claims;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Contracts.Results;
using EcoData.Identity.Database;
using EcoData.Identity.Database.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Identity.Application.Services;

public sealed class AuthService(
    UserManager<User> userManager,
    IDbContextFactory<IdentityDbContext> contextFactory,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IJwtTokenService jwtTokenService
) : IAuthService
{
    public async Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var validationResult = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ValidationFailed(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            );
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return new EmailAlreadyExists();
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName,
            GlobalRole = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return new ValidationFailed(createResult.Errors.Select(e => e.Description).ToList());
        }

        return new UserInfo(
            user.Id,
            user.Email,
            user.DisplayName,
            user.GlobalRole.HasValue
                ? (Contracts.Authorization.GlobalRole)user.GlobalRole.Value
                : null,
            user.CreatedAt
        );
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var validationResult = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ValidationFailed(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            );
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new InvalidCredentials();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return new AccountLocked();
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
            {
                return new AccountLocked();
            }

            return new InvalidCredentials();
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var globalRole = user.GlobalRole.HasValue
            ? (Contracts.Authorization.GlobalRole)user.GlobalRole.Value
            : (Contracts.Authorization.GlobalRole?)null;

        var (token, expiresAt) = jwtTokenService.GenerateUserToken(
            user.Id,
            user.Email!,
            user.DisplayName,
            globalRole?.ToString()
        );

        var userInfo = new UserInfo(
            user.Id,
            user.Email!,
            user.DisplayName,
            globalRole,
            user.CreatedAt
        );

        return new LoginResponse(token, expiresAt, userInfo);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        // JWT-based auth: cookie clearing is handled by the endpoint
        return Task.CompletedTask;
    }

    public async Task<UserInfo?> GetCurrentUserAsync(
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken = default
    )
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
            user.GlobalRole.HasValue
                ? (Contracts.Authorization.GlobalRole)user.GlobalRole.Value
                : null,
            user.CreatedAt
        );
    }

    public IAsyncEnumerable<UserInfo> GetUsersAsync(
        UserParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        return GetUsersInternalAsync(parameters, cancellationToken);
    }

    private async IAsyncEnumerable<UserInfo> GetUsersInternalAsync(
        UserParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(search) || u.DisplayName.ToLower().Contains(search)
            );
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(u => u.Id > parameters.Cursor.Value);
        }

        await foreach (
            var user in query
                .OrderBy(u => u.Id)
                .Take(parameters.PageSize + 1)
                .Select(u => new UserInfo(
                    u.Id,
                    u.Email!,
                    u.DisplayName,
                    u.GlobalRole.HasValue
                        ? (Contracts.Authorization.GlobalRole)u.GlobalRole.Value
                        : null,
                    u.CreatedAt
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return user;
        }
    }
}
