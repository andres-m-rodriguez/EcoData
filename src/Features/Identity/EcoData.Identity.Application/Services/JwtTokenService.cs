using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EcoData.Identity.Application.Services;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateSensorToken(
        Guid sensorId,
        Guid organizationId,
        string organizationName,
        string sensorName
    );

    (string Token, DateTimeOffset ExpiresAt) GenerateUserToken(
        Guid userId,
        string email,
        string displayName,
        string? role
    );
}

public sealed class JwtTokenService(IOptions<JwtSettings> settings) : IJwtTokenService
{
    private readonly JwtSettings _settings = settings.Value;

    public (string Token, DateTimeOffset ExpiresAt) GenerateSensorToken(
        Guid sensorId,
        Guid organizationId,
        string organizationName,
        string sensorName
    )
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SensorSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTimeOffset.UtcNow.AddHours(_settings.ExpirationHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, sensorId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("sensor_id", sensorId.ToString()),
            new Claim("organization_id", organizationId.ToString()),
            new Claim("organization_name", organizationName),
            new Claim("sensor_name", sensorName),
            new Claim("token_type", "sensor"),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string Token, DateTimeOffset ExpiresAt) GenerateUserToken(
        Guid userId,
        string email,
        string displayName,
        string? role
    )
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.UserSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("DisplayName", displayName),
            new("token_type", "user"),
        };

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
