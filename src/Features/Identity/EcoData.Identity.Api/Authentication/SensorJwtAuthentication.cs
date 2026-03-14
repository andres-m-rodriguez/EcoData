using System.Text;
using EcoData.Identity.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EcoData.Identity.Api.Authentication;

public static class SensorJwtAuthentication
{
    public const string SchemeName = "SensorJwt";

    public static AuthenticationBuilder AddSensorJwtAuthentication(
        this AuthenticationBuilder builder,
        IConfiguration configuration
    )
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured");

        builder.AddJwtBearer(SchemeName, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
                ),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                // Require the token_type claim to be "sensor"
                RoleClaimType = "token_type"
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var tokenType = context.Principal?.FindFirst("token_type")?.Value;
                    if (tokenType != "sensor")
                    {
                        context.Fail("Invalid token type");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return builder;
    }
}
