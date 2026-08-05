namespace EcoData.Sensors.Contracts.Errors;

/// <summary>
/// Per-field validation errors returned by a ValidationProblem response.
/// Deliberately duplicated per feature to keep slices independent.
/// </summary>
public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors)
{
    public string[] AllMessages => Errors.Values.SelectMany(messages => messages).ToArray();
}
