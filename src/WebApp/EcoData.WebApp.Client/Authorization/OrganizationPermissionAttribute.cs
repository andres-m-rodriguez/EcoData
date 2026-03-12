using Microsoft.AspNetCore.Authorization;

namespace EcoData.WebApp.Client.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class OrganizationPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "OrganizationPermission:";

    public OrganizationPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = $"{PolicyPrefix}{permission}";
    }

    public string Permission { get; }
}
