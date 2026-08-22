namespace EcoData.Wildlife.Contracts;

public static class Permissions
{
    public static class Species
    {
        public const string Read = "wildlife:species:read";
    }

    public static class Occurrence
    {
        public const string Submit = "wildlife:occurrence:submit";
        public const string Verify = "wildlife:occurrence:verify";
    }
}
