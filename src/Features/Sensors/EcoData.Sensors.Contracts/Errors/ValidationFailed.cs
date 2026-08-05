namespace EcoData.Sensors.Contracts.Errors;

/// <summary>The server rejected the request with per-field validation errors.</summary>
public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors)
{
    public string[] AllMessages => Errors.Values.SelectMany(messages => messages).ToArray();
}
