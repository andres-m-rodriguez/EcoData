namespace EcoData.Locations.Contracts.Parameters;

public sealed record StateParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null
);
