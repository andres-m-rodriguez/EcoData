namespace EcoData.Wildlife.Contracts;

public static class Permissions
{
    public static class Species
    {
        public const string Read = "wildlife:species:read";
        public const string ViewAreas = "wildlife:species:view-areas";
    }

    public static class Occurrence
    {
        public const string Submit = "wildlife:occurrence:submit";
        public const string Verify = "wildlife:occurrence:verify";
    }
}
