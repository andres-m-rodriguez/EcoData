namespace EcoData.Common.Authorization;

public interface IPermission
{
    string Key { get; }
}

public interface IOrganizationPermission : IPermission;

public interface IGlobalPermission : IPermission;

public sealed record OrgPermission(string Key) : IOrganizationPermission;

public sealed record GlobalPermission(string Key) : IGlobalPermission;

// A global action granted by role membership rather than a stored grant.
public sealed record GlobalRolePermission(string Key, string Role) : IGlobalPermission;
