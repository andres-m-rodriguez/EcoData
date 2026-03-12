using System.Text.RegularExpressions;
using EcoPortal.Client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace EcoPortal.Client.Authorization;

public sealed partial class OrganizationPermissionHandler(
    NavigationManager navigationManager,
    PermissionContextService permissionContext
) : AuthorizationHandler<OrganizationPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationPermissionRequirement requirement
    )
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        var organizationId = ExtractOrganizationId(navigationManager.Uri);

        if (organizationId is null)
        {
            context.Fail();
            return;
        }

        // HasPermissionAsync already checks for global admin
        var hasPermission = await permissionContext.HasPermissionAsync(
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

    private static Guid? ExtractOrganizationId(string uri)
    {
        var match = OrganizationIdPattern().Match(uri);

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
