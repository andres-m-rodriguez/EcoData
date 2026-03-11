namespace EcoData.AquaTrack.Contracts;

public static class Permissions
{
    public static class Sensor
    {
        public const string Read = "sensor:read";
        public const string Create = "sensor:create";
        public const string Update = "sensor:update";
        public const string Delete = "sensor:delete";
    }

    public static class Organization
    {
        public const string Update = "org:update";
        public const string Delete = "org:delete";
        public const string ManageMembers = "org:members:manage";
    }
}
