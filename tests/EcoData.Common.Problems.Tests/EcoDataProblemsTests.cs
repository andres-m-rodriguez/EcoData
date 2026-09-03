using Xunit;

namespace EcoData.Common.Problems.Tests;

public class EcoDataProblemsTests
{
    [Fact]
    public void Validation_SetsTypeStatusAndErrors()
    {
        var errors = new Dictionary<string, string[]> { ["field"] = ["required"] };

        var problem = EcoDataProblemDetails.Validation(errors, detail: "d", instance: "/i");

        Assert.Equal(ProblemTypes.Validation, problem.Type);
        Assert.Equal(400, problem.Status);
        Assert.Equal("d", problem.Detail);
        Assert.Equal("/i", problem.Instance);
        Assert.Same(errors, problem.Errors);
        Assert.NotNull(problem.Title);
    }

    [Fact]
    public void Validation_NullErrors_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => EcoDataProblemDetails.Validation(null!));
    }

    public static TheoryData<EcoDataProblemDetails, string, int> Factories => new()
    {
        { EcoDataProblemDetails.NotFound(), ProblemTypes.NotFound, 404 },
        { EcoDataProblemDetails.Unauthorized(), ProblemTypes.Unauthorized, 401 },
        { EcoDataProblemDetails.Forbidden(), ProblemTypes.Forbidden, 403 },
        { EcoDataProblemDetails.Conflict(), ProblemTypes.Conflict, 409 },
        { EcoDataProblemDetails.Internal(), ProblemTypes.Internal, 500 },
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void Factory_SetsExpectedTypeStatusAndTitle(EcoDataProblemDetails problem, string expectedType, int expectedStatus)
    {
        Assert.Equal(expectedType, problem.Type);
        Assert.Equal(expectedStatus, problem.Status);
        var titleIsBlank = string.IsNullOrWhiteSpace(problem.Title);
        Assert.False(titleIsBlank);
    }

    [Fact]
    public void Internal_CarriesTraceId_AndNoDetail()
    {
        var problem = EcoDataProblemDetails.Internal(traceId: "trace-9");

        Assert.Equal("trace-9", problem.TraceId);
        Assert.Null(problem.Detail);
    }
}
