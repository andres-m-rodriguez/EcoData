namespace EcoData.Sensors.Contracts.Errors;

// Server-side only: returned by ISensorRepository.RegisterAsync and mapped
// to a 409 problem response in the endpoint. Clients see the 409 via RequestFailed.
public sealed record ConflictError(string Message);
