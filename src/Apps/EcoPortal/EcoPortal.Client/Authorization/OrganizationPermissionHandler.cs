using System.Text.RegularExpressions;
using EcoData.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace EcoPortal.Client.Authorization;

public sealed partial class OrganizationPermissionHandler(
    NavigationManager navigationManager,
    IAuthorization auth
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

        // The permission source's DTO carries IsGlobalAdmin, so admins pass without a grant.
        var hasPermission = await auth.HasAsync(
            new OrgPermission(requirement.Permission),
            organizationId.Value
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
