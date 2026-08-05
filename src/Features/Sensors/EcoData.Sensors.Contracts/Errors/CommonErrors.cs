namespace EcoData.Sensors.Contracts.Errors;

/// <summary>
/// Server-side only: returned by <c>ISensorRepository.RegisterAsync</c> and mapped
/// to a 409 problem response in the endpoint. Clients see the 409 via RequestFailed.
/// </summary>
public sealed record ConflictError(string Message);
