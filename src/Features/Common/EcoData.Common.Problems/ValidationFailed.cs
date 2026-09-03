namespace EcoData.Common.Problems;

/// <summary>Per-field validation errors, keyed by field name.</summary>
public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors)
{
    /// <summary>Every message across every field, in field order.</summary>
    public string[] AllMessages => Errors.Values.SelectMany(messages => messages).ToArray();

    /// <summary>The per-field errors carried by a <see cref="ProblemTypes.Validation"/> problem.</summary>
    public static ValidationFailed From(EcoDataProblemDetails problem) =>
        new(problem.Errors ?? new Dictionary<string, string[]>());
}
