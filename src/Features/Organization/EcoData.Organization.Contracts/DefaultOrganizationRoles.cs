namespace EcoData.Organization.Contracts;

/// <summary>
/// Default role names seeded when creating a new organization.
/// Note: Organizations can create custom roles at runtime; these constants
/// represent only the standard roles created by default.
/// </summary>
public static class DefaultOrganizationRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Contributor = "Contributor";
    public const string Viewer = "Viewer";
}
