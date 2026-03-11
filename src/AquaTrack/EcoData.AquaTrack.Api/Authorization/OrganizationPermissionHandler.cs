using System.Security.Claims;
using System.Text.RegularExpressions;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace EcoData.AquaTrack.Api.Authorization;

public sealed partial class OrganizationPermissionHandler(
    IHttpContextAccessor httpContextAccessor,
    IPermissionService permissionService
) : AuthorizationHandler<OrganizationPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationPermissionRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            // During pre-rendering without HttpContext, succeed and let client handle auth
            context.Succeed(requirement);
            return;
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        var organizationId = ExtractOrganizationId(httpContext.Request.Path);

        if (organizationId is null)
        {
            context.Fail();
            return;
        }

        var hasPermission = await permissionService.HasPermissionAsync(
            userId,
            organizationId.Value,
            requirement.Permission
        );

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    private static Guid? ExtractOrganizationId(PathString path)
    {
        var match = OrganizationIdPattern().Match(path.Value ?? string.Empty);

        if (match.Success && Guid.TryParse(match.Groups[1].Value, out var id))
        {
            return id;
        }

        return null;
    }

    [GeneratedRegex(
        @"/organizations/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex OrganizationIdPattern();
}
