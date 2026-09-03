using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EcoData.Common.Problems.Tests;

public class ProblemDetailsHttpExtensionsTests
{
    private static HttpResponseMessage Response(HttpStatusCode status, string? body = null, string mediaType = ProblemDetailsHttpExtensions.ProblemMediaType)
    {
        var response = new HttpResponseMessage(status);
        if (body is not null)
            response.Content = new StringContent(body, Encoding.UTF8, mediaType);

        return response;
    }

    private static string Serialize(EcoDataProblemDetails problem) =>
        JsonSerializer.Serialize(problem, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);

    [Fact]
    public async Task ReadProblemAsync_SuccessResponse_ReturnsNull()
    {
        using var response = Response(HttpStatusCode.OK, "{}", "application/json");

        var problem = await response.ReadProblemAsync();

        Assert.Null(problem);
    }

    [Fact]
    public async Task ReadProblemAsync_ProblemJsonBody_IsParsed()
    {
        var expected = EcoDataProblemDetails.NotFound(detail: "Species 7 does not exist.");
        var body = Serialize(expected);
        using var response = Response(HttpStatusCode.NotFound, body);

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(ProblemTypes.NotFound, problem.Type);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Species 7 does not exist.", problem.Detail);
    }

    [Fact]
    public async Task ReadProblemAsync_PlainJsonContentType_IsParsed()
    {
        var expected = EcoDataProblemDetails.Conflict();
        var body = Serialize(expected);
        using var response = Response(HttpStatusCode.Conflict, body, "application/json");

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(ProblemTypes.Conflict, problem.Type);
        Assert.Equal(409, problem.Status);
    }

    [Fact]
    public async Task ReadProblemAsync_MediaTypeCasingDiffers_IsParsed()
    {
        var expected = EcoDataProblemDetails.NotFound();
        var body = Serialize(expected);
        using var response = Response(HttpStatusCode.NotFound, body, "Application/Problem+JSON");

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(ProblemTypes.NotFound, problem.Type);
    }

    [Fact]
    public async Task ReadProblemAsync_MissingStatusInBody_FilledFromResponse()
    {
        using var response = Response(HttpStatusCode.Conflict, """{"type":"urn:ecodata:problem:conflict"}""");

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
    }

    [Fact]
    public async Task ReadProblemAsync_UnparseableJsonBody_FallsBackToStatusCode()
    {
        using var response = Response(HttpStatusCode.BadGateway, "<html>gateway error</html>", "application/json");

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(502, problem.Status);
        Assert.Equal("about:blank", problem.Type);
    }

    [Fact]
    public async Task ReadProblemAsync_NonJsonContentType_FallsBackToStatusCode()
    {
        using var response = Response(HttpStatusCode.InternalServerError, "plain text error", "text/plain");

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
    }

    [Fact]
    public async Task ReadProblemAsync_EmptyErrorResponse_FallsBackToStatusCode()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        var problem = await response.ReadProblemAsync();

        Assert.NotNull(problem);
        Assert.Equal(503, problem.Status);
    }

    [Fact]
    public async Task EnsureSuccessOrProblemAsync_FailedResponse_ThrowsWithProblem()
    {
        var expected = EcoDataProblemDetails.Forbidden();
        var body = Serialize(expected);
        using var response = Response(HttpStatusCode.Forbidden, body);

        var exception = await Assert.ThrowsAsync<EcoDataProblemException>(
            () => response.EnsureSuccessOrProblemAsync());

        Assert.Equal(ProblemTypes.Forbidden, exception.Problem.Type);
        Assert.Equal(403, exception.Problem.Status);
    }

    [Fact]
    public async Task EnsureSuccessOrProblemAsync_SuccessResponse_DoesNotThrow()
    {
        using var response = Response(HttpStatusCode.NoContent);

        await response.EnsureSuccessOrProblemAsync();
    }
}
