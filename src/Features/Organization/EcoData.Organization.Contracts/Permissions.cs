namespace EcoData.Organization.Contracts;

public static class Permissions
{
    public static class Organization
    {
        public const string Update = "org:update";
        public const string Delete = "org:delete";
        public const string ManageMembers = "org:members:manage";
    }
}
