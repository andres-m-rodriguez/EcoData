using System.Text.Json.Serialization;

namespace EcoData.Common.Problems.Contracts;

public sealed record ProblemDetail(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("status")] int? Status,
    [property: JsonPropertyName("detail")] string? Detail,
    [property: JsonPropertyName("instance")] string? Instance
);

public sealed record ValidationProblemDetail(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("status")] int? Status,
    [property: JsonPropertyName("detail")] string? Detail,
    [property: JsonPropertyName("instance")] string? Instance,
    [property: JsonPropertyName("errors")] Dictionary<string, string[]>? Errors
);
