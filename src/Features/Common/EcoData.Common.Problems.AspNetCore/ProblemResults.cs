using Microsoft.AspNetCore.Http;

namespace EcoData.Common.Problems.AspNetCore;

/// <summary>Maps typed errors to problem responses.</summary>
public static class ProblemResults
{
    public static IResult Validation(ValidationFailed failed)
    {
        var problem = EcoDataProblemDetails.Validation(failed.Errors);
        return ToResult(problem);
    }

    public static IResult Conflict(string detail)
    {
        var problem = EcoDataProblemDetails.Conflict(detail);
        return ToResult(problem);
    }

    public static IResult NotFound(string detail)
    {
        var problem = EcoDataProblemDetails.NotFound(detail);
        return ToResult(problem);
    }

    public static IResult Unauthorized(string detail)
    {
        var problem = EcoDataProblemDetails.Unauthorized(detail);
        return ToResult(problem);
    }

    public static IResult Forbidden(string detail)
    {
        var problem = EcoDataProblemDetails.Forbidden(detail);
        return ToResult(problem);
    }

    private static IResult ToResult(EcoDataProblemDetails problem) =>
        TypedResults.Json(
            problem,
            EcoDataProblemJsonContext.Default.EcoDataProblemDetails,
            contentType: ProblemDetailsHttpExtensions.ProblemMediaType,
            statusCode: (int?)problem.Status);
}
