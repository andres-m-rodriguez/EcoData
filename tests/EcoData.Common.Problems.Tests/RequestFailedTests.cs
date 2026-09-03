using Xunit;

namespace EcoData.Common.Problems.Tests;

public class RequestFailedTests
{
    [Fact]
    public void StatusCodeZero_IsTransportFailure()
    {
        var failed = new RequestFailed(0, "Connection refused");

        Assert.True(failed.IsTransportFailure);
    }

    [Fact]
    public void HttpStatusCode_IsNotTransportFailure()
    {
        var failed = new RequestFailed(503);

        Assert.False(failed.IsTransportFailure);
        Assert.Null(failed.Message);
    }
}
