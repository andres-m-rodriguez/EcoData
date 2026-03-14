namespace EcoData.Identity.Contracts.Responses;

public record SensorProvisionResponse(Guid SensorId, string AccessToken, DateTimeOffset ExpiresAt);

public record SensorTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
