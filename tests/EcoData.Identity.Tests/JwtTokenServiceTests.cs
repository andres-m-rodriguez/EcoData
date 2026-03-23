using System.IdentityModel.Tokens.Jwt;
using EcoData.Identity.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EcoData.Identity.Tests;

public sealed class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;
    private readonly JwtSettings _settings;

    public JwtTokenServiceTests()
    {
        _settings = new JwtSettings
        {
            SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationHours = 24
        };

        _sut = new JwtTokenService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateSensorToken_ReturnsValidToken()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var organizationName = "Test Org";
        var sensorName = "Test Sensor";

        // Act
        var (token, expiresAt) = _sut.GenerateSensorToken(
            sensorId,
            organizationId,
            organizationName,
            sensorName
        );

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void GenerateSensorToken_ContainsCorrectClaims()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var organizationName = "Test Org";
        var sensorName = "Test Sensor";

        // Act
        var (token, _) = _sut.GenerateSensorToken(
            sensorId,
            organizationId,
            organizationName,
            sensorName
        );

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "sensor_id" && c.Value == sensorId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "organization_id" && c.Value == organizationId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "organization_name" && c.Value == organizationName);
        jwtToken.Claims.Should().Contain(c => c.Type == "sensor_name" && c.Value == sensorName);
        jwtToken.Claims.Should().Contain(c => c.Type == "token_type" && c.Value == "sensor");
    }

    [Fact]
    public void GenerateSensorToken_ExpiresAtCorrectTime()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var beforeGeneration = DateTimeOffset.UtcNow;

        // Act
        var (_, expiresAt) = _sut.GenerateSensorToken(sensorId, organizationId, "Org", "Sensor");

        // Assert
        var expectedExpiration = beforeGeneration.AddHours(_settings.ExpirationHours);
        expiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateSensorToken_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        // Act
        var (token, _) = _sut.GenerateSensorToken(sensorId, organizationId, "Org", "Sensor");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(_settings.Issuer);
        jwtToken.Audiences.Should().Contain(_settings.Audience);
    }
}
