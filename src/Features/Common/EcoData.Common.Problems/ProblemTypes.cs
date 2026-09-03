namespace EcoData.Common.Problems;

/// <summary>
/// Stable URIs identifying problem types.
/// </summary>
public static class ProblemTypes
{
    /// <summary>One or more request fields failed validation.</summary>
    public const string Validation = "urn:ecodata:problem:validation";

    /// <summary>The requested resource does not exist.</summary>
    public const string NotFound = "urn:ecodata:problem:not-found";

    /// <summary>The caller is not authenticated.</summary>
    public const string Unauthorized = "urn:ecodata:problem:unauthorized";

    /// <summary>The caller is authenticated but not allowed to perform the action.</summary>
    public const string Forbidden = "urn:ecodata:problem:forbidden";

    /// <summary>The request conflicts with the current state of the resource.</summary>
    public const string Conflict = "urn:ecodata:problem:conflict";

    /// <summary>An unexpected server-side failure.</summary>
    public const string Internal = "urn:ecodata:problem:internal";
}
