namespace EcoData.Identity.Application.Services;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string SecretKey { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int ExpirationHours { get; set; } = 24;
}
