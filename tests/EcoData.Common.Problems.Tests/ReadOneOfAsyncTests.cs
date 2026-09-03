using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace EcoData.Common.Problems.Tests;

public sealed record SampleDto(string Name, int Level);

[JsonSerializable(typeof(SampleDto))]
internal sealed partial class TestJsonContext : JsonSerializerContext;

public class ReadOneOfAsyncTests
{
    private static HttpResponseMessage Response(HttpStatusCode status, string body, string mediaType = "application/json")
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        };
    }

    [Fact]
    public async Task SuccessResponse_ReturnsPayload()
    {
        using var response = Response(HttpStatusCode.OK, """{"Name":"Ada","Level":3}""");

        var result = await response.ReadOneOfAsync(TestJsonContext.Default.SampleDto);

        Assert.True(result.IsT0);
        Assert.Equal(new SampleDto("Ada", 3), result.AsT0);
    }

    [Fact]
    public async Task FailedResponse_ReturnsProblem()
    {
        var expected = EcoDataProblemDetails.NotFound(detail: "Species 7 does not exist.");
        var body = JsonSerializer.Serialize(expected, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);
        using var response = Response(HttpStatusCode.NotFound, body, ProblemDetailsHttpExtensions.ProblemMediaType);

        var result = await response.ReadOneOfAsync(TestJsonContext.Default.SampleDto);

        Assert.True(result.IsT1);
        Assert.Equal(ProblemTypes.NotFound, result.AsT1.Type);
        Assert.Equal(HttpStatusCode.NotFound, result.AsT1.Status);
    }

    [Fact]
    public async Task FailedResponse_WithoutBody_ReturnsFallbackProblem()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        var result = await response.ReadOneOfAsync(TestJsonContext.Default.SampleDto);

        Assert.True(result.IsT1);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.AsT1.Status);
    }

    [Fact]
    public async Task SuccessResponse_UnparseableBody_ThrowsJsonException()
    {
        using var response = Response(HttpStatusCode.OK, "not json");

        await Assert.ThrowsAsync<JsonException>(
            () => response.ReadOneOfAsync(TestJsonContext.Default.SampleDto));
    }

    [Fact]
    public async Task SuccessResponse_EmptyBody_ThrowsJsonException()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        await Assert.ThrowsAsync<JsonException>(
            () => response.ReadOneOfAsync(TestJsonContext.Default.SampleDto));
    }

    [Fact]
    public async Task SuccessResponse_NullBody_ThrowsJsonException()
    {
        using var response = Response(HttpStatusCode.OK, "null");

        await Assert.ThrowsAsync<JsonException>(
            () => response.ReadOneOfAsync(TestJsonContext.Default.SampleDto));
    }
}
