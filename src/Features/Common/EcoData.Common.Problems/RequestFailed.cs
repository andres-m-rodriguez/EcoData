using System.Net;

namespace EcoData.Common.Problems;

/// <summary>
/// A request that produced no usable problem payload. A <see cref="StatusCode"/> of zero means
/// the request never reached the server: the host was unreachable, the connection dropped, the
/// client timed out, or the call was made offline.
/// </summary>
public sealed record RequestFailed(HttpStatusCode StatusCode, string? Message = null)
{
    /// <summary>True when the request never reached the server.</summary>
    public bool IsTransportFailure => StatusCode == 0;

    /// <summary>Collapses a problem the caller does not distinguish further into its status and message.</summary>
    public static RequestFailed From(EcoDataProblemDetails problem) =>
        new(problem.Status ?? default, problem.Detail ?? problem.Title);
}
