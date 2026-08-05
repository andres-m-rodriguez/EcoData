namespace EcoData.Common.Problems.Contracts;

/// <summary>
/// An RFC 9457 problem details response plus the ASP.NET Core "errors" extension
/// produced by TypedResults.ValidationProblem.
/// </summary>
public sealed record ProblemDetailsDocument(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    IReadOnlyDictionary<string, string[]>? Errors)
{
    /// <summary>Every validation message flattened, for UIs that render a flat list instead of per-field errors.</summary>
    public string[] AllMessages => Errors?.Values.SelectMany(messages => messages).ToArray() ?? [];
}
