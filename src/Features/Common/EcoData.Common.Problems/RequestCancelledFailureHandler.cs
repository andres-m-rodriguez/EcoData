namespace EcoData.Common.Problems;

/// <summary>
/// Turns a request the client stopped waiting for into a status-zero problem response. A
/// cancellation the caller asked for is left alone and still propagates.
/// </summary>
public sealed class RequestCancelledFailureHandler : DelegatingHandler
{
    // This handler owns the timeout. HttpClient links its own timeout into the token it
    // passes down, so by the time that fires the token is cancelled and a caller's
    // cancellation looks identical. Firing first, with the caller's token still live,
    // keeps the two apart. Stay under HttpClient's default of 100 seconds.
    /// <summary>How long a request may take before it is reported as timed out.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            return await base.SendAsync(request, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var problem = EcoDataProblemDetails.Timeout($"No answer within {Timeout.TotalSeconds:0} seconds.");
            return problem.ToResponse();
        }
    }
}
