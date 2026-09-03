namespace EcoData.Common.Problems;

/// <summary>
/// Thrown when an HTTP response carries a problem payload.
/// </summary>
public sealed class EcoDataProblemException : Exception
{
    /// <summary>The problem payload parsed from the failed response.</summary>
    public EcoDataProblemDetails Problem { get; }

    /// <summary>Creates the exception from a parsed problem payload.</summary>
    public EcoDataProblemException(EcoDataProblemDetails problem)
        : base(problem?.Title ?? "The request failed with a problem response.")
    {
        ArgumentNullException.ThrowIfNull(problem);
        Problem = problem;
    }
}
