namespace EcoData.Common.Problems;

/// <summary>
/// Turns a request that never reached the server into a status-zero problem response, so a
/// client reads an unreachable host the same way it reads any other failure.
/// </summary>
public sealed class TransportFailureHandler : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            var problem = EcoDataProblemDetails.Unreachable(e.Message);
            return problem.ToResponse();
        }
    }
}
