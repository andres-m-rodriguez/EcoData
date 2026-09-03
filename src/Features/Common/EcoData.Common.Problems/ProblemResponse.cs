using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EcoData.Common.Problems;

internal static class ProblemResponse
{
    extension(EcoDataProblemDetails problem)
    {
        /// <summary>A response carrying the problem as its body, for handlers that never got one.</summary>
        public HttpResponseMessage ToResponse() =>
            new()
            {
                StatusCode = problem.Status ?? default,
                Content = JsonContent.Create(
                    problem,
                    EcoDataProblemJsonContext.Default.EcoDataProblemDetails,
                    new MediaTypeHeaderValue(ProblemDetailsHttpExtensions.ProblemMediaType)),
            };
    }
}
