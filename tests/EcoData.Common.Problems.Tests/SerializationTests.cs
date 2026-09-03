using System.Net;
using System.Text.Json;
using Xunit;

namespace EcoData.Common.Problems.Tests;

public class SerializationTests
{
    [Fact]
    public void Serialize_ThenDeserialize_PreservesCoreFields()
    {
        var problem = new EcoDataProblemDetails
        {
            Type = ProblemTypes.Conflict,
            Title = "Conflict",
            Status = HttpStatusCode.Conflict,
            Detail = "The sighting was modified by another session.",
            Instance = "/sightings/42",
            TraceId = "trace-123",
        };

        var json = JsonSerializer.Serialize(problem, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);
        var roundTripped = JsonSerializer.Deserialize(json, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);

        Assert.NotNull(roundTripped);
        Assert.Equal(problem.Type, roundTripped.Type);
        Assert.Equal(problem.Title, roundTripped.Title);
        Assert.Equal(problem.Status, roundTripped.Status);
        Assert.Equal(problem.Detail, roundTripped.Detail);
        Assert.Equal(problem.Instance, roundTripped.Instance);
        Assert.Equal(problem.TraceId, roundTripped.TraceId);
    }

    [Fact]
    public void Serialize_OmitsNullFields()
    {
        var problem = new EcoDataProblemDetails { Status = HttpStatusCode.NotFound };

        var json = JsonSerializer.Serialize(problem, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);

        Assert.DoesNotContain("\"detail\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"title\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"errors\"", json, StringComparison.Ordinal);
        Assert.Contains("\"status\":404", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_UsesRfc9457MemberNames()
    {
        var problem = EcoDataProblemDetails.Validation(
            new Dictionary<string, string[]> { ["email"] = ["Email is required."] },
            detail: "Bad input",
            instance: "/species");

        var json = JsonSerializer.Serialize(problem, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);

        Assert.Contains("\"type\":", json, StringComparison.Ordinal);
        Assert.Contains("\"title\":", json, StringComparison.Ordinal);
        Assert.Contains("\"status\":", json, StringComparison.Ordinal);
        Assert.Contains("\"detail\":", json, StringComparison.Ordinal);
        Assert.Contains("\"instance\":", json, StringComparison.Ordinal);
        Assert.Contains("\"errors\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtensionData_RoundTrips_UnknownMembers()
    {
        const string json = """{"type":"urn:ecodata:problem:conflict","status":409,"retryAfterSeconds":30}""";

        var problem = JsonSerializer.Deserialize(json, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);

        Assert.NotNull(problem);
        Assert.NotNull(problem.Extensions);
        var retryAfterSeconds = problem.Extensions["retryAfterSeconds"].GetInt32();
        Assert.Equal(30, retryAfterSeconds);

        var reserialized = JsonSerializer.Serialize(problem, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);
        Assert.Contains("\"retryAfterSeconds\":30", reserialized, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_ValidationErrors_RoundTrip()
    {
        var problem = EcoDataProblemDetails.Validation(new Dictionary<string, string[]>
        {
            ["email"] = ["Email is required.", "Email is invalid."],
            ["name"] = ["Name is required."],
        });

        var json = JsonSerializer.Serialize(problem, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);
        var roundTripped = JsonSerializer.Deserialize(json, EcoDataProblemJsonContext.Default.EcoDataProblemDetails);

        Assert.NotNull(roundTripped?.Errors);
        Assert.Equal(2, roundTripped.Errors.Count);
        Assert.Equal(["Email is required.", "Email is invalid."], roundTripped.Errors["email"]);
        Assert.Equal(["Name is required."], roundTripped.Errors["name"]);
    }
}
