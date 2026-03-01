namespace EcoData.Locations.Contracts.Parameters;

public sealed record MunicipalityParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null,
    string? StateCode = null,
    Guid? StateId = null
);
