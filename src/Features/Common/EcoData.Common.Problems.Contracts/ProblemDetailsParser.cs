using System.Net.Http.Json;
using System.Text.Json;

namespace EcoData.Common.Problems.Contracts;

/// <summary>
/// Parsers only — no ensure/throw policy. A response that is not problem+json,
/// or whose body is malformed, parses to null rather than throwing.
/// </summary>
public static class ProblemDetailsParser
{
    public static async Task<ProblemDetailsDocument?> ParseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/problem+json")
            return null;

        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetailsDocument>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static ProblemDetailsDocument? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ProblemDetailsDocument>(json, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
