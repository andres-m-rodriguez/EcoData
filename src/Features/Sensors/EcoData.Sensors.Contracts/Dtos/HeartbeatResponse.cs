namespace EcoData.Sensors.Contracts.Dtos;

public sealed record HeartbeatResponse(string Message, Guid SensorId, DateTimeOffset Timestamp);
