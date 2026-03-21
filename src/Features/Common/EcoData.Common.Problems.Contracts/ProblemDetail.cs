namespace EcoData.Common.Problems.Contracts;

public sealed record ProblemDetail(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    string? Instance
);

public sealed record ValidationProblemDetail(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    string? Instance,
    Dictionary<string, string[]>? Errors
);
