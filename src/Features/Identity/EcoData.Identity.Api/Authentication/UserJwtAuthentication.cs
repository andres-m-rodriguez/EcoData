using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcoData.Identity.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EcoData.Identity.Api.Authentication;

public static class UserJwtAuthentication
{
    public const string SchemeName = "UserJwt";
    public const string CookieName = "auth_token";

    public static AuthenticationBuilder AddUserJwtAuthentication(
        this AuthenticationBuilder builder,
        IConfiguration configuration
    )
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured");

        builder.AddScheme<UserJwtAuthenticationOptions, UserJwtAuthenticationHandler>(
            SchemeName,
            options =>
            {
                options.Issuer = jwtSettings.Issuer;
                options.Audience = jwtSettings.Audience;
                options.SecretKey = jwtSettings.UserSecretKey;
            }
        );

        return builder;
    }
}

public sealed class UserJwtAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public sealed class UserJwtAuthenticationHandler(
    IOptionsMonitor<UserJwtAuthenticationOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder
) : AuthenticationHandler<UserJwtAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Cookies[UserJwtAuthentication.CookieName];

        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Options.Issuer,
                ValidateAudience = true,
                ValidAudience = Options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Options.SecretKey)
                ),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var tokenType = principal.FindFirst("token_type")?.Value;
            if (tokenType != "user")
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid token type"));
            }

            var ticket = new AuthenticationTicket(principal, UserJwtAuthentication.SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Token has expired"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail($"Token validation failed: {ex.Message}"));
        }
    }
}
