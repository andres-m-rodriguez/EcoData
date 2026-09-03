using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EcoData.Common.Problems.AspNetCore;

/// <summary>
/// Maps typed errors to problem responses. The results are typed so they slot into a
/// <c>Results&lt;...&gt;</c> union next to the success case.
/// </summary>
public static class ProblemResults
{
    public static JsonHttpResult<EcoDataProblemDetails> Validation(ValidationFailed failed)
    {
        var problem = EcoDataProblemDetails.Validation(failed.Errors);
        return ToResult(problem);
    }

    public static JsonHttpResult<EcoDataProblemDetails> Conflict(string detail)
    {
        var problem = EcoDataProblemDetails.Conflict(detail);
        return ToResult(problem);
    }

    public static JsonHttpResult<EcoDataProblemDetails> NotFound(string detail)
    {
        var problem = EcoDataProblemDetails.NotFound(detail);
        return ToResult(problem);
    }

    public static JsonHttpResult<EcoDataProblemDetails> Unauthorized(string detail)
    {
        var problem = EcoDataProblemDetails.Unauthorized(detail);
        return ToResult(problem);
    }

    public static JsonHttpResult<EcoDataProblemDetails> Forbidden(string detail)
    {
        var problem = EcoDataProblemDetails.Forbidden(detail);
        return ToResult(problem);
    }

    private static JsonHttpResult<EcoDataProblemDetails> ToResult(EcoDataProblemDetails problem) =>
        TypedResults.Json(
            problem,
            EcoDataProblemJsonContext.Default.EcoDataProblemDetails,
            contentType: ProblemDetailsHttpExtensions.ProblemMediaType,
            statusCode: (int?)problem.Status);
}
