namespace EcoData.Common.Problems.Contracts;

/// <summary>
/// The transport-generic failure: the HTTP status code is the error code.
/// Status 0 means the request never reached the server.
/// </summary>
public sealed record RequestFailed(int StatusCode, string? Message = null);
