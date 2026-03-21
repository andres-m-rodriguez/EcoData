using System.Net.Http.Json;

namespace EcoData.Common.Problems.Contracts;

public static class HttpResponseExtensions
{
    public static async Task<ProblemDetail> ReadProblemAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default
    )
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetail>(cancellationToken);
        return problem ?? new ProblemDetail(
            Type: null,
            Title: "Unknown Error",
            Status: (int)response.StatusCode,
            Detail: null,
            Instance: null
        );
    }

    public static async Task<ValidationProblemDetail> ReadValidationProblemAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default
    )
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetail>(cancellationToken);
        return problem ?? new ValidationProblemDetail(
            Type: null,
            Title: "Validation Error",
            Status: (int)response.StatusCode,
            Detail: null,
            Instance: null,
            Errors: null
        );
    }
}
