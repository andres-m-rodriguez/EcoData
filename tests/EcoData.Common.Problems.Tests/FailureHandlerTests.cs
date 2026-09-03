using System.Net;
using Xunit;

namespace EcoData.Common.Problems.Tests;

// Every case goes through a real HttpClient, so the token the handlers see is the
// linked one HttpClient builds, exactly as in the apps.
public class FailureHandlerTests
{
    private const string Url = "http://localhost/species";

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class OkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
    }

    [Fact]
    public async Task Transport_HttpRequestException_BecomesUnreachableProblem()
    {
        using var client = new HttpClient(new TransportFailureHandler { InnerHandler = new ThrowingHandler(new HttpRequestException("Connection refused")) });
        using var response = await client.GetAsync(Url);

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(ProblemTypes.Unreachable, problem.Type);
        Assert.Equal((HttpStatusCode)0, problem.Status);
        Assert.Equal("Connection refused", problem.Detail);
        Assert.True(RequestFailed.From(problem).IsTransportFailure);
    }

    [Fact]
    public async Task Transport_SuccessfulResponse_PassesThrough()
    {
        using var client = new HttpClient(new TransportFailureHandler { InnerHandler = new OkHandler() });
        using var response = await client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Cancelled_HandlerTimeout_BecomesTimeoutProblem()
    {
        var handler = new RequestCancelledFailureHandler
        {
            Timeout = TimeSpan.FromMilliseconds(50),
            InnerHandler = new HangingHandler(),
        };
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync(Url);

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(ProblemTypes.Timeout, problem.Type);
        Assert.True(RequestFailed.From(problem).IsTransportFailure);
    }

    [Fact]
    public async Task Cancelled_ByCaller_StillThrows()
    {
        var handler = new RequestCancelledFailureHandler
        {
            Timeout = TimeSpan.FromSeconds(10),
            InnerHandler = new HangingHandler(),
        };
        using var client = new HttpClient(handler);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync(Url, cts.Token));
    }
}
